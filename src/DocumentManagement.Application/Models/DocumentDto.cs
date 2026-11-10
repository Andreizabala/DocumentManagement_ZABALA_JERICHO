namespace DocumentManagement.Application.Models;

public sealed record DocumentDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    DateTimeOffset UploadedAtUtc);

