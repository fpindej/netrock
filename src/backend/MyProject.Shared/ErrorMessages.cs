namespace MyProject.Shared;

/// <summary>
/// User-facing error messages organized by domain area.
/// Constants are used in <c>Result.Failure()</c> calls so that messages remain consistent,
/// greppable, and easy to extract into translation keys later.
/// <para>
/// All client-facing messages must be static constants — never interpolate runtime values
/// (role names, user IDs, framework error descriptions) into error responses.
/// Log runtime details server-side via <c>ILogger</c> instead.
/// </para>
/// </summary>
public static class ErrorMessages
{
    /// <summary>
    /// Authentication error messages.
    /// </summary>
    public static class Auth
    {
        public const string LoginInvalidCredentials = "Invalid username or password.";
        public const string LoginAccountLocked = "Account is temporarily locked. Please try again later or contact an administrator.";
        public const string RegisterRoleAssignFailed = "Account was created but role assignment failed. Please contact an administrator.";
        public const string TokenMissing = "Refresh token is missing.";
        public const string TokenNotFound = "Refresh token not found.";
        public const string TokenInvalidated = "Refresh token has been invalidated.";
        public const string TokenReused = "Invalid refresh token.";
        public const string TokenExpired = "Refresh token has expired.";
        public const string TokenUserNotFound = "Token owner not found.";
        public const string NotAuthenticated = "User is not authenticated.";
        public const string InsufficientPermissions = "You do not have the required permissions for this action.";
        public const string UserNotFound = "User not found.";
        public const string PasswordIncorrect = "Current password is incorrect.";
        public const string ResetPasswordFailed = "Password reset failed. The link may have expired or already been used.";
        public const string ResetPasswordTokenInvalid = "Invalid or expired password reset token.";
        public const string EmailVerificationFailed = "Email verification failed. The link may have expired or already been used.";
        public const string EmailAlreadyVerified = "Email address is already verified.";
        public const string PasswordSameAsCurrent = "New password must be different from your current password.";
        public const string CaptchaInvalid = "CAPTCHA verification failed. Please try again.";
    }

    /// <summary>
    /// Two-factor authentication error messages.
    /// </summary>
    public static class TwoFactor
    {
        public const string SetupFailed = "Failed to set up two-factor authentication.";
        public const string VerificationFailed = "The verification code is invalid. Please try again.";
        public const string AlreadyEnabled = "Two-factor authentication is already enabled.";
        public const string NotEnabled = "Two-factor authentication is not enabled.";
        public const string DisableFailed = "Failed to disable two-factor authentication.";
        public const string ChallengeNotFound = "Two-factor challenge not found or expired.";
        public const string ChallengeLocked = "Too many failed attempts. Please log in again.";
        public const string RecoveryCodeInvalid = "The recovery code is invalid.";
        public const string InvalidCode = "The two-factor code is invalid.";
    }

    /// <summary>
    /// User self-service error messages.
    /// </summary>
    public static class User
    {
        public const string NotAuthenticated = "User is not authenticated.";
        public const string NotFound = "User not found.";
        public const string DeleteInvalidPassword = "Invalid password.";
        public const string PhoneNumberTaken = "This phone number is already in use.";
        public const string UpdateFailed = "Failed to update profile.";
        public const string DeleteFailed = "Failed to delete account.";
        public const string LastAdminCannotDelete = "Cannot delete your account while you are the last administrator.";
    }

    /// <summary>
    /// Administrative operation error messages.
    /// </summary>
    public static class Admin
    {
        public const string UserNotFound = "User not found.";
        public const string HierarchyInsufficient = "You do not have sufficient privileges to manage this user.";
        public const string RoleAssignAboveRank = "Cannot assign a role at or above your own rank.";
        public const string RoleRemoveAboveRank = "Cannot remove a role at or above your own rank.";
        public const string RoleSelfRemove = "Cannot remove a role from your own account.";
        public const string LockSelfAction = "Cannot lock your own account.";
        public const string DeleteSelfAction = "Cannot delete your own account.";
        public const string EmailVerificationRequired = "User must have a verified email address before being assigned this role.";
        public const string EmailAlreadyRegistered = "A user with this email address already exists.";
        public const string RoleAssignEscalation = "Cannot assign a role that grants permissions you do not hold.";
        public const string RoleNotFound = "Role not found.";
        public const string RoleAlreadyAssigned = "User already has this role.";
        public const string RoleNotAssigned = "User does not have this role.";
        public const string LastRoleHolder = "Cannot remove this role — this is the last user holding it.";
        public const string RoleAssignFailed = "Failed to assign role.";
        public const string RoleRemoveFailed = "Failed to remove role.";
        public const string LockFailed = "Failed to lock user account.";
        public const string UnlockFailed = "Failed to unlock user account.";
        public const string DeleteFailed = "Failed to delete user account.";
        public const string EmailVerificationFailed = "Failed to verify email address.";
        public const string CreateUserFailed = "Failed to create user account.";
        public const string LastAdminCannotDelete = "Cannot delete this user — they are the last user holding an administrative role.";
    }

    /// <summary>
    /// Role management error messages.
    /// </summary>
    public static class Roles
    {
        public const string SystemRoleCannotBeDeleted = "System roles cannot be deleted.";
        public const string SystemRoleCannotBeRenamed = "System roles cannot be renamed.";
        public const string RoleNotFound = "Role not found.";
        public const string RoleNameTaken = "A role with this name already exists.";
        public const string RoleHasUsers = "Cannot delete a role that has users assigned to it.";
        public const string InvalidPermission = "One or more permission values are invalid.";
        public const string SystemRoleNameReserved = "This name is reserved for a system role.";
        public const string SuperAdminPermissionsFixed = "SuperAdmin permissions cannot be modified.";
        public const string CannotGrantUnheldPermission = "Cannot grant permissions that you do not hold.";
        public const string CreateFailed = "Failed to create role.";
        public const string UpdateFailed = "Failed to update role.";
        public const string DeleteFailed = "Failed to delete role.";
    }

    /// <summary>
    /// Pagination error messages.
    /// </summary>
    public static class Pagination
    {
        public const string InvalidPage = "Page number must be positive.";
        public const string InvalidPageSize = "Page size must be positive.";
    }

    /// <summary>
    /// Server-level error messages.
    /// </summary>
    public static class Server
    {
        public const string InternalError = "An internal error occurred.";
    }

    /// <summary>
    /// Job scheduling error messages.
    /// </summary>
    public static class Jobs
    {
        public const string NotFound = "Job not found.";
        public const string TriggerFailed = "Failed to trigger job.";
        public const string RestoreFailed = "Failed to restore jobs.";
    }

    /// <summary>
    /// Security infrastructure error messages (CSRF, origin validation).
    /// </summary>
    public static class Security
    {
        public const string CrossOriginRequestBlocked = "Cross-origin requests are not allowed.";
    }

    /// <summary>
    /// Avatar upload and processing error messages.
    /// </summary>
    public static class Avatar
    {
        public const string FileTooLarge = "The file exceeds the maximum allowed size of 5 MB.";
        public const string UnsupportedFormat = "Unsupported image format. Allowed formats: JPEG, PNG, WebP, GIF.";
        public const string ProcessingFailed = "Failed to process the avatar image.";
        public const string NotFound = "Avatar not found.";
    }

    /// <summary>
    /// Generic entity operation error messages (repository layer).
    /// </summary>
    public static class Entity
    {
        public const string AddFailed = "Failed to add entity.";
        public const string NotFound = "Entity not found.";
        public const string NotDeleted = "Entity could not be deleted.";
    }
}
