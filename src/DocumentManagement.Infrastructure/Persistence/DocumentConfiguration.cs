using DocumentManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentManagement.Infrastructure.Persistence;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(document => document.StoredFileName).HasMaxLength(64).IsRequired();
        builder.HasIndex(document => document.StoredFileName).IsUnique();
        builder.Property(document => document.ContentType).HasMaxLength(255).IsRequired();
        builder.Property(document => document.FileSize).IsRequired();
        builder.Property(document => document.UploadedAtUtc).IsRequired();
    }
}

