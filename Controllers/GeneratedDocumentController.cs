using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SSRBusiness.Interfaces;

namespace SSRBlazor.Controllers;

/// <summary>
/// API controller for retrieving generated documents from Azure File Share.
/// </summary>
[Route("api/generated-document")]
[ApiController]
public class GeneratedDocumentController : ControllerBase
{
    private readonly IGeneratedDocumentService _documentService;
    private readonly ILogger<GeneratedDocumentController> _logger;

    public GeneratedDocumentController(
        IGeneratedDocumentService documentService,
        ILogger<GeneratedDocumentController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a generated document by its storage path.
    /// </summary>
    /// <param name="path">URL-encoded path within the generated-documents share</param>
    /// <returns>The document file</returns>
    [HttpGet("{**path}")]
    public async Task<IActionResult> GetGeneratedDocument(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return BadRequest("Document path is required.");
            }

            // Decode the path (it may be URL-encoded)
            var documentPath = Uri.UnescapeDataString(path);
            
            _logger.LogInformation("Retrieving generated document: {DocumentPath}", documentPath);

            var stream = await _documentService.RetrieveDocumentAsync(documentPath);
            
            if (stream == null)
            {
                _logger.LogWarning("Generated document not found: {DocumentPath}", documentPath);
                return NotFound($"Document not found: {path}");
            }

            // Extract filename from path
            var fileName = Path.GetFileName(documentPath);
            var contentType = GetMimeType(fileName);

            _logger.LogInformation("Returning generated document: {FileName} ({ContentType})", fileName, contentType);

            return File(stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving generated document: {Path}", path);
            return StatusCode(500, "An error occurred while retrieving the document.");
        }
    }

    /// <summary>
    /// Lists all generated documents for a specific entity.
    /// </summary>
    /// <param name="entityType">Entity type (e.g., "Acquisition", "LetterAgreement")</param>
    /// <param name="entityId">Entity ID</param>
    /// <returns>List of document metadata</returns>
    [HttpGet("list/{entityType}/{entityId:int}")]
    public async Task<IActionResult> ListDocuments(string entityType, int entityId)
    {
        try
        {
            var documents = await _documentService.ListDocumentsAsync(entityType, entityId);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing documents for {EntityType}/{EntityId}", entityType, entityId);
            return StatusCode(500, "An error occurred while listing documents.");
        }
    }

    private static string GetMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".pdf" => "application/pdf",
            ".rtf" => "application/rtf",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}
