using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class ObservationConfiguration : IEntityTypeConfiguration<Observation>
{
    public void Configure(EntityTypeBuilder<Observation> builder)
    {
        builder.ToTable("observations");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        builder.Property(o => o.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(o => o.CategoryId)
            .HasColumnName("category_id")
            .IsRequired(false);

        builder.Property(o => o.AuthorUserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne(o => o.Person)
            .WithMany()
            .HasForeignKey(o => o.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Category)
            .WithMany()
            .HasForeignKey(o => o.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.AuthorUser)
            .WithMany()
            .HasForeignKey(o => o.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}