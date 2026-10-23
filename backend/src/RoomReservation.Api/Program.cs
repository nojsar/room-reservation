using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomReservation.Api.Booking;
using RoomReservation.Api.Booking.Rules;
using RoomReservation.Api.Data;
using RoomReservation.Api.Dtos;
using RoomReservation.Api.Options;
using RoomReservation.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Requests that cannot even be read (bad JSON, wrong types) get the same error shape as rule violations.
builder.Services.Configure<ApiBehaviorOptions>(options =>
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .SelectMany(entry => entry.Value!.Errors.Select(error => new ErrorDto(
                "INVALID_INPUT",
                string.IsNullOrEmpty(entry.Key) ? error.ErrorMessage : $"{entry.Key}: {error.ErrorMessage}")))
            .ToList();
        return new BadRequestObjectResult(new ErrorResponse(errors));
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("RoomReservation"));

// The clock is injected instead of calling DateTime.Now directly, so tests can freeze time.
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.Configure<BookingOptions>(builder.Configuration.GetSection(BookingOptions.SectionName));

// Booking rules R1 to R7. They are stateless, so one instance of each is enough. The validator
// receives all of them as IEnumerable<IBookingRule> and reports errors in this order.
builder.Services.AddSingleton<IBookingRule, MaxDurationRule>();
builder.Services.AddSingleton<IBookingRule, NoRoomOverlapRule>();
builder.Services.AddSingleton<IBookingRule, NoOrganizerOverlapRule>();
builder.Services.AddSingleton<IBookingRule, ValidTimeRangeRule>();
builder.Services.AddSingleton<IBookingRule, CapacityRule>();
builder.Services.AddSingleton<IBookingRule, BookingHorizonRule>();
builder.Services.AddSingleton<IBookingRule, ActiveRoomRule>();
builder.Services.AddScoped<IBookingValidator, BookingValidator>();

builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReservationService, ReservationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
    DbSeeder.Seed(db, timeProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

// Lets integration tests start the app with WebApplicationFactory<Program>.
public partial class Program { }
