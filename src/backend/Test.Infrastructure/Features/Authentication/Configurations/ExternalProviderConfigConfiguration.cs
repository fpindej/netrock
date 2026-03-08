using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Test.Infrastructure.Features.Authentication.Models;

namespace Test.Infrastructure.Features.Authentication.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="ExternalProviderConfig"/> entity in the <c>auth</c> schema.
/// </summary>
internal class ExternalProviderConfigConfiguration : IEntityTypeConfiguration<ExternalProviderConfig>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExternalProviderConfig> builder)
    {
        builder.ToTable("ExternalProviderConfigs", "auth");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.IsEnabled)
            .IsRequired()
            .HasColumnName("Enabled");

        builder.Property(e => e.EncryptedClientId)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(e => e.EncryptedClientSecret)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired(false);

        builder.Property(e => e.UpdatedBy)
            .IsRequired(false);

        builder.HasIndex(e => e.Provider)
            .IsUnique();
    }
}
