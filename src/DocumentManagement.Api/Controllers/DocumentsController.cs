using DocumentManagement.Application.Models;
using DocumentManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagement.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController(DocumentService documentService) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<DocumentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentDto>> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var document = await documentService.UploadAsync(
            content,
            file.FileName,
            file.ContentType,
            file.Length,
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DocumentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> List(CancellationToken cancellationToken) =>
        Ok(await documentService.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<DocumentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await documentService.GetAsync(id, cancellationToken));

    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var document = await documentService.DownloadAsync(id, cancellationToken);
        return File(document.Content, document.ContentType, document.FileName, enableRangeProcessing: true);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await documentService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

