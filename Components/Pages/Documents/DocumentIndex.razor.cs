using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using SSRBusiness.Entities;
using SSRBlazor.Services;
using SSRBlazor.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace SSRBlazor.Components.Pages.Documents;

public partial class DocumentIndex : ComponentBase
{
    // Grid reference
    private MudDataGrid<DocumentTemplate>? _dataGrid;
    private MudMenu? _contextMenu;

    // State
    private DocumentTemplate? _selectedItem;
    private string _displayMessage = string.Empty;
    private string _errorMessage = string.Empty;
    private string _selectedDocumentType = string.Empty;
    private int _totalCount = 0;
    private bool _showDeleteDialog = false;
    private bool _hasAssociatedDocuments = false;

    // Document types for dropdown
    private List<DocumentTypeItem> _documentTypes = new();

    // Dynamic View State
    private bool _isInitialized = false;
    private int? _selectedViewId;
    private List<View> _availableViews = new();
    private string _activeView = "Default";
    private Dictionary<string, bool> _visibleColumns = new();



    protected override async Task OnInitializedAsync()
    {
        // Load document types for dropdown
        await LoadDocumentTypes();

        // Load views
        await LoadViewsAsync();
        _isInitialized = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _dataGrid != null)
        {
            await _dataGrid.ReloadServerData();
        }
    }

    private async Task LoadDocumentTypes()
    {
        try
        {
            _documentTypes = await DocumentService.GetDocumentTypesAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading document types: {ex.Message}";
        }
    }

    /// <summary>
    /// Server-side data loading for the grid
    /// </summary>
    private async Task<GridData<DocumentTemplate>> LoadServerData(GridState<DocumentTemplate> state)
    {
        if (!_isInitialized)
        {
            return new GridData<DocumentTemplate>
            {
                Items = Enumerable.Empty<DocumentTemplate>(),
                TotalItems = 0
            };
        }

        try
        {
            // Convert MudBlazor sort definitions to service format
            var sortDefinitions = state.SortDefinitions
                .Select(s => new SortDefinition
                {
                    SortBy = s.SortBy,
                    Descending = s.Descending
                })
                .ToList();

            // Convert MudBlazor filter definitions to service format
            var filterDefinitions = state.FilterDefinitions
                .Where(f => f.Value != null)
                .Select(f => new FilterDefinition
                {
                    Field = f.Column?.PropertyName ?? string.Empty,
                    Operator = f.Operator?.ToString() ?? "Contains",
                    Value = f.Value?.ToString()
                })
                .ToList();

            // Call the service
            var result = await DocumentService.GetDocumentTemplatesPagedAsync(
                page: state.Page,
                pageSize: state.PageSize,
                documentTypeCode: string.IsNullOrEmpty(_selectedDocumentType) ? null : _selectedDocumentType,
                sortDefinitions: sortDefinitions,
                filterDefinitions: filterDefinitions
            );

            _totalCount = result.TotalCount;

            return new GridData<DocumentTemplate>
            {
                Items = result.Items,
                TotalItems = result.TotalCount
            };
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading data: {ex.Message}", Severity.Error);
            return new GridData<DocumentTemplate>
            {
                Items = Enumerable.Empty<DocumentTemplate>(),
                TotalItems = 0
            };
        }
    }

    /// <summary>
    /// Handle document type dropdown change
    /// </summary>
    private async Task OnDocumentTypeChanged(string value)
    {
        _selectedDocumentType = value;
        _selectedItem = null;

        if (_dataGrid != null)
        {
            await _dataGrid.ReloadServerData();
        }
    }

    /// <summary>
    /// Handle row click for selection, double-click to edit
    /// </summary>
    private void OnRowClick(DataGridRowClickEventArgs<DocumentTemplate> args)
    {
        _selectedItem = args.Item;

        // Double-click navigates to edit (matching OnRowDblClick behavior)
        if (args.MouseEventArgs.Detail == 2)
        {
            EditDocument(args.Item);
        }
    }

    /// <summary>
    /// Row styling function
    /// </summary>
    private string RowStyleFunc(DocumentTemplate item, int index)
    {
        // Highlight selected row
        if (_selectedItem?.DocumentTemplateID == item.DocumentTemplateID)
            return "background-color: #e3f2fd;";

        return string.Empty;
    }

    /// <summary>
    /// Check if document template has an associated document file
    /// </summary>
    private bool HasDocument(DocumentTemplate template)
    {
        return !string.IsNullOrEmpty(template.DocumentTemplateLocation) ||
               !string.IsNullOrEmpty(template.DSFileID);
    }

    /// <summary>
    /// View/download document file
    /// </summary>
    private async Task ViewDocument(DocumentTemplate template)
    {
        try
        {
            var url = await DocumentService.GetDocumentUrlAsync(template.DocumentTemplateID);
            await JSRuntime.InvokeVoidAsync("open", url, "_blank");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error viewing document: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Navigate to add new document template
    /// </summary>
    private void AddDocumentTemplate()
    {
        // Pass selected document type if available
        if (!string.IsNullOrEmpty(_selectedDocumentType))
        {
            Navigation.NavigateTo($"/document/edit?dtc={_selectedDocumentType}");
        }
        else
        {
            Navigation.NavigateTo("/document/edit");
        }
    }

    /// <summary>
    /// Navigate to edit document template
    /// </summary>
    private void EditDocument(DocumentTemplate template)
    {
        Navigation.NavigateTo($"/document/edit?dtid={template.DocumentTemplateID}");
    }

    /// <summary>
    /// Edit selected document template
    /// </summary>
    private void EditSelectedDocument()
    {
        if (_selectedItem != null)
        {
            EditDocument(_selectedItem);
        }
        else
        {
            Snackbar.Add("Please select a document template to edit", Severity.Warning);
        }
    }

    /// <summary>
    /// Show delete confirmation dialog
    /// </summary>
    private async Task DeleteSelectedDocument()
    {
        if (_selectedItem == null)
        {
            Snackbar.Add("Please select a document template to delete", Severity.Warning);
            return;
        }

        // Check for associated documents
        _hasAssociatedDocuments = await DocumentService.HasAssociatedDocumentsAsync(_selectedItem.DocumentTemplateID);
        _showDeleteDialog = true;
    }

    /// <summary>
    /// Confirm and execute delete
    /// </summary>
    private async Task ConfirmDelete()
    {
        if (_selectedItem == null) return;

        try
        {
            await DocumentService.DeleteAsync(_selectedItem.DocumentTemplateID);

            Snackbar.Add("Document template deleted successfully", Severity.Success);

            _showDeleteDialog = false;
            _selectedItem = null;

            if (_dataGrid != null)
            {
                await _dataGrid.ReloadServerData();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error deleting document template: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Reset all filters
    /// </summary>
    private async Task ResetFilters()
    {
        _selectedDocumentType = string.Empty;
        _selectedItem = null;

        if (_dataGrid != null)
        {
            await _dataGrid.ReloadServerData();
        }

        Snackbar.Add("Filters reset", Severity.Info);
    }

    /// <summary>
    /// Export to Excel
    /// </summary>
    private async Task ExportToExcel()
    {
        try
        {
            Snackbar.Add("Preparing export...", Severity.Info);

            var fileBytes = await DocumentService.ExportToExcelAsync(_selectedDocumentType);

            if (fileBytes.Length == 0)
            {
                Snackbar.Add("No data to export or export not implemented", Severity.Warning);
                return;
            }

            var fileName = $"DocumentTemplates_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            await JSRuntime.InvokeVoidAsync(
                "downloadFile",
                fileName,
                Convert.ToBase64String(fileBytes),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            Snackbar.Add("Export complete", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error exporting data: {ex.Message}", Severity.Error);
        }
    }
    private async Task LoadViewsAsync()
    {
        try
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var userId = 0; // Default or extract from claims if available

            if (user.Identity?.IsAuthenticated == true)
            {
                // Logic to get UserID from claims would go here
                // For now assuming 0 or handled by service if not available
            }

            _availableViews = await ViewService.GetAllViewsAsync("Document");
            // Get user preference
            var preferredViewId = await ViewService.GetUserDefaultViewAsync(userId.ToString(), "DocumentIndex");

            if (preferredViewId.HasValue && _availableViews.Any(v => v.ViewID == preferredViewId.Value))
            {
                _selectedViewId = preferredViewId.Value;
            }
            else
            {
                var defaultView = _availableViews.FirstOrDefault();
                _selectedViewId = defaultView?.ViewID;
            }

            // Load columns for the selected view
            if (_selectedViewId.HasValue && _selectedViewId.Value > 0)
            {
                await LoadDataWithViewAsync(_selectedViewId.Value);
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading views: {ex.Message}";
        }
    }

    private async Task OnViewSelected(int? viewId)
    {
        _selectedViewId = viewId;

        if (viewId.HasValue)
        {
            await LoadDataWithViewAsync(viewId.Value);

            // Save preference
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (userId != null)
            {
                await ViewService.SetUserDefaultViewAsync(userId, viewId.Value, "DocumentIndex");

                var view = _availableViews.FirstOrDefault(v => v.ViewID == viewId.Value);
                if (view != null)
                {
                    _activeView = view.ViewName ?? "Default";
                }
            }
        }
    }

    private async Task LoadDataWithViewAsync(int viewId)
    {
        var viewConfig = await ViewService.GetViewConfigurationAsync(viewId);
        if (viewConfig != null)
        {
            _visibleColumns = viewConfig.Fields.ToDictionary(f => f.FieldName, f => f.IsSelected);
        }
    }

    private bool IsColumnVisible(string fieldName)
    {
        if (_visibleColumns.TryGetValue(fieldName, out var isVisible))
        {
            return isVisible;
        }
        return true; // Default to visible if not found
    }

    private async Task OpenColumnOrdering()
    {
        if (!_selectedViewId.HasValue)
        {
            Snackbar.Add("Please select a specific view to customize columns.", Severity.Warning);
            return;
        }

        var viewConfig = await ViewService.GetViewConfigurationAsync(_selectedViewId.Value);
        if (viewConfig == null) return;

        var parameters = new DialogParameters();
        parameters.Add("Fields", viewConfig.Fields);

        var dialog = await DialogService.ShowAsync<SSRBlazor.Components.Dialogs.ColumnOrderingDialog>("Customize Columns", parameters);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is List<ViewFieldSelection> updatedFields)
        {
            viewConfig.Fields = updatedFields;
            var updateResult = await ViewService.UpdateAsync(viewConfig);

            if (updateResult.Success)
            {
                Snackbar.Add("View columns updated.", Severity.Success);
                _visibleColumns = updatedFields.ToDictionary(f => f.FieldName, f => f.IsSelected);
                StateHasChanged();
            }
            else
            {
                Snackbar.Add($"Error updating view: {updateResult.Error}", Severity.Error);
            }
        }
    }
}
