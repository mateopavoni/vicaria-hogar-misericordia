using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;
public class PersonAttachmentConfiguration : IEntityTypeConfiguration<PersonAttachment>
{
    public void Configure(EntityTypeBuilder<PersonAttachment> builder)
    {
        builder.ToTable("person_attachments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OriginalFileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(e => e.StoredFileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(e => e.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.FileSizeBytes)
            .IsRequired();

        builder.Property(e => e.UploadedAt)
            .IsRequired();

        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.UploadedBy)
            .WithMany()
            .HasForeignKey(e => e.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.PersonId);
        builder.HasIndex(e => e.UploadedAt);
    }
}