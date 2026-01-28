using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SSRBusiness.BusinessClasses;
using SSRBusiness.Interfaces;

namespace SSRBlazor.Controllers;

[Route("document")]
[ApiController]
public class DocumentController : ControllerBase
{
    private readonly DocumentTemplateRepository _repository;
    private readonly IFileService _fileService;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(
        DocumentTemplateRepository repository,
        IFileService fileService,
        ILogger<DocumentController> logger)
    {
        _repository = repository;
        _fileService = fileService;
        _logger = logger;
    }

    [HttpGet("{dsFileId}")]
    public async Task<IActionResult> GetDocumentTemplate(string dsFileId)
    {
        try
        {
            // 1. Query DocumentTemplates table by DSFileID
            var template = await _repository.GetByDSFileIdAsync(dsFileId);
            
            if (template == null)
            {
                _logger.LogWarning("Document template not found for DSFileID: {DSFileID}", dsFileId);
                return NotFound($"Document template with ID '{dsFileId}' not found.");
            }

            // 2. Validate required fields
            if (string.IsNullOrEmpty(template.DocumentTypeCode))
            {
                _logger.LogError("DocumentTypeCode is null for DSFileID: {DSFileID}", dsFileId);
                return StatusCode(500, "Document template configuration is invalid.");
            }

            if (string.IsNullOrEmpty(template.DocumentTemplateLocation))
            {
                _logger.LogError("DocumentTemplateLocation is null for DSFileID: {DSFileID}", dsFileId);
                return StatusCode(500, "Document template location is not configured.");
            }

            // 3. Build file path: {DocumentTypeCode}/{DocumentTemplateLocation}
            var filePath = $"{template.DocumentTypeCode}/{template.DocumentTemplateLocation}";
            
            _logger.LogInformation("Retrieving document template from Azure File Share: {FilePath}", filePath);

            // 4. Download from Azure File Share
            Stream fileStream;
            try
            {
                fileStream = await _fileService.DownloadFileAsync("document-templates", filePath);
            }
            catch (FileNotFoundException)
            {
                _logger.LogError("File not found in Azure File Share: {FilePath}", filePath);
                return NotFound($"Document file not found in storage: {filePath}");
            }

            // 5. Determine MIME type from file extension
            var fileName = Path.GetFileName(template.DocumentTemplateLocation);
            var contentType = GetMimeType(fileName);

            _logger.LogInformation("Returning document template: {FileName} ({ContentType})", fileName, contentType);

            // Return file stream with appropriate content type and filename
            return File(fileStream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document template for DSFileID: {DSFileID}", dsFileId);
            return StatusCode(500, "An error occurred while retrieving the document template.");
        }
    }

    private string GetMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".pdf" => "application/pdf",
            ".rtf" => "application/rtf",
            ".txt" => "text/plain",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }
}
