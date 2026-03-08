using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Test.Infrastructure.Features.Audit.Models;
using Test.Infrastructure.Features.Authentication.Models;
using Test.Infrastructure.Features.Jobs.Models;
using Test.Infrastructure.Persistence.Extensions;

namespace Test.Infrastructure.Persistence;

/// <summary>
/// Application database context extending <see cref="IdentityDbContext{TUser, TRole, TKey}"/>
/// with refresh token storage and custom model configuration.
/// </summary>
internal class TestDbContext(DbContextOptions<TestDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    /// <summary>
    /// Gets or sets the refresh tokens table for JWT token rotation.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    /// <summary>
    /// Gets or sets the email tokens table for opaque password-reset and email-verification links.
    /// </summary>
    public DbSet<EmailToken> EmailTokens { get; set; }

    /// <summary>
    /// Gets or sets the paused jobs table for persisting pause state across restarts.
    /// </summary>
    public DbSet<PausedJob> PausedJobs { get; set; }

    /// <summary>
    /// Gets or sets the two-factor authentication challenge tokens for pending 2FA logins.
    /// </summary>
    public DbSet<TwoFactorChallenge> TwoFactorChallenges { get; set; }

    /// <summary>
    /// Gets or sets the external OAuth2 authorization state tokens for pending OAuth flows.
    /// </summary>
    public DbSet<ExternalAuthState> ExternalAuthStates { get; set; }

    /// <summary>
    /// Gets or sets the audit events table for the append-only audit log.
    /// </summary>
    public DbSet<AuditEvent> AuditEvents { get; set; }

    /// <summary>
    /// Gets or sets the external provider configurations for admin-managed OAuth credentials.
    /// </summary>
    public DbSet<ExternalProviderConfig> ExternalProviderConfigs { get; set; }

    /// <summary>
    /// Configures the model by applying all <see cref="IEntityTypeConfiguration{TEntity}"/> from this assembly,
    /// the auth schema, and fuzzy search extensions.
    /// <para>
    /// Role seed data is handled at runtime by <see cref="Extensions.ApplicationBuilderExtensions.InitializeDatabaseAsync"/>
    /// via <c>RoleManager</c>, which correctly handles normalization.
    /// See <see cref="Test.Application.Identity.Constants.AppRoles"/>.
    /// </para>
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TestDbContext).Assembly);
        modelBuilder.ApplyAuthSchema();
        modelBuilder.ApplyFuzzySearch();
    }
}
