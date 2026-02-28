using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Infrastructure.Features.Authentication.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="TwoFactorChallenge"/> entity in the <c>auth</c> schema.
/// </summary>
internal class TwoFactorChallengeConfiguration : IEntityTypeConfiguration<TwoFactorChallenge>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TwoFactorChallenge> builder)
    {
        builder.ToTable("TwoFactorChallenges", "auth");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.ExpiresAt)
            .IsRequired();

        builder.Property(t => t.IsUsed)
            .IsRequired()
            .HasColumnName("Used");

        builder.Property(t => t.IsRememberMe)
            .IsRequired()
            .HasColumnName("RememberMe");

        builder.Property(t => t.FailedAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.Token)
            .IsUnique();

        builder.HasIndex(t => t.ExpiresAt);
    }
}
