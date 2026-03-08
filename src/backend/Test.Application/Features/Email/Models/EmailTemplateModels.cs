namespace Test.Application.Features.Email.Models;

/// <summary>
/// Model for the verify-email template. Exposed as <c>verify_url</c> in Liquid.
/// </summary>
/// <param name="VerifyUrl">The email verification URL.</param>
public record VerifyEmailModel(string VerifyUrl);

/// <summary>
/// Model for the reset-password template. Exposed as <c>reset_url</c> and <c>expiration</c> in Liquid.
/// </summary>
/// <param name="ResetUrl">The password reset URL.</param>
/// <param name="Expiration">Human-readable token expiration (e.g. "24 hours").</param>
public record ResetPasswordModel(string ResetUrl, string Expiration);

/// <summary>
/// Model for the admin-reset-password template. Exposed as <c>reset_url</c> and <c>expiration</c> in Liquid.
/// </summary>
/// <param name="ResetUrl">The password reset URL.</param>
/// <param name="Expiration">Human-readable token expiration (e.g. "24 hours").</param>
public record AdminResetPasswordModel(string ResetUrl, string Expiration);

/// <summary>
/// Model for the invitation template. Exposed as <c>set_password_url</c> and <c>expiration</c> in Liquid.
/// </summary>
/// <param name="SetPasswordUrl">The set-password URL for the invited user.</param>
/// <param name="Expiration">Human-readable token expiration (e.g. "24 hours").</param>
public record InvitationModel(string SetPasswordUrl, string Expiration);

/// <summary>
/// Model for the admin-disable-2fa template. Exposed as <c>user_name</c> and <c>reason</c> in Liquid.
/// </summary>
/// <param name="UserName">The display name or email of the affected user.</param>
/// <param name="Reason">Optional admin-provided reason for disabling 2FA.</param>
public record AdminDisableTwoFactorModel(string UserName, string? Reason);
