namespace MyProject.Api.Tests.Contracts;

// Auth
internal record AuthTokensResponse(string AccessToken, string RefreshToken, bool RequiresTwoFactor, string? ChallengeToken);
internal record TwoFactorSetupContract(string SharedKey, string AuthenticatorUri);
internal record TwoFactorVerifySetupContract(List<string> RecoveryCodes);
internal record RegisterUserResponse(Guid Id);

// Users
internal record UserMeResponse(Guid Id, string Username, string Email, string? FirstName, string? LastName,
    string? PhoneNumber, string? Bio, bool HasAvatar, List<string> Roles, List<string> Permissions,
    bool EmailConfirmed, bool TwoFactorEnabled);

// Admin - Users
internal record AdminUserResponse(Guid Id, string Username, string Email, string? FirstName, string? LastName,
    string? PhoneNumber, string? Bio, bool HasAvatar, List<string> Roles,
    bool EmailConfirmed, bool LockoutEnabled, DateTimeOffset? LockoutEnd, int AccessFailedCount, bool IsLockedOut);
internal record AdminUserListResponse(List<AdminUserResponse> Items, int TotalCount, int PageNumber, int PageSize,
    int TotalPages, bool HasPreviousPage, bool HasNextPage);

// Admin - Roles
internal record AdminRoleResponse(Guid Id, string Name, string? Description, bool IsSystem, int UserCount);
internal record RoleDetailResponse(Guid Id, string Name, string? Description, bool IsSystem,
    List<string> Permissions, int UserCount);
internal record CreateRoleResponse(Guid Id);
internal record PermissionGroupResponse(string Category, List<string> Permissions);

// Jobs
internal record RecurringJobResponse(string Id, string Cron, DateTimeOffset? NextExecution,
    DateTimeOffset? LastExecution, string? LastStatus, bool IsPaused, DateTimeOffset? CreatedAt);
internal record RecurringJobDetailResponse(string Id, string Cron, DateTimeOffset? NextExecution,
    DateTimeOffset? LastExecution, string? LastStatus, bool IsPaused, DateTimeOffset? CreatedAt,
    List<JobExecutionResponse> ExecutionHistory);
internal record JobExecutionResponse(string JobId, string Status, DateTimeOffset? StartedAt,
    TimeSpan? Duration, string? Error);

// Audit
internal record AuditEventContract(Guid Id, Guid? UserId, string Action, string? TargetEntityType,
    Guid? TargetEntityId, string? Metadata, DateTime CreatedAt);
internal record ListAuditEventsContract(List<AuditEventContract> Items, int TotalCount, int PageNumber,
    int PageSize, int TotalPages, bool HasPreviousPage, bool HasNextPage);
