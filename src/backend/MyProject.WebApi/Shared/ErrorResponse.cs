namespace MyProject.WebApi.Shared;

/// <summary>
/// Response DTO for error information.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// A stable, dot-separated error code for frontend localization (e.g. "auth.login.invalidCredentials").
    /// Clients use this to resolve translated error messages. Null when no specific code applies.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// The main error message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Additional error details or technical information.
    /// </summary>
    public string? Details { get; init; }
}
