using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Admin.Dtos;

/// <summary>
/// Represents a user's full profile and account details for admin views.
/// </summary>
public class AdminUserResponse
{
    /// <summary>
    /// The unique identifier of the user.
    /// </summary>
    public Guid Id { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// The username of the user (same as email).
    /// </summary>
    public string Username { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The email address of the user.
    /// </summary>
    public string Email { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The first name of the user.
    /// </summary>
    public string? FirstName { [UsedImplicitly] get; init; }

    /// <summary>
    /// The last name of the user.
    /// </summary>
    public string? LastName { [UsedImplicitly] get; init; }

    /// <summary>
    /// The phone number of the user.
    /// </summary>
    public string? PhoneNumber { [UsedImplicitly] get; init; }

    /// <summary>
    /// A short biography or description of the user.
    /// </summary>
    public string? Bio { [UsedImplicitly] get; init; }

    /// <summary>
    /// The URL to the user's avatar image.
    /// </summary>
    public string? AvatarUrl { [UsedImplicitly] get; init; }

    /// <summary>
    /// The roles assigned to the user.
    /// </summary>
    public IReadOnlyList<string> Roles { [UsedImplicitly] get; init; } = [];

    /// <summary>
    /// Whether the user's email address has been confirmed.
    /// </summary>
    public bool EmailConfirmed { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// Whether lockout is enabled for this user.
    /// </summary>
    public bool LockoutEnabled { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// When the lockout ends, or null if the user is not locked out.
    /// </summary>
    public DateTimeOffset? LockoutEnd { [UsedImplicitly] get; init; }

    /// <summary>
    /// The number of consecutive failed login attempts.
    /// </summary>
    public int AccessFailedCount { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// Whether the user is currently locked out.
    /// </summary>
    public bool IsLockedOut { [UsedImplicitly] get; [UsedImplicitly] init; }
}
