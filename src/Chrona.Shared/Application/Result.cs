namespace Chrona.Shared.Application;

public enum ErrorKind { Validation, NotFound, Conflict }

public sealed record Error(string Code, string Message, ErrorKind Kind, string? Field = null);

public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        // Un succès ne porte jamais d'erreur ; un échec en porte toujours une.
        if (isSuccess == (error is not null))
            throw new InvalidOperationException("A successful result cannot carry an error, and a failed one must.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, Error? error) : base(isSuccess, error) => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot read the value of a failed result.");

    public static Result<T> Success(T value) => new(value, true, null);
    public static new Result<T> Failure(Error error) => new(default, false, error);
}