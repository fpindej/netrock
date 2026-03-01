namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Input for setting an initial password on a passwordless OAuth-created account.
/// </summary>
/// <param name="NewPassword">The password to set.</param>
public record SetPasswordInput(string NewPassword);
