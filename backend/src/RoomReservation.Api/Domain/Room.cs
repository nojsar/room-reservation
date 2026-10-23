namespace RoomReservation.Api.Domain;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Maximum number of people.</summary>
    public int Capacity { get; set; }

    public int Floor { get; set; }

    /// <summary>Where on the floor the room is, e.g. "East wing".</summary>
    public string Location { get; set; } = string.Empty;

    public Equipment Equipment { get; set; }

    /// <summary>Inactive rooms (e.g. under renovation) cannot be booked.</summary>
    public bool IsActive { get; set; } = true;

    public List<Reservation> Reservations { get; set; } = new();
}
