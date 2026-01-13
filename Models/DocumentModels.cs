namespace SSRBlazor.Models;

/// <summary>
/// View model for document type
/// </summary>
public class DocumentTypeModel
{
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string DocumentTypeDescription { get; set; } = string.Empty;
}

/// <summary>
/// View model for document template
/// </summary>
public class DocumentTemplateModel
{
    public int DocumentTemplateID { get; set; }
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string? DocumentTemplateDesc { get; set; }
    public string? TemplatePath { get; set; }
    public bool RequiresCounty { get; set; }
    public bool RequiresOperator { get; set; }
    public List<DocumentCustomFieldModel> CustomFields { get; set; } = new();
}

/// <summary>
/// View model for document custom field
/// </summary>
public class DocumentCustomFieldModel
{
    public int DocTemplateCustomFieldID { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Value { get; set; }
}

/// <summary>
/// View model for acquisition document
/// </summary>
public class AcquisitionDocumentModel
{
    public int AcquisitionDocumentID { get; set; }
    public int AcquisitionID { get; set; }
    public string Handle { get; set; } = string.Empty;
    public string? ParentHandle { get; set; }
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public string? Owner { get; set; }
    public DateTime? TimeCreated { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? TimeModified { get; set; }
    public string? Trace { get; set; }
    public string? MimeType { get; set; }
    public string? FileName { get; set; }
    public bool IsLocked { get; set; }
    public string? LockedBy { get; set; }
}

/// <summary>
/// Request model for generating a document
/// </summary>
public class GenerateDocumentRequest
{
    public int AcquisitionID { get; set; }
    public int DocumentTemplateID { get; set; }
    public int? CountyID { get; set; }
    public int? OperatorID { get; set; }
    public int? NumberOfPagesToRecord { get; set; }
    public Dictionary<string, string> CustomFieldValues { get; set; } = new();
}

/// <summary>
/// Result of document operation
/// </summary>
public class DocumentOperationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? FilePath { get; set; }
}
