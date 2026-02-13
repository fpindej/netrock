using System.Reflection;

namespace MyProject.Application.Identity.Constants;

/// <summary>
/// Defines the application role names used for authorization.
/// <para>
/// All role assignment and lookup should reference these constants instead of inline string literals.
/// ASP.NET Identity normalizes role names to uppercase for comparison, but the constant
/// values defined here use PascalCase for display purposes.
/// </para>
/// <para>
/// Roles follow a strict hierarchy: <c>SuperAdmin</c> (rank 3) &gt; <c>Admin</c> (rank 2) &gt; <c>User</c> (rank 1).
/// A caller can only manage users whose highest role rank is strictly lower than their own.
/// Use <see cref="GetRoleRank"/> to resolve individual role ranks and <see cref="GetHighestRank"/>
/// to determine a user's effective rank from their full role list.
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
    /// The highest-level administrative role. SuperAdmins can manage all users including other admins.
    /// </summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>
    /// All defined roles, discovered automatically from <c>public const string</c> fields.
    /// Adding a new role constant is sufficient — no manual registration required.
    /// </summary>
    public static readonly IReadOnlyList<string> All = typeof(AppRoles)
        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
        .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToList();

    /// <summary>
    /// Returns the hierarchy rank of a single role. Higher rank means more authority.
    /// <para>
    /// Custom roles intentionally receive rank 0, making them assignable by any admin (rank 2+).
    /// Custom roles act as permission bundles with no hierarchy authority — they cannot be used
    /// to manage other users' roles.
    /// </para>
    /// </summary>
    /// <param name="role">The role name.</param>
    /// <returns>The numeric rank: SuperAdmin=3, Admin=2, User=1, custom/unknown=0.</returns>
    public static int GetRoleRank(string role) => role switch
    {
        SuperAdmin => 3,
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
