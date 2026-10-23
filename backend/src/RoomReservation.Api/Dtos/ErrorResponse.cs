namespace RoomReservation.Api.Dtos;

/// <summary>The body of every error response, so the front end handles all errors the same way.</summary>
public sealed record ErrorResponse(IReadOnlyList<ErrorDto> Errors);

public sealed record ErrorDto(string Code, string Message);
