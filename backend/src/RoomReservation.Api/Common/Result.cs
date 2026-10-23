namespace RoomReservation.Api.Common;

/// <summary>
/// The Result pattern: a service returns either a value or a list of errors, instead of throwing
/// exceptions for expected situations such as a broken booking rule. The controller decides
/// which HTTP status code that becomes.
/// </summary>
public sealed class Result<T>
{
    private Result(T? value, IReadOnlyList<Error> errors)
    {
        Value = value;
        Errors = errors;
    }

    public T? Value { get; }
    public IReadOnlyList<Error> Errors { get; }
    public bool IsSuccess => Errors.Count == 0;

    public static Result<T> Success(T value) => new(value, Array.Empty<Error>());

    public static Result<T> Failure(Error error) => new(default, new[] { error });

    public static Result<T> Failure(IEnumerable<Error> errors)
    {
        var list = errors.ToList();
        if (list.Count == 0)
        {
            throw new ArgumentException("A failed result needs at least one error.", nameof(errors));
        }

        return new(default, list);
    }

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
