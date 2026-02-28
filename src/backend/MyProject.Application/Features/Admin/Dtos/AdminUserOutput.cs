namespace MyProject.Application.Features.Admin.Dtos;

/// <summary>
/// Output representing a user's full profile and account details for admin views.
/// </summary>
/// <param name="Id">The user's unique identifier.</param>
/// <param name="UserName">The user's username (same as email in this system).</param>
/// <param name="FirstName">The user's first name, or <c>null</c> if not set.</param>
/// <param name="LastName">The user's last name, or <c>null</c> if not set.</param>
/// <param name="PhoneNumber">The user's phone number, or <c>null</c> if not set.</param>
/// <param name="Bio">The user's biography text, or <c>null</c> if not set.</param>
/// <param name="HasAvatar">Whether the user has an uploaded avatar image.</param>
/// <param name="Roles">The roles assigned to the user.</param>
/// <param name="EmailConfirmed">Whether the user's email address has been confirmed.</param>
/// <param name="LockoutEnabled">Whether lockout is enabled for this user.</param>
/// <param name="LockoutEnd">When the lockout ends, or <c>null</c> if the user is not locked out.</param>
/// <param name="AccessFailedCount">The number of consecutive failed login attempts.</param>
/// <param name="IsLockedOut">Whether the user is currently locked out (computed by the service using <see cref="TimeProvider"/>).</param>
/// <param name="IsTwoFactorEnabled">Whether the user has two-factor authentication enabled.</param>
public record AdminUserOutput(
    Guid Id,
    string UserName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Bio,
    bool HasAvatar,
    IReadOnlyList<string> Roles,
    bool EmailConfirmed,
    bool LockoutEnabled,
    DateTimeOffset? LockoutEnd,
    int AccessFailedCount,
    bool IsLockedOut,
    bool IsTwoFactorEnabled
)
{
    /// <summary>
    /// Email is derived from UserName (they are the same value in this system).
    /// </summary>
    public string Email => UserName;
}
