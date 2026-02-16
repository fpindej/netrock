using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Admin.Dtos.CreateUser;

/// <summary>
/// Request to create a new user account via admin invitation.
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// The email address of the user to invite.
    /// </summary>
    [Required]
    public string Email { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// An optional first name for the new user.
    /// </summary>
    public string? FirstName { get; [UsedImplicitly] init; }

    /// <summary>
    /// An optional last name for the new user.
    /// </summary>
    public string? LastName { get; [UsedImplicitly] init; }
}
