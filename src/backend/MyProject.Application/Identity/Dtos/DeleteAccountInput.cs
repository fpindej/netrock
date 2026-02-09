namespace MyProject.Application.Identity.Dtos;

/// <summary>
/// Input for deleting the current user's account.
/// </summary>
/// <param name="Password">The user's current password for confirmation.</param>
public record DeleteAccountInput(string Password);
