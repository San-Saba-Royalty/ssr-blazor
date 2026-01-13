namespace SSRBlazor.Models;

/// <summary>
/// Represents a document search result from DocuShare or file storage
/// </summary>
public class DocumentSearchResult
{
    /// <summary>
    /// The file ID in the document storage system (e.g., DocuShare handle)
    /// </summary>
    public string FileId { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name of the document
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Original file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// Document type (e.g., "Assignment", "Deed", etc.)
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Content type/MIME type of the document
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
    
    /// <summary>
    /// User who created the document
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// When the document was created
    /// </summary>
    public DateTime? CreationDate { get; set; }
    
    /// <summary>
    /// When the document was last modified
    /// </summary>
    public DateTime? ModifiedDate { get; set; }
    
    /// <summary>
    /// Associated acquisition ID (if applicable)
    /// </summary>
    public int? AcquisitionId { get; set; }
    
    /// <summary>
    /// Detailed acquisition information (loaded optionally)
    /// </summary>
    public AcquisitionDetailsInfo? AcquisitionDetails { get; set; }
}

/// <summary>
/// Acquisition details for display in search results
/// </summary>
public class AcquisitionDetailsInfo
{
    public int AcquisitionId { get; set; }
    public string? SellerName { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? BuyerEffectiveDate { get; set; }
    public string? Counties { get; set; }
    public string? Operators { get; set; }
    public string? Units { get; set; }
    public string? Buyers { get; set; }
}
