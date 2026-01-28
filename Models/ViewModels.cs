namespace SSRBlazor.Models;

/// <summary>
/// View configuration model with field selections
/// </summary>
public class ViewConfiguration
{
    public int ViewID { get; set; }
    public string ViewName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsShared { get; set; }
    public string Module { get; set; } = "Acquisition";
    public List<ViewFieldSelection> Fields { get; set; } = new();
}

/// <summary>
/// Field selection for a view
/// </summary>
public class ViewFieldSelection
{
    public int FieldID { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsSelected { get; set; }
    public int DisplayOrder { get; set; }
    public int? ViewFieldID { get; set; }
}

public static class ViewConstants
{
    public const int MaxDisplayColumns = 25;
}
