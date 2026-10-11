namespace Application.Common;

/// <summary>
/// Outcome of an operation that can fail for an expected reason.
/// </summary>
/// <remarks>
/// Queries do not use this. A record that is not found is a valid answer, not a failure, so query
/// methods return <c>T?</c> instead. Only writes use <see cref="Result{T}"/>.
/// </remarks>
public sealed class Result<T>
{
    private Result(bool succeeded, T? value, Error? error)
    {
        Succeeded = succeeded;
        Value = value;
        Error = error;
    }

    public bool Succeeded { get; }

    public T? Value { get; }

    public Error? Error { get; }

    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(Error error) => new(false, default, error);

    public static Result<T> Failure(ErrorCode code, string message)
        => new(false, default, new Error(code, message));
}

/// <summary>
/// An expected failure, safe to return to a caller.
/// </summary>
public sealed record Error(ErrorCode Code, string Message);

/// <summary>
/// Stable failure identifiers. The HTTP layer maps these to status codes, so a caller never has to
/// parse a message string.
/// </summary>
public enum ErrorCode
{
    Unknown = 0,

    /// <summary>A program with the requested code already exists.</summary>
    ProgramCodeAlreadyExists = 60050,

    /// <summary>The referenced academic level does not exist.</summary>
    ProgramInvalidAcademicLevel = 60051,

    /// <summary>The referenced department does not exist.</summary>
    ProgramInvalidDepartment = 60052,

    /// <summary>The requested program does not exist.</summary>
    ProgramNotFound = 60053
}
