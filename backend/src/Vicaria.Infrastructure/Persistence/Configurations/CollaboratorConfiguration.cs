using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class CollaboratorConfiguration : IEntityTypeConfiguration<Collaborator>
{
    public void Configure(EntityTypeBuilder<Collaborator> builder)
    {
        builder.ToTable("collaborators");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired()
            .UseCollation("Modern_Spanish_CI_AI");

        builder.Property(c => c.LastName).HasColumnName("last_name").HasMaxLength(100)
            .UseCollation("Modern_Spanish_CI_AI");
        builder.Property(c => c.Dni).HasColumnName("dni").HasMaxLength(20);
        builder.Property(c => c.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(c => c.Email).HasColumnName("email").HasMaxLength(255);
        builder.Property(c => c.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(c => c.WorkArea).HasColumnName("work_area").HasMaxLength(100)
            .UseCollation("Modern_Spanish_CI_AI");
        builder.Property(c => c.RegisteredByUserId).HasColumnName("registered_by_user_id").IsRequired();
        builder.Property(c => c.RegisteredAt).HasColumnName("registered_at").IsRequired();

        // DNI único solo cuando está cargado: SQL Server trata los NULL como iguales en un
        // índice único, sin el filtro solo cabería un colaborador sin DNI (SCRUM-199)
        builder.HasIndex(c => c.Dni)
            .IsUnique()
            .HasFilter("[dni] IS NOT NULL");

        // el usuario registrador no se borra mientras queden colaboradores suyos (patrón de todas las FK a User)
        builder.HasOne(c => c.RegisteredByUser)
            .WithMany()
            .HasForeignKey(c => c.RegisteredByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
