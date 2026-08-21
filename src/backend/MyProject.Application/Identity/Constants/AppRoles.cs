namespace MyProject.Application.Identity.Constants;

/// <summary>
/// Defines the application role names used for authorization.
/// <para>
/// All role assignment and lookup should reference these constants instead of inline string literals.
/// ASP.NET Identity normalizes role names to uppercase for comparison, but the constant
/// values defined here use PascalCase for display purposes.
/// </para>
/// <para>
/// Roles follow a strict hierarchy: <c>Superuser</c> (rank 3) &gt; <c>Admin</c> (rank 2) &gt; <c>User</c> (rank 1).
/// A caller can only manage users whose highest role rank is strictly lower than their own.
/// Rank and other role metadata are authored in <see cref="Definitions"/>, seeded into the database
/// at startup, and read from the database at runtime.
/// </para>
/// </summary>
public static class AppRoles
{
    /// <summary>
    /// The default role assigned to all registered users.
    /// </summary>
    public const string User = "User";

    /// <summary>
    /// The administrative role with elevated privileges for user and role management.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// The highest-level administrative role. Superusers can manage all users including other admins.
    /// </summary>
    public const string Superuser = "Superuser";

    /// <summary>
    /// Declarative definitions of the built-in system roles: name, description, hierarchy rank,
    /// flags, and default permissions. The startup seeder upserts these into the database,
    /// which is the runtime source of truth for role metadata.
    /// </summary>
    public static readonly IReadOnlyList<RoleDefinition> Definitions =
    [
        new(
            Superuser,
            "Full access to every feature. Permissions are implicit and cannot be edited.",
            Rank: 3,
            IsSystem: true,
            GrantsAllPermissions: true,
            DefaultPermissions: []),
        new(
            Admin,
            "User and role administration with explicitly granted permissions.",
            Rank: 2,
            IsSystem: true,
            GrantsAllPermissions: false,
            // Admin gets user management + role viewing by default.
            // Roles.Manage is deliberately excluded - only Superuser can create/edit/delete roles.
            DefaultPermissions:
            [
                AppPermissions.Users.View,
                AppPermissions.Users.Manage,
                AppPermissions.Users.AssignRoles,
                AppPermissions.Roles.View
            ]),
        new(
            User,
            "Standard account with access to personal features only.",
            Rank: 1,
            IsSystem: true,
            GrantsAllPermissions: false,
            DefaultPermissions: [])
    ];

    /// <summary>
    /// All built-in role names, derived from <see cref="Definitions"/>.
    /// Adding a new <see cref="RoleDefinition"/> is sufficient - no manual registration required.
    /// </summary>
    public static readonly IReadOnlyList<string> All = Definitions
        .Select(d => d.Name)
        .ToList();

    /// <summary>
    /// Returns the hierarchy rank of a single role. Higher rank means more authority.
    /// <para>
    /// Custom roles intentionally receive rank 0, making them assignable by any admin (rank 2+).
    /// Custom roles act as permission bundles with no hierarchy authority - they cannot be used
    /// to manage other users' roles.
    /// </para>
    /// </summary>
    /// <param name="role">The role name.</param>
    /// <returns>The numeric rank: Superuser=3, Admin=2, User=1, custom/unknown=0.</returns>
    public static int GetRoleRank(string role) => role switch
    {
        Superuser => 3,
        Admin => 2,
        User => 1,
        _ => 0
    };

    /// <summary>
    /// Returns the highest hierarchy rank from a collection of role names.
    /// Returns 0 if the collection is empty or contains only unknown roles.
    /// </summary>
    /// <param name="roles">The role names to evaluate.</param>
    /// <returns>The highest numeric rank found.</returns>
    public static int GetHighestRank(IEnumerable<string> roles) =>
        roles.Select(GetRoleRank).DefaultIfEmpty(0).Max();
}
