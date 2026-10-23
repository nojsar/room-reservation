using RoomReservation.Api.Domain;
using RoomReservation.Api.Dtos;

namespace RoomReservation.Api.Mapping;

/// <summary>
/// Hand-written mapping from entities to DTOs. A mapping library would also work, but explicit
/// mapping is easy to read, easy to debug and fails at compile time when a field changes.
/// </summary>
public static class MappingExtensions
{
    public static RoomDto ToDto(this Room room) => new(
        room.Id,
        room.Name,
        room.Capacity,
        room.Floor,
        room.Location,
        room.Equipment.ToItems(),
        room.IsActive);

    public static UserDto ToDto(this User user, int activeReservationCount) => new(
        user.Id,
        user.FirstName,
        user.LastName,
        user.FullName(),
        activeReservationCount);

    public static string FullName(this User user) => $"{user.FirstName} {user.LastName}";

    /// <summary>Splits a flags value into its single items, e.g. Projector | Whiteboard becomes [Projector, Whiteboard].</summary>
    public static IReadOnlyList<Equipment> ToItems(this Equipment equipment) =>
        Enum.GetValues<Equipment>()
            .Where(item => item != Equipment.None && equipment.HasFlag(item))
            .ToList();

    /// <summary>Combines single items into one flags value.</summary>
    public static Equipment Combine(this IEnumerable<Equipment> items) =>
        items.Aggregate(Equipment.None, (all, item) => all | item);
}
