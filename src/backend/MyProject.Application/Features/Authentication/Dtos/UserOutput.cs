namespace MyProject.Application.Features.Authentication.Dtos;

public record UserOutput(
    Guid Id,
    string UserName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Bio,
    string? AvatarUrl,
    IEnumerable<string> Roles
)
{
    /// <summary>
    /// Email is derived from UserName (they are the same value in this system).
    /// </summary>
    public string Email => UserName;
}
