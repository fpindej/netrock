namespace MyProject.Application.Features.Email;

/// <summary>
/// Constants for email template names used by <see cref="IEmailTemplateRenderer"/> and <see cref="ITemplatedEmailSender"/>.
/// </summary>
public static class EmailTemplateNames
{
    /// <summary>Email verification sent after registration.</summary>
    public const string VerifyEmail = "verify-email";

    /// <summary>Password reset requested by the user.</summary>
    public const string ResetPassword = "reset-password";

    /// <summary>Password reset initiated by an administrator.</summary>
    public const string AdminResetPassword = "admin-reset-password";

    /// <summary>Invitation to set a password for a newly created account.</summary>
    public const string Invitation = "invitation";
}
