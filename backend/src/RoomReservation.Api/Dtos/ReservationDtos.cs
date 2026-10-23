using System.ComponentModel.DataAnnotations;
using RoomReservation.Api.Domain;

namespace RoomReservation.Api.Dtos;

/// <summary>
/// A reservation as the front end sees it. Status and the Can* flags are calculated by the back end,
/// so the front end only shows the buttons that will work.
/// </summary>
public sealed record ReservationDto(
    int Id,
    int RoomId,
    string RoomName,
    int OrganizerId,
    string OrganizerName,
    string Title,
    DateTimeOffset Start,
    DateTimeOffset End,
    DateTimeOffset? EndedEarlyAt,
    DateTimeOffset? CancelledAt,
    int Attendees,
    ReservationStatus Status,
    bool CanEdit,
    bool CanCancel,
    bool CanEndEarly);

/// <summary>The body for creating or editing a reservation. Basic shape checks happen here; booking rules run in the service.</summary>
public sealed class SaveReservationRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Choose a room.")]
    public int RoomId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose an organizer.")]
    public int OrganizerId { get; set; }

    [Required(ErrorMessage = "Enter a meeting title.")]
    [StringLength(200, ErrorMessage = "The title can be at most 200 characters long.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a start time.")]
    public DateTimeOffset? Start { get; set; }

    [Required(ErrorMessage = "Choose an end time.")]
    public DateTimeOffset? End { get; set; }

    [Range(1, 1000, ErrorMessage = "There must be at least 1 attendee.")]
    public int Attendees { get; set; }
}

/// <summary>Query string filters for the reservations list. Every filter is optional.</summary>
public sealed class ReservationFilter
{
    public int? RoomId { get; set; }
    public int? OrganizerId { get; set; }
    public ReservationStatus? Status { get; set; }

    /// <summary>Only reservations that overlap the period from From to To.</summary>
    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }
}
