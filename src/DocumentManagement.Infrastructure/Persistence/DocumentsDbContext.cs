using DocumentManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagement.Infrastructure.Persistence;

public sealed class DocumentsDbContext(DbContextOptions<DocumentsDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentsDbContext).Assembly);
    }
}

