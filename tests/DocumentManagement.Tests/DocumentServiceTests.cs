using DocumentManagement.Application.Exceptions;
using DocumentManagement.Application.Interfaces;
using DocumentManagement.Application.Services;
using DocumentManagement.Domain.Entities;
using Xunit;

namespace DocumentManagement.Tests;

public sealed class DocumentServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 1, 2, 3, TimeSpan.Zero);

    [Fact]
    public async Task UploadSavesFileAndMetadataForValidInput()
    {
        var repository = new FakeRepository();
        var storage = new FakeStorage();
        var service = CreateService(repository, storage);

        var result = await service.UploadAsync(
            new MemoryStream([1, 2, 3]),
            "../quarterly-report.pdf",
            "application/pdf",
            3,
            CancellationToken.None);

        Assert.Equal("quarterly-report.pdf", result.OriginalFileName);
        Assert.Equal(3, result.FileSize);
        Assert.Equal(Now, result.UploadedAtUtc);
        Assert.Single(repository.Documents);
        Assert.Single(storage.Files);
    }

    [Fact]
    public async Task GetReturnsMetadataForExistingDocument()
    {
        var document = CreateDocument();
        var repository = new FakeRepository(document);
        var service = CreateService(repository, new FakeStorage());

        var result = await service.GetAsync(document.Id, CancellationToken.None);

        Assert.Equal(document.Id, result.Id);
        Assert.Equal(document.OriginalFileName, result.OriginalFileName);
    }

    [Fact]
    public async Task GetThrowsForMissingDocument()
    {
        var service = CreateService(new FakeRepository(), new FakeStorage());

        await Assert.ThrowsAsync<DocumentNotFoundException>(() =>
            service.GetAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task UploadRejectsFileOverTenMegabytesWithoutWriting()
    {
        var storage = new FakeStorage();
        var service = CreateService(new FakeRepository(), storage);

        await Assert.ThrowsAsync<DocumentValidationException>(() => service.UploadAsync(
            new MemoryStream([1]),
            "large.bin",
            "application/octet-stream",
            DocumentService.MaximumFileSize + 1,
            CancellationToken.None));
        Assert.Empty(storage.Files);
    }

    [Fact]
    public async Task DeleteRemovesMetadataAndFileForExistingDocument()
    {
        var document = CreateDocument();
        var repository = new FakeRepository(document);
        var storage = new FakeStorage((document.StoredFileName, new byte[] { 1, 2, 3 }));
        var service = CreateService(repository, storage);

        await service.DeleteAsync(document.Id, CancellationToken.None);

        Assert.Empty(repository.Documents);
        Assert.Empty(storage.Files);
    }

    [Fact]
    public async Task DeleteRestoresStagedFileWhenDatabaseSaveFails()
    {
        var document = CreateDocument();
        var repository = new FakeRepository(document) { FailSave = true };
        var storage = new FakeStorage((document.StoredFileName, new byte[] { 1 }));
        var service = CreateService(repository, storage);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync(document.Id, CancellationToken.None));

        Assert.True(storage.Files.ContainsKey(document.StoredFileName));
    }

    private static DocumentService CreateService(FakeRepository repository, FakeStorage storage) =>
        new(repository, storage, new FixedTimeProvider(Now));

    private static Document CreateDocument() => new(
        Guid.NewGuid(), "report.pdf", "stored-key", "application/pdf", 3, Now);

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }

    private sealed class FakeRepository(params Document[] seed) : IDocumentRepository
    {
        public List<Document> Documents { get; } = [.. seed];
        public bool FailSave { get; init; }

        public Task AddAsync(Document document, CancellationToken cancellationToken)
        {
            Documents.Add(document);
            return Task.CompletedTask;
        }

        public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Documents.SingleOrDefault(document => document.Id == id));

        public Task<IReadOnlyList<Document>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Document>>(Documents.ToArray());

        public void Remove(Document document) => Documents.Remove(document);

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            FailSave ? Task.FromException(new InvalidOperationException("Database unavailable.")) : Task.CompletedTask;
    }

    private sealed class FakeStorage(params (string Key, byte[] Content)[] seed) : IFileStorageService
    {
        public Dictionary<string, byte[]> Files { get; } = seed.ToDictionary(item => item.Key, item => item.Content);

        public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken)
        {
            var key = Guid.NewGuid().ToString("N");
            await using var copy = new MemoryStream();
            await content.CopyToAsync(copy, cancellationToken);
            Files.Add(key, copy.ToArray());
            return key;
        }

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
        {
            Stream? stream = Files.TryGetValue(storageKey, out var content) ? new MemoryStream(content) : null;
            return Task.FromResult(stream);
        }

        public Task<IStagedFileDeletion?> StageDeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            if (!Files.Remove(storageKey, out var content))
            {
                return Task.FromResult<IStagedFileDeletion?>(null);
            }

            return Task.FromResult<IStagedFileDeletion?>(new FakeStagedDeletion(Files, storageKey, content));
        }
    }

    private sealed class FakeStagedDeletion(
        Dictionary<string, byte[]> files,
        string key,
        byte[] content) : IStagedFileDeletion
    {
        private bool _completed;

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            _completed = true;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken)
        {
            if (!_completed)
            {
                files[key] = content;
                _completed = true;
            }

            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            if (!_completed)
            {
                files[key] = content;
                _completed = true;
            }

            return ValueTask.CompletedTask;
        }
    }
}

