using RoomReservation.Api.Domain;

namespace RoomReservation.Api.Dtos;

public sealed record RoomDto(
    int Id,
    string Name,
    int Capacity,
    int Floor,
    string Location,
    IReadOnlyList<Equipment> Equipment,
    bool IsActive);

/// <summary>Query string filters for the rooms list. Every filter is optional.</summary>
public sealed class RoomFilter
{
    public int? MinCapacity { get; set; }

    /// <summary>A room must have all of the listed equipment.</summary>
    public List<Equipment>? Equipment { get; set; }

    public int? Floor { get; set; }
}
