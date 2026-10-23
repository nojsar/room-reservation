using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RoomReservation.Api.Data;
using RoomReservation.Api.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("RoomReservation"));

// The clock is injected instead of calling DateTime.Now directly, so tests can freeze time.
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.Configure<BookingOptions>(builder.Configuration.GetSection(BookingOptions.SectionName));

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
