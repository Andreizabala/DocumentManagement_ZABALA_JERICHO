namespace DocumentManagement.Domain.Entities;

public sealed class Document
{
    private Document() { }

    public Document(
        Guid id,
        string originalFileName,
        string storedFileName,
        string contentType,
        long fileSize,
        DateTimeOffset uploadedAtUtc)
    {
        Id = id;
        OriginalFileName = originalFileName;
        StoredFileName = storedFileName;
        ContentType = contentType;
        FileSize = fileSize;
        UploadedAtUtc = uploadedAtUtc;
    }

    public Guid Id { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StoredFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public DateTimeOffset UploadedAtUtc { get; private set; }
}

