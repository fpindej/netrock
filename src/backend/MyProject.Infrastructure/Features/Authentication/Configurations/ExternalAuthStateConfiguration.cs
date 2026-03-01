using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Infrastructure.Features.Authentication.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="ExternalAuthState"/> entity in the <c>auth</c> schema.
/// </summary>
internal class ExternalAuthStateConfiguration : IEntityTypeConfiguration<ExternalAuthState>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExternalAuthState> builder)
    {
        builder.ToTable("ExternalAuthStates", "auth");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.RedirectUri)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(e => e.UserId)
            .IsRequired(false);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.ExpiresAt)
            .IsRequired();

        builder.Property(e => e.IsUsed)
            .IsRequired()
            .HasColumnName("Used");

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Token)
            .IsUnique();

        builder.HasIndex(e => e.ExpiresAt);
    }
}
