using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using MyProject.Application.Identity.Dtos;

namespace MyProject.WebApi.Features.Users.Dtos.DeleteAccount;

/// <summary>
/// Represents a request to permanently delete the current user's account.
/// </summary>
[UsedImplicitly]
public class DeleteAccountRequest
{
    /// <summary>
    /// The user's current password for confirmation.
    /// </summary>
    [Required]
    [MinLength(6)]
    [MaxLength(255)]
    public string Password { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// Converts the request to an application layer input.
    /// </summary>
    public DeleteAccountInput ToInput() => new(Password);
}
