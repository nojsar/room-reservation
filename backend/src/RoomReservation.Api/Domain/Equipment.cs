namespace RoomReservation.Api.Domain;

/// <summary>
/// Equipment a room can have. A room can have several items, so this is a flags enum:
/// each value is its own bit and they combine with |, e.g. Projector | Whiteboard.
/// </summary>
[Flags]
public enum Equipment
{
    None = 0,
    Projector = 1,
    TvScreen = 2,
    VideoConferencing = 4,
    Whiteboard = 8
}
