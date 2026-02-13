namespace MyProject.Application.Features.Admin.Dtos;

/// <summary>
/// A group of permissions belonging to the same category.
/// </summary>
/// <param name="Category">The category name (e.g. <c>"Users"</c>, <c>"Roles"</c>).</param>
/// <param name="Permissions">The permission values in this category.</param>
public record PermissionGroupOutput(string Category, IReadOnlyList<string> Permissions);
