namespace DocumentManagement.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
    Task<IStagedFileDeletion?> StageDeleteAsync(string storageKey, CancellationToken cancellationToken);
}

public interface IStagedFileDeletion : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}

