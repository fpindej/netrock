namespace MyProject.WebApi.Shared;

/// <summary>
/// Base class for all paginated responses.
/// </summary>
public abstract class PaginatedResponse
{
    /// <summary>
    /// The total number of items (across all pages).
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// The current page number.
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// The total number of pages.
    /// </summary>
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;

    /// <summary>
    /// Indicates if there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Indicates if there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
