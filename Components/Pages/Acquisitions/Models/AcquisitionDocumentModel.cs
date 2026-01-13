namespace SSRBlazor.Components.Pages.Acquisitions.Models;

/// <summary>
/// Represents a document associated with an acquisition
/// </summary>
public class AcquisitionDocumentModel
{
    /// <summary>
    /// Unique handle/identifier for the document
    /// </summary>
    public string Handle { get; set; } = string.Empty;
    
    /// <summary>
    /// Parent folder/container handle
    /// </summary>
    public string? ParentHandle { get; set; }
    
    /// <summary>
    /// User who created the document
    /// </summary>
    public string? Owner { get; set; }
    
    /// <summary>
    /// Date/time the document was created
    /// </summary>
    public DateTime? TimeCreated { get; set; }
    
    /// <summary>
    /// User who last modified the document
    /// </summary>
    public string? ModifiedBy { get; set; }
    
    /// <summary>
    /// Date/time the document was last modified
    /// </summary>
    public DateTime? TimeModified { get; set; }
    
    /// <summary>
    /// Document type/trace (e.g., "Deed", "Letter Agreement")
    /// </summary>
    public string? Trace { get; set; }
    
    /// <summary>
    /// Document summary/description
    /// </summary>
    public string? Summary { get; set; }
    
    /// <summary>
    /// Document title/file name
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// Document name
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Document author
    /// </summary>
    public string? Author { get; set; }
    
    /// <summary>
    /// Document description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// MIME type of the document
    /// </summary>
    public string? MimeType { get; set; }
    
    /// <summary>
    /// Whether the document is locked for editing
    /// </summary>
    public bool IsLocked { get; set; }
    
    /// <summary>
    /// User who has the document locked
    /// </summary>
    public string? LockedBy { get; set; }
    
    /// <summary>
    /// Physical file name
    /// </summary>
    public string? FileName { get; set; }
    
    /// <summary>
    /// URL to access/download the document
    /// </summary>
    public string? Url { get; set; }
    
    /// <summary>
    /// Associated user
    /// </summary>
    public string? User { get; set; }
    
    /// <summary>
    /// Binary data (for uploads)
    /// </summary>
    public byte[]? Data { get; set; }
}

/// <summary>
/// Document type for the dropdown
/// </summary>
public class DocumentTypeModel
{
    public int DocumentTypeID { get; set; }
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string DocumentTypeDescription { get; set; } = string.Empty;
}

/// <summary>
/// Document template for generation
/// </summary>
public class DocumentTemplateModel
{
    public int DocumentTemplateID { get; set; }
    public int DocumentTypeID { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string? TemplatePath { get; set; }
    public bool RequiresCounty { get; set; }
    public bool RequiresOperator { get; set; }
    public List<DocumentCustomFieldModel> CustomFields { get; set; } = new();
}

/// <summary>
/// Custom field for document generation
/// </summary>
public class DocumentCustomFieldModel
{
    public string TagName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Value { get; set; }
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