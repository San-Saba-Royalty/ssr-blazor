using Microsoft.EntityFrameworkCore;
using SSRBlazor.Models;
using SSRBusiness.BusinessClasses;
using SSRBusiness.Entities;

namespace SSRBlazor.Services;

/// <summary>
/// Service for managing acquisition documents - simplified for current minimal database schema
/// </summary>
public class AcquisitionDocumentService
{
    private readonly AcquisitionDocumentRepository _repository;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AcquisitionDocumentService> _logger;

    public AcquisitionDocumentService(
        AcquisitionDocumentRepository repository,
        IWebHostEnvironment environment,
        ILogger<AcquisitionDocumentService> logger)
    {
        _repository = repository;
        _environment = environment;
        _logger = logger;
    }

    #region Document Types

    /// <summary>
    /// Get all document types
    /// </summary>
    public async Task<List<DocumentTypeModel>> GetDocumentTypesAsync()
    {
        var types = await _repository.GetDocumentTypesAsync().ToListAsync();
        
        return types.Select(t => new DocumentTypeModel
        {
            DocumentTypeCode = t.DocumentTypeCode,
            DocumentTypeDescription = t.DocumentTypeDesc ?? string.Empty
        }).ToList();
    }

    #endregion

    #region Document Templates

    /// <summary>
    /// Get document templates for a specific document type
    /// </summary>
    public async Task<List<DocumentTemplateModel>> GetDocumentTemplatesByTypeAsync(string documentTypeCode)
    {
        var templates = await _repository.GetDocumentTemplatesByTypeAsync(documentTypeCode).ToListAsync();
        
        return templates.Select(t => new DocumentTemplateModel
        {
            DocumentTemplateID = t.DocumentTemplateID,
            DocumentTypeCode = t.DocumentTypeCode,
            DocumentName = t.DocumentTemplateDesc ?? string.Empty,
            TemplatePath = t.DocumentTemplateLocation
        }).ToList();
    }

    /// <summary>
    /// Get document template with custom fields
    /// </summary>
    public async Task<DocumentTemplateModel?> GetDocumentTemplateAsync(int documentTemplateId)
    {
        var template = await _repository.GetDocumentTemplateByIdAsync(documentTemplateId);
        if (template == null)
            return null;

        var customFields = await _repository.GetTemplateCustomFieldsAsync(documentTemplateId);

        return new DocumentTemplateModel
        {
            DocumentTemplateID = template.DocumentTemplateID,
            DocumentTypeCode = template.DocumentTypeCode,
            DocumentName = template.DocumentTemplateDesc ?? string.Empty,
            TemplatePath = template.DocumentTemplateLocation,
            CustomFields = customFields.Select(cf => new DocumentCustomFieldModel
            {
                DocTemplateCustomFieldID = cf.DocTemplateCustomFieldID,
                TagName = cf.CustomTag ?? string.Empty,
                DisplayName = cf.CustomPhrase ?? string.Empty
            }).ToList()
        };
    }

    #endregion

    #region Acquisition Documents

    /// <summary>
    /// Get all documents for an acquisition
    /// </summary>
    public async Task<List<AcquisitionDocumentModel>> GetAcquisitionDocumentsAsync(int acquisitionId)
    {
        var documents = await _repository.GetAcquisitionDocumentsAsync(acquisitionId).ToListAsync();
        
        return documents.Select(MapToModel).ToList();
    }

    /// <summary>
    /// Get document by handle (using ID as fallback)
    /// </summary>
    public async Task<AcquisitionDocumentModel?> GetDocumentByHandleAsync(string handle)
    {
        var document = await _repository.GetAcquisitionDocumentByHandleAsync(handle);
        return document == null ? null : MapToModel(document);
    }

    /// <summary>
    /// Delete a document by handle
    /// </summary>
    public async Task<bool> DeleteDocumentAsync(string handle)
    {
        try
        {
            var document = await _repository.GetAcquisitionDocumentByHandleAsync(handle);
            if (document == null)
                return false;

            // Delete database record
            await _repository.DeleteAcquisitionDocumentAsync(document);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document with handle {Handle}", handle);
            return false;
        }
    }

    #endregion

    #region Counties and Operators

    /// <summary>
    /// Get counties for document generation dropdown
    /// </summary>
    public async Task<List<(int Id, string Name)>> GetAcquisitionCountiesAsync(int acquisitionId)
    {
        var counties = await _repository.GetAcquisitionCountiesAsync(acquisitionId).ToListAsync();
        return counties.Select(c => (c.CountyID, c.CountyName ?? string.Empty)).ToList();
    }

    /// <summary>
    /// Get operators for document generation dropdown
    /// </summary>
    public async Task<List<(int Id, string Name)>> GetAcquisitionOperatorsAsync(int acquisitionId)
    {
        var operators = await _repository.GetAcquisitionOperatorsAsync(acquisitionId).ToListAsync();
        return operators.Select(o => (o.OperatorID, o.OperatorName ?? string.Empty)).ToList();
    }

    #endregion

    #region Document Generation

    /// <summary>
    /// Generate a document from template
    /// </summary>
    public async Task<DocumentOperationResult> GenerateDocumentAsync(GenerateDocumentRequest request)
    {
        try
        {
            var template = await _repository.GetDocumentTemplateByIdAsync(request.DocumentTemplateID);
            if (template == null)
                return new DocumentOperationResult { Success = false, Error = "Document template not found" };

            if (string.IsNullOrEmpty(template.DocumentTemplateLocation))
                return new DocumentOperationResult { Success = false, Error = "Template file path not configured" };

            // TODO: Implement actual document generation logic
            var generatedFileName = $"Generated_{template.DocumentTemplateDesc}_{DateTime.Now:yyyyMMddHHmmss}.docx";
            var outputPath = Path.Combine("Documents", "Generated", generatedFileName);

            _logger.LogInformation("Document generation requested for template {TemplateId}, acquisition {AcquisitionId}",
                request.DocumentTemplateID, request.AcquisitionID);

            return new DocumentOperationResult { Success = true, FilePath = outputPath };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating document for acquisition {AcquisitionId}", request.AcquisitionID);
            return new DocumentOperationResult { Success = false, Error = ex.Message };
        }
    }

    #endregion

    #region File Upload

    /// <summary>
    /// Upload a document file
    /// </summary>
    public async Task<DocumentOperationResult> UploadDocumentAsync(
        int acquisitionId,
        string fileName,
        string contentType,
        Stream fileStream)
    {
        try
        {
            // For now, just create a minimal database record
            var document = new AcquisitionDocument
            {
                AcquisitionID = acquisitionId,
                CreatedOn = DateTime.Now,
                UserId = "System" // TODO: Get current user
            };

            await _repository.AddAcquisitionDocumentAsync(document);
            
            return new DocumentOperationResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for acquisition {AcquisitionId}", acquisitionId);
            return new DocumentOperationResult { Success = false, Error = ex.Message };
        }
    }

    #endregion

    #region Download

    /// <summary>
    /// Get file path for download
    /// </summary>
    public async Task<DocumentOperationResult> GetDocumentForDownloadAsync(string handle)
    {
        var document = await _repository.GetAcquisitionDocumentByHandleAsync(handle);
        if (document == null)
            return new DocumentOperationResult { Success = false, Error = "Document not found" };

        // TODO: Implement when DocumentLocation column exists
        return new DocumentOperationResult { Success = false, Error = "Document download not yet implemented" };
    }

    #endregion

    #region Helpers

    private static AcquisitionDocumentModel MapToModel(AcquisitionDocument entity)
    {
        return new AcquisitionDocumentModel
        {
            AcquisitionDocumentID = entity.AcquisitionDocumentID,
            AcquisitionID = entity.AcquisitionID,
            Handle = entity.AcquisitionDocumentID.ToString(), // Use ID as handle
            TimeCreated = entity.CreatedOn,
            Summary = $"Document {entity.AcquisitionDocumentID}",
            Title = $"Document {entity.AcquisitionDocumentID}"
        };
    }

    #endregion
}
