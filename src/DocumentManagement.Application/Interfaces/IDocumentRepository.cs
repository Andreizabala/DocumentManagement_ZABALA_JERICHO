using DocumentManagement.Domain.Entities;

namespace DocumentManagement.Application.Interfaces;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken);
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Document>> ListAsync(CancellationToken cancellationToken);
    void Remove(Document document);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

