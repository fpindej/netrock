namespace MyProject.Shared;

/// <summary>
/// Categorizes failure results for HTTP status code mapping at the controller layer.
/// </summary>
public enum ErrorType
{
    /// <summary>Validation or business rule violation (400 Bad Request).</summary>
    Validation = 0,

    /// <summary>Authentication or token failure (401 Unauthorized).</summary>
    Unauthorized = 1,

    /// <summary>Requested resource not found (404 Not Found).</summary>
    NotFound = 2
}
