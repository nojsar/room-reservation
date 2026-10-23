using Microsoft.EntityFrameworkCore;
using RoomReservation.Api.Data;
using RoomReservation.Api.Domain;

namespace RoomReservation.Tests;

/// <summary>Small, hand-made test data: easier to reason about in a test than the generated seed.</summary>
internal static class TestDb
{
    // A fixed Wednesday at 10:00 UTC. Every test uses this as "now".
    public static readonly DateTimeOffset Now = new(2026, 10, 21, 10, 0, 0, TimeSpan.Zero);

    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    public static AppDbContext CreateWithRoomsAndUsers()
    {
        var db = Create();
        db.Rooms.AddRange(
            new Room { Id = 1, Name = "Small", Capacity = 4, Floor = 1, Equipment = Equipment.TvScreen },
            new Room { Id = 2, Name = "Medium", Capacity = 8, Floor = 2, Equipment = Equipment.Projector | Equipment.Whiteboard },
            new Room { Id = 3, Name = "Large", Capacity = 20, Floor = 2, Equipment = Equipment.Projector | Equipment.Whiteboard | Equipment.VideoConferencing },
            new Room { Id = 4, Name = "Closed", Capacity = 10, Floor = 3, Equipment = Equipment.Projector, IsActive = false });
        db.Users.AddRange(
            new User { Id = 1, FirstName = "Jonas", LastName = "Petraitis" },
            new User { Id = 2, FirstName = "Ieva", LastName = "Kazlauskienė" });
        db.SaveChanges();
        return db;
    }

    public static Reservation Reservation(int roomId, int organizerId, DateTimeOffset start, TimeSpan length, int attendees = 2) =>
        new()
        {
            RoomId = roomId,
            OrganizerId = organizerId,
            Title = "Meeting",
            Start = start,
            End = start + length,
            Attendees = attendees,
        };
}
