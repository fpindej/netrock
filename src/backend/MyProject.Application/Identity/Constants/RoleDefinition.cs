namespace MyProject.Application.Identity.Constants;

/// <summary>
/// Declarative definition of a built-in application role. The seeder upserts these definitions
/// into the database at startup; at runtime services read the persisted role metadata columns,
/// making the database the single source of truth for role behavior.
/// </summary>
/// <param name="Name">The role name (e.g. <c>"Admin"</c>).</param>
/// <param name="Description">An optional human-readable description of the role's purpose.</param>
/// <param name="Rank">The hierarchy rank. Higher rank means more authority; custom roles have rank 0.</param>
/// <param name="IsSystem">Whether the role is a built-in system role that cannot be renamed or deleted.</param>
/// <param name="GrantsAllPermissions">Whether the role implicitly grants every permission via a single wildcard claim.</param>
/// <param name="DefaultPermissions">The permission claim values seeded for the role. Seeding is additive-only.</param>
public sealed record RoleDefinition(
    string Name,
    string? Description,
    int Rank,
    bool IsSystem,
    bool GrantsAllPermissions,
    IReadOnlyList<string> DefaultPermissions);
