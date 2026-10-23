namespace RoomReservation.Api.Common;

public enum ErrorType
{
    /// <summary>The request breaks a business rule. Returned as 422 Unprocessable Entity.</summary>
    Validation,

    /// <summary>Something the request refers to does not exist. Returned as 404 Not Found.</summary>
    NotFound
}

/// <summary>
/// A business error: a stable code the front end can rely on, and a message written for people.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Validation)
{
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
}
