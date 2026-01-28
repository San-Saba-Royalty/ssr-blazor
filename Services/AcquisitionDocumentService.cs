using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using SSRBlazor.Models;
using SSRBusiness.BusinessClasses;
using SSRBusiness.Entities;
using SSRBusiness.Interfaces;

namespace SSRBlazor.Services;

/// <summary>
/// Service for managing acquisition documents - simplified for current minimal database schema
/// </summary>
public class AcquisitionDocumentService
{
    private readonly AcquisitionDocumentRepository _repository;
    private readonly AcquisitionRepository _acquisitionRepo; // Added
    private readonly UserRepository _userRepo; // Added
    private readonly AuthenticationStateProvider _authStateProvider; // Added
    private readonly IFileService _fileService; // Refactored to interface
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AcquisitionDocumentService> _logger;

    public AcquisitionDocumentService(
        AcquisitionDocumentRepository repository,
        AcquisitionRepository acquisitionRepo,
        UserRepository userRepo,
        AuthenticationStateProvider authStateProvider,
        IFileService fileService,
        IWebHostEnvironment environment,
        ILogger<AcquisitionDocumentService> logger)
    {
        _repository = repository;
        _acquisitionRepo = acquisitionRepo;
        _userRepo = userRepo;
        _authStateProvider = authStateProvider;
        _fileService = fileService;
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
            // 1. Get Template
            var template = await _repository.GetDocumentTemplateByIdAsync(request.DocumentTemplateID);
            if (template == null)
                return new DocumentOperationResult { Success = false, Error = "Document template not found" };

            if (string.IsNullOrEmpty(template.DocumentTemplateLocation))
                return new DocumentOperationResult { Success = false, Error = "Template file path not configured" };

            // 2 Get Acquisition Data
            var acquisition = await _acquisitionRepo.LoadAcquisitionByAcquisitionIDAsync(request.AcquisitionID);
            if (acquisition == null)
                return new DocumentOperationResult { Success = false, Error = "Acquisition not found" };

            // 3. Get Current User
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var userName = authState.User.Identity?.Name;
            User? user = null;
            if (!string.IsNullOrEmpty(userName))
            {
                user = await _userRepo.LoadUserByUserNameAsync(userName);
            }

            // 4. Setup Paths
            // Assuming templates are stored in wwwroot/Documents/Templates
            // and we save generated files to wwwroot/Documents/Generated
            var webRoot = _environment.WebRootPath;
            var templateDir = Path.Combine(webRoot, "Documents", "Templates");
            var outputDir = Path.Combine(webRoot, "Documents", "Generated");

            // Ensure directories exist
            Directory.CreateDirectory(templateDir);
            Directory.CreateDirectory(outputDir);

            var sourcePath = Path.Combine(templateDir, template.DocumentTemplateLocation);

            // Fallback: check if location is full path or just filename
            if (!File.Exists(sourcePath))
            {
                // Try absolute path if stored that way
                if (File.Exists(template.DocumentTemplateLocation))
                {
                    sourcePath = template.DocumentTemplateLocation;
                }
                else
                {
                    return new DocumentOperationResult { Success = false, Error = $"Template file not found at {sourcePath}" };
                }
            }

            var generatedFileName = $"{acquisition.AcquisitionNumber ?? "Acq"}_{template.DocumentTemplateDesc}_{DateTime.Now:yyyyMMddHHmmss}.docx";
            // Sanitize filename
            generatedFileName = string.Join("_", generatedFileName.Split(Path.GetInvalidFileNameChars()));

            var outputPath = Path.Combine(outputDir, generatedFileName);

            _logger.LogInformation("Generating document {FileName} for Acq {AcqID} using Template {TemplateID}",
                generatedFileName, request.AcquisitionID, request.DocumentTemplateID);

            // 5. Generate Document
            var engine = new WordTemplateEngine();
            // TODO: Handle custom fields from request if Engine supports it? 
            // Current Engine doesn't seem to take custom field values dictionary, relies on Entity data.
            // If Request has custom values, we might need to apply them AFTER engine or modify engine.
            // For now, using standard Engine.

            engine.CreateMergeDocument(sourcePath, outputPath, acquisition, user, request.DocumentTemplateID.ToString());

            // 6. Register in Database
            var docRecord = new AcquisitionDocument
            {
                AcquisitionID = acquisition.AcquisitionID,
                CreatedOn = DateTime.Now,
                UserId = user?.UserId ?? 0
                // DocumentLocation = generatedFileName // If column exists? Using ID/Handle for now.
            };

            await _repository.AddAcquisitionDocumentAsync(docRecord);

            // Return relative path for download
            var relativePath = $"/Documents/Generated/{generatedFileName}";

            return new DocumentOperationResult { Success = true, FilePath = relativePath };
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
            // 1. Get Current User
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var userName = authState.User.Identity?.Name;
            var userId = 0;
            if (!string.IsNullOrEmpty(userName))
            {
                var user = await _userRepo.LoadUserByUserNameAsync(userName);
                if (user != null) userId = user.UserId;
            }

            // 2. Save File using IFileService (Abstraction for Azure/Local)
            // Use 'acquisitions' container as per architecture

            // Clean filename logic
            var safeFileName = $"{acquisitionId}_{DateTime.Now:yyyyMMdd}_{fileName}";
            safeFileName = string.Join("_", safeFileName.Split(Path.GetInvalidFileNameChars()));

            var blobUri = await _fileService.UploadFileAsync("acquisitions", safeFileName, fileStream);

            // 3. Create Record
            var document = new AcquisitionDocument
            {
                AcquisitionID = acquisitionId,
                CreatedOn = DateTime.Now,
                UserId = userId,
                DocumentLocation = blobUri // Storing the URI or Path returned by service
            };

            await _repository.AddAcquisitionDocumentAsync(document);

            // Add new note
            var note = new AcquisitionNote
            {
                AcquisitionID = acquisitionId,
                UserID = userId,
                CreatedDateTime = DateTime.Now,
                NoteTypeCode = "D",
                NoteText = $"Uploaded document: {fileName}"
            };
            await _repository.AddAcquisitionNoteAsync(note);

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
