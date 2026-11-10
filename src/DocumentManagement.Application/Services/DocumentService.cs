using DocumentManagement.Application.Exceptions;
using DocumentManagement.Application.Interfaces;
using DocumentManagement.Application.Models;
using DocumentManagement.Domain.Entities;

namespace DocumentManagement.Application.Services;

public sealed class DocumentService(
    IDocumentRepository repository,
    IFileStorageService fileStorage,
    TimeProvider timeProvider)
{
    public const long MaximumFileSize = 10 * 1024 * 1024;

    public async Task<DocumentDto> UploadAsync(
        Stream content,
        string fileName,
        string? contentType,
        long fileSize,
        CancellationToken cancellationToken)
    {
        ValidateUpload(content, fileName, fileSize);
        var safeFileName = GetSafeLeafFileName(fileName);
        var storageKey = await fileStorage.SaveAsync(content, cancellationToken);

        var document = new Document(
            Guid.NewGuid(),
            safeFileName,
            storageKey,
            GetSafeContentType(contentType),
            fileSize,
            timeProvider.GetUtcNow());

        try
        {
            await repository.AddAsync(document, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await CleanupStoredFileAsync(storageKey);
            throw;
        }

        return Map(document);
    }

    public async Task<IReadOnlyList<DocumentDto>> ListAsync(CancellationToken cancellationToken) =>
        (await repository.ListAsync(cancellationToken)).Select(Map).ToArray();

    public async Task<DocumentDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new DocumentNotFoundException(id);
        return Map(document);
    }

    public async Task<DownloadDocumentResult> DownloadAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new DocumentNotFoundException(id);
        var stream = await fileStorage.OpenReadAsync(document.StoredFileName, cancellationToken)
            ?? throw new DocumentFileMissingException(id);
        return new DownloadDocumentResult(stream, document.ContentType, document.OriginalFileName);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new DocumentNotFoundException(id);
        await using var stagedDeletion = await fileStorage.StageDeleteAsync(
            document.StoredFileName, cancellationToken);

        repository.Remove(document);
        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (stagedDeletion is not null)
            {
                await stagedDeletion.RollbackAsync(CancellationToken.None);
            }

            throw;
        }

        if (stagedDeletion is not null)
        {
            await stagedDeletion.CommitAsync(CancellationToken.None);
        }
    }

    private async Task CleanupStoredFileAsync(string storageKey)
    {
        await using var stagedDeletion = await fileStorage.StageDeleteAsync(storageKey, CancellationToken.None);
        if (stagedDeletion is not null)
        {
            await stagedDeletion.CommitAsync(CancellationToken.None);
        }
    }

    private static void ValidateUpload(Stream content, string fileName, long fileSize)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DocumentValidationException("A file name is required.");
        }

        if (fileSize <= 0)
        {
            throw new DocumentValidationException("The uploaded file must not be empty.");
        }

        if (fileSize > MaximumFileSize)
        {
            throw new DocumentValidationException("The uploaded file exceeds the 10 MB limit.");
        }
    }

    private static string GetSafeLeafFileName(string suppliedName)
    {
        var normalized = suppliedName.Replace('\\', '/');
        var leafName = normalized[(normalized.LastIndexOf('/') + 1)..].Trim();
        if (leafName is "" or "." or "..")
        {
            throw new DocumentValidationException("The uploaded file name is invalid.");
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(leafName
            .Select(character => char.IsControl(character) || invalidCharacters.Contains(character) ? '_' : character)
            .ToArray());
        return sanitized.Length <= 255 ? sanitized : sanitized[..255];
    }

    private static string GetSafeContentType(string? suppliedContentType)
    {
        var contentType = string.IsNullOrWhiteSpace(suppliedContentType)
            ? "application/octet-stream"
            : suppliedContentType.Trim();
        if (contentType.Length > 255 || contentType.Any(char.IsControl))
        {
            throw new DocumentValidationException("The uploaded content type is invalid.");
        }

        return contentType;
    }

    private static DocumentDto Map(Document document) => new(
        document.Id,
        document.OriginalFileName,
        document.ContentType,
        document.FileSize,
        document.UploadedAtUtc);
}

