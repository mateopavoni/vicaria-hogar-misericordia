using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class PsychiatricEvaluationConfiguration : IEntityTypeConfiguration<PsychiatricEvaluation>
{
    public void Configure(EntityTypeBuilder<PsychiatricEvaluation> builder)
    {
        builder.ToTable("psychiatric_evaluation");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.Professional)
            .HasMaxLength(200);

        builder.Property(e => e.Diagnosis);

        builder.Property(e => e.IsValid)
            .IsRequired();

        // 1:N — una persona puede tener varias evaluaciones
        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1:N — un usuario puede registrar evaluaciones de distintas personas;
        // no se borran evaluaciones si se borra el usuario
        builder.HasOne(e => e.RegisteredBy)
            .WithMany()
            .HasForeignKey(e => e.RegisteredByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.PersonId);
    }
}