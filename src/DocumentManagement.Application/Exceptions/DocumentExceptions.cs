namespace DocumentManagement.Application.Exceptions;

public sealed class DocumentNotFoundException(Guid id)
    : Exception($"Document '{id}' was not found.");

public sealed class DocumentFileMissingException(Guid id)
    : Exception($"The stored file for document '{id}' was not found.");

public sealed class DocumentValidationException(string message)
    : Exception(message);

