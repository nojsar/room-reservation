using Microsoft.Extensions.Time.Testing;
using RoomReservation.Api.Domain;
using RoomReservation.Api.Dtos;
using RoomReservation.Api.Services;

namespace RoomReservation.Tests;

public class RoomServiceTests
{
    [Fact]
    public async Task GetRooms_FiltersByMinimumCapacity()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        var rooms = await new RoomService(db).GetRoomsAsync(new RoomFilter { MinCapacity = 8 }, default);

        // Sorted by floor, then by name.
        Assert.Equal(new[] { "Large", "Medium", "Closed" }, rooms.Select(r => r.Name));
    }

    [Fact]
    public async Task GetRooms_EquipmentFilterRequiresEveryItem()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        var filter = new RoomFilter { Equipment = new() { Equipment.Projector, Equipment.VideoConferencing } };

        var rooms = await new RoomService(db).GetRoomsAsync(filter, default);

        Assert.Equal("Large", Assert.Single(rooms).Name);
    }

    [Fact]
    public async Task GetRooms_FiltersByFloor()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        var rooms = await new RoomService(db).GetRoomsAsync(new RoomFilter { Floor = 2 }, default);

        Assert.Equal(new[] { "Large", "Medium" }, rooms.Select(r => r.Name));
    }

    [Fact]
    public async Task GetRooms_ReturnsEquipmentAsSeparateItems()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        var rooms = await new RoomService(db).GetRoomsAsync(new RoomFilter(), default);

        Assert.Equal(new[] { Equipment.Projector, Equipment.Whiteboard }, rooms.Single(r => r.Name == "Medium").Equipment);
    }

    [Fact]
    public async Task GetRoom_UnknownId_ReturnsNotFoundError()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        var result = await new RoomService(db).GetRoomAsync(99, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("ROOM_NOT_FOUND", Assert.Single(result.Errors).Code);
    }
}

public class UserServiceTests
{
    [Fact]
    public async Task GetUsers_CountsOnlyBookedReservations()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        var now = TestDb.Now;
        db.Reservations.AddRange(
            TestDb.Reservation(1, 1, now.AddDays(1), TimeSpan.FromHours(1)),                 // future: active
            TestDb.Reservation(2, 1, now.AddMinutes(-30), TimeSpan.FromHours(1)),            // in progress: active
            TestDb.Reservation(3, 1, now.AddDays(-1), TimeSpan.FromHours(1)),                // finished: not active
            Cancelled(TestDb.Reservation(1, 1, now.AddDays(2), TimeSpan.FromHours(1)), now), // cancelled: not active
            TestDb.Reservation(1, 2, now.AddDays(3), TimeSpan.FromHours(1)));                // another user
        db.SaveChanges();

        var users = await new UserService(db, new FakeTimeProvider(now)).GetUsersAsync(default);

        Assert.Equal(2, users.Single(u => u.Id == 1).ActiveReservationCount);
        Assert.Equal(1, users.Single(u => u.Id == 2).ActiveReservationCount);
    }

    private static Reservation Cancelled(Reservation reservation, DateTimeOffset at)
    {
        reservation.CancelledAt = at;
        return reservation;
    }
}
