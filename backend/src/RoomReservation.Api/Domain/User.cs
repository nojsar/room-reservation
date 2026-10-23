namespace RoomReservation.Api.Domain;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>Reservations this user organizes.</summary>
    public List<Reservation> Reservations { get; set; } = new();
}
