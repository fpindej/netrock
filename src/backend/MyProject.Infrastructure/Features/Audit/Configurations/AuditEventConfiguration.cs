using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyProject.Infrastructure.Features.Audit.Models;

namespace MyProject.Infrastructure.Features.Audit.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="AuditEvent"/> entity in the <c>audit</c> schema.
/// </summary>
internal class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents", "audit");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.TargetEntityType)
            .HasMaxLength(50);

        builder.Property(x => x.Metadata)
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // No FK on UserId — users are hard-deleted and audit records must survive deletion.
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Action);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.TargetEntityId);
    }
}
