namespace MyProject.Infrastructure.Persistence.Options;

/// <summary>
/// Configuration options for seeding initial users from environment variables.
/// Bound from the <c>Seed</c> configuration section (e.g. <c>Seed__Users__0__Email</c>).
/// </summary>
internal sealed class SeedOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Seed";

    /// <summary>
    /// The list of users to seed on startup.
    /// </summary>
    public List<SeedUserEntry> Users { get; init; } = [];
}

/// <summary>
/// A single user entry for startup seeding.
/// </summary>
internal sealed class SeedUserEntry
{
    /// <summary>
    /// The email address (used as both email and username).
    /// </summary>
    public string Email { get; init; } = "";

    /// <summary>
    /// The initial password.
    /// </summary>
    public string Password { get; init; } = "";

    /// <summary>
    /// The role to assign (must match a value in <see cref="Application.Identity.Constants.AppRoles"/>).
    /// </summary>
    public string Role { get; init; } = "";
}
