namespace MyProject.Application.Features.Audit;

/// <summary>
/// Defines all audit action constants used to classify audit events.
/// </summary>
public static class AuditActions
{
    /// <summary>Successful user login.</summary>
    public const string LoginSuccess = "LoginSuccess";

    /// <summary>Failed login attempt (wrong credentials or unknown email).</summary>
    public const string LoginFailure = "LoginFailure";

    /// <summary>User logged out.</summary>
    public const string Logout = "Logout";

    /// <summary>New user registration.</summary>
    public const string Register = "Register";

    /// <summary>User changed their own password.</summary>
    public const string PasswordChange = "PasswordChange";

    /// <summary>User requested a password reset email.</summary>
    public const string PasswordResetRequest = "PasswordResetRequest";

    /// <summary>User reset their password via email link.</summary>
    public const string PasswordReset = "PasswordReset";

    /// <summary>User verified their email address.</summary>
    public const string EmailVerification = "EmailVerification";

    /// <summary>User requested a new verification email.</summary>
    public const string ResendVerificationEmail = "ResendVerificationEmail";

    /// <summary>User updated their profile.</summary>
    public const string ProfileUpdate = "ProfileUpdate";

    /// <summary>User deleted their own account.</summary>
    public const string AccountDeletion = "AccountDeletion";

    /// <summary>Admin created a new user account.</summary>
    public const string AdminCreateUser = "AdminCreateUser";

    /// <summary>Admin locked a user account.</summary>
    public const string AdminLockUser = "AdminLockUser";

    /// <summary>Admin unlocked a user account.</summary>
    public const string AdminUnlockUser = "AdminUnlockUser";

    /// <summary>Admin deleted a user account.</summary>
    public const string AdminDeleteUser = "AdminDeleteUser";

    /// <summary>Admin manually verified a user's email.</summary>
    public const string AdminVerifyEmail = "AdminVerifyEmail";

    /// <summary>Admin sent a password reset email to a user.</summary>
    public const string AdminSendPasswordReset = "AdminSendPasswordReset";

    /// <summary>Admin assigned a role to a user.</summary>
    public const string AdminAssignRole = "AdminAssignRole";

    /// <summary>Admin removed a role from a user.</summary>
    public const string AdminRemoveRole = "AdminRemoveRole";

    /// <summary>Admin created a new role.</summary>
    public const string AdminCreateRole = "AdminCreateRole";

    /// <summary>Admin updated a role.</summary>
    public const string AdminUpdateRole = "AdminUpdateRole";

    /// <summary>Admin deleted a role.</summary>
    public const string AdminDeleteRole = "AdminDeleteRole";

    /// <summary>Admin updated role permissions.</summary>
    public const string AdminSetRolePermissions = "AdminSetRolePermissions";

    /// <summary>User uploaded an avatar image.</summary>
    public const string AvatarUpload = "AvatarUpload";

    /// <summary>User removed their avatar image.</summary>
    public const string AvatarRemove = "AvatarRemove";
}
