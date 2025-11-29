using Domain.Entities.Organizations;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Organizations;

internal sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments", Schemas.Default);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.BlobPath)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(x => x.Description)
            .HasMaxLength(512);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
