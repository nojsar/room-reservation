namespace RoomReservation.Api.Dtos;

public sealed record UserDto(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    int ActiveReservationCount);
