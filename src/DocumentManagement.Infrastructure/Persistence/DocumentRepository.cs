using DocumentManagement.Application.Interfaces;
using DocumentManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagement.Infrastructure.Persistence;

public sealed class DocumentRepository(DocumentsDbContext dbContext) : IDocumentRepository
{
    public async Task AddAsync(Document document, CancellationToken cancellationToken) =>
        await dbContext.Documents.AddAsync(document, cancellationToken);

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Documents.SingleOrDefaultAsync(document => document.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Document>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Documents
            .AsNoTracking()
            .OrderByDescending(document => document.UploadedAtUtc)
            .ToListAsync(cancellationToken);

    public void Remove(Document document) => dbContext.Documents.Remove(document);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}

