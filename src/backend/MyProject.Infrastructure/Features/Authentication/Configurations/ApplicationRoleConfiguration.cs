using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Infrastructure.Features.Authentication.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="ApplicationRole"/> metadata columns.
/// The <c>auth.Roles</c> table mapping itself is applied by <c>ModelBuilderExtensions.ApplyAuthSchema()</c>.
/// </summary>
internal class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.Property(x => x.IsSystem)
            .HasColumnName("System")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.Rank)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.GrantsAllPermissions)
            .IsRequired()
            .HasDefaultValue(false);
    }
}
