namespace DocumentManagement.Application.Models;

public sealed record DownloadDocumentResult(
    Stream Content,
    string ContentType,
    string FileName);

