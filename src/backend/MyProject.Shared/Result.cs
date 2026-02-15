namespace MyProject.Shared;

/// <summary>
/// Represents the result of an operation, indicating success or failure.
/// </summary>
/// <remarks>Pattern documented in src/backend/AGENTS.md — update both when changing.</remarks>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets the error category for the failure, or <c>null</c> when not specified (defaults to 400).
    /// Controllers pass this to <c>ProblemFactory.Create</c> for HTTP status code mapping.
    /// </summary>
    public ErrorType? ErrorType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    protected Result(bool isSuccess, string? error = null, ErrorType? errorType = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success()
    {
        return new Result(true);
    }

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// Defaults to 400 (Validation) at the controller layer.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result containing the error message.</returns>
    public static Result Failure(string error)
    {
        return new Result(false, error, Shared.ErrorType.Validation);
    }

    /// <summary>
    /// Creates a failed result with the specified error message and error category.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="errorType">The error category for HTTP status code mapping.</param>
    /// <returns>A failed result containing the error message and error type.</returns>
    public static Result Failure(string error, ErrorType errorType)
    {
        return new Result(false, error, errorType);
    }
}

/// <summary>
/// Represents the result of an operation, indicating success or failure, and optionally containing a value.
/// </summary>
/// <typeparam name="T">The type of the value returned in case of success.</typeparam>
/// <remarks>Pattern documented in src/backend/AGENTS.md — update both when changing.</remarks>
public class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Gets the value returned by the operation.
    /// Throws <see cref="InvalidOperationException"/> if the result is a failure.
    /// </summary>
    // _value! is sound here: Success() guarantees _value is non-null, and the
    // IsSuccess guard prevents access on failure paths. Same pattern as Nullable<T>.Value.
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result.");

    private Result(bool isSuccess, string? error, T? value, ErrorType? errorType = null)
        : base(isSuccess, error, errorType)
    {
        _value = value;
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The value to return.</param>
    /// <returns>A successful result containing the value.</returns>
    public static Result<T> Success(T value)
    {
        return new Result<T>(true, null, value);
    }

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// Defaults to 400 (Validation) at the controller layer.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result containing the error message.</returns>
    public new static Result<T> Failure(string error)
    {
        return new Result<T>(false, error, default, Shared.ErrorType.Validation);
    }

    /// <summary>
    /// Creates a failed result with the specified error message and error category.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="errorType">The error category for HTTP status code mapping.</param>
    /// <returns>A failed result containing the error message and error type.</returns>
    public new static Result<T> Failure(string error, ErrorType errorType)
    {
        return new Result<T>(false, error, default, errorType);
    }
}
