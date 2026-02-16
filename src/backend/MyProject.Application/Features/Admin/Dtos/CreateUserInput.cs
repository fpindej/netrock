namespace MyProject.Application.Features.Admin.Dtos;

/// <summary>
/// Input for admin-initiated user creation.
/// </summary>
public record CreateUserInput(string Email, string? FirstName, string? LastName);
