namespace SSRBlazor.Components.Filters.Models;

/// <summary>
/// View model for filter detail row in the UI
/// </summary>
public class FilterDetailModel
{
    /// <summary>
    /// Row number for display purposes
    /// </summary>
    public int RowNumber { get; set; }
    
    /// <summary>
    /// Name of the field to filter on (e.g., "SellerLastName", "CountyName")
    /// </summary>
    public string? FieldName { get; set; }
    
    /// <summary>
    /// Type of comparison (e.g., "Contains", "Equals", "GreaterThan")
    /// </summary>
    public string? ComparisonType { get; set; }
    
    /// <summary>
    /// Value to compare against
    /// </summary>
    public string? CompareValue { get; set; }
}

/// <summary>
/// Defines an available field for filtering
/// </summary>
public class FilterFieldDefinition
{
    public string FieldName { get; }
    public string DisplayName { get; }
    public FieldType FieldType { get; }

    public FilterFieldDefinition(string fieldName, string displayName, FieldType fieldType)
    {
        FieldName = fieldName;
        DisplayName = displayName;
        FieldType = fieldType;
    }
}

/// <summary>
/// Comparison type option for dropdown
/// </summary>
public class ComparisonTypeOption
{
    public string Value { get; }
    public string Description { get; }

    public ComparisonTypeOption(string value, string description)
    {
        Value = value;
        Description = description;
    }
}

/// <summary>
/// Field data types for determining available comparison operations
/// </summary>
public enum FieldType
{
    String,
    Number,
    Decimal,
    Date,
    Boolean
}