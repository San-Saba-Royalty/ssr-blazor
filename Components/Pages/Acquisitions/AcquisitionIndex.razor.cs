using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using MudBlazor;
using SSRBusiness.Entities;
using SSRBlazor.Services;
using SSRBlazor.Models;
using AcquisitionEntity = SSRBusiness.Entities.Acquisition;

namespace SSRBlazor.Components.Pages.Acquisitions;

public partial class AcquisitionIndex : ComponentBase
{
    // Grid reference
    private MudDataGrid<AcquisitionEntity>? _dataGrid;

    // State
    private AcquisitionEntity? _selectedItem;
    private string _displayMessage = string.Empty;
    private List<string> _validationErrors = new();
    private string _activeFilter = "All Records";
    private string _activeView = "Default";
    private int _totalCount = 0;
    private bool _showBarcodeDialog = false;

    // View State
    private int? _selectedViewId;
    private List<View> _availableViews = new();
    private List<NamedFilter> _availableFilters = new();

    // Current view's visible columns
    private HashSet<string> _visibleColumns = new();

    // Flag to prevent concurrent loading during initialization
    private bool _isInitialized = false;

    protected override async Task OnInitializedAsync()
    {
        // Load available filters from AcquisitionService
        _availableFilters = await AcquisitionService.GetAvailableFiltersAsync();

        // Load available views from ViewService
        await LoadViewsAsync();

        // Load user's default view
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            _selectedViewId = await ViewService.GetUserDefaultViewAsync(userId);
            if (_selectedViewId.HasValue)
            {
                var view = _availableViews.FirstOrDefault(v => v.ViewID == _selectedViewId.Value);
                if (view != null)
                {
                    _activeView = view.ViewName ?? "Default";
                    // Mark initialized before loading data
                    _isInitialized = true;
                    await LoadDataWithViewAsync(_selectedViewId.Value);
                    return; // LoadDataWithViewAsync calls LoadDataAsync
                }
            }
        }

        // If no default view, fallback to legacy default or just load data
        if (_visibleColumns.Count == 0)
        {
            InitializeDefaultLegacyView();
        }

        _isInitialized = true;
        await LoadDataAsync();
    }

    private void InitializeDefaultLegacyView()
    {
        _visibleColumns = new HashSet<string>
        {
            "AcquisitionID",
            "AcquisitionNumber",
            "Buyer",
            "Assignee",
            "EffectiveDate",
            "DueDate",
            "PaidDate",
            "TotalBonus",
            "ClosingStatus",
            "SsrInPay"
        };
    }

    private async Task LoadViewsAsync()
    {
        _availableViews = await ViewService.GetAllViewsAsync();
    }

    private async Task OnViewSelected(int? viewId)
    {
        _selectedViewId = viewId;

        // Update UI Label
        if (viewId.HasValue)
        {
            var view = _availableViews.FirstOrDefault(v => v.ViewID == viewId.Value);
            _activeView = view?.ViewName ?? "Default";
        }
        else
        {
            _activeView = "Default";
            // Check if user has "Default" view logic, otherwise fallback
            InitializeDefaultLegacyView();
        }

        // Persist this choice
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId) && viewId.HasValue)
        {
            await ViewService.SetUserDefaultViewAsync(userId, viewId.Value);
        }

        // Reload grid with new view configuration
        await LoadDataWithViewAsync(viewId);

        StateHasChanged();
    }

    private async Task LoadDataWithViewAsync(int? viewId)
    {
        if (!viewId.HasValue)
        {
            // Reset to default columns if needed
            _activeFilter = "All Records";
            InitializeDefaultLegacyView();
            if (_dataGrid != null) await _dataGrid.ReloadServerData();
            return;
        }

        var viewConfig = await ViewService.GetViewConfigurationAsync(viewId.Value);
        if (viewConfig != null)
        {
            // Apply view's selected fields to grid
            var selectedFields = viewConfig.Fields.Where(f => f.IsSelected).Select(f => f.FieldName).ToList();

            if (selectedFields.Any())
            {
                _visibleColumns = new HashSet<string>(selectedFields);
            }
            else
            {
                // FALLBACK: View exists but has no selected columns
                // Use default columns to prevent empty grid
                Snackbar.Add($"View '{_activeView}' has no columns configured. Using defaults. Please configure columns using the Column Ordering button.", Severity.Warning);
                InitializeDefaultLegacyView();
            }
        }
        else
        {
            // View not found, use defaults
            Snackbar.Add($"View configuration not found. Using default columns.", Severity.Warning);
            InitializeDefaultLegacyView();
        }

        if (_dataGrid != null) await _dataGrid.ReloadServerData();
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

        // Note: Assuming ColumnOrderingDialog exists in SSRBlazor.Components.Dialogs
        // If not, this will need to be adjusted to the correct namespace or component name
        var dialog = DialogService.Show<SSRBlazor.Components.Dialogs.ColumnOrderingDialog>("Column Ordering", parameters);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is List<ViewFieldSelection> orderedFields)
        {
            viewConfig.Fields = orderedFields;
            // Save the updated view configuration
            var updateResult = await ViewService.UpdateAsync(viewConfig);
            if (updateResult.Success)
            {
                Snackbar.Add("View columns updated.", Severity.Success);
                await LoadDataWithViewAsync(_selectedViewId.Value);
            }
            else
            {
                Snackbar.Add($"Error updating view: {updateResult.Error}", Severity.Error);
            }
        }
    }

    // Helper for loading data (called by LoadServerData)
    private async Task LoadDataAsync()
    {
        if (_dataGrid != null)
        {
            await _dataGrid.ReloadServerData();
        }
    }

    private bool IsColumnVisible(string columnName)
    {
        // If "All Fields" view (or logic dictates everything shown), logic could go here
        // But with ViewService, we strictly follow _visibleColumns unless it's empty/all
        if (_activeView == "All Fields" || _visibleColumns.Count == 0)
            return true;

        return _visibleColumns.Contains(columnName);
    }

    /// <summary>
    /// Server-side data loading for the grid using AcquisitionService
    /// </summary>
    private async Task<GridData<AcquisitionEntity>> LoadServerData(GridState<AcquisitionEntity> state)
    {
        try
        {
            // Prevent concurrent access if initialization is not complete
            if (!_isInitialized)
            {
                return new GridData<AcquisitionEntity>
                {
                    Items = Enumerable.Empty<AcquisitionEntity>(),
                    TotalItems = 0
                };
            }

            // Convert MudBlazor sort definitions to our service format
            var sortDefinitions = state.SortDefinitions
                .Select(s => new SortDefinition
                {
                    SortBy = s.SortBy,
                    Descending = s.Descending
                })
                .ToList();

            // Convert MudBlazor filter definitions to our service format
            var filterDefinitions = state.FilterDefinitions
                    .Where(f => f.Value != null)
                    .Select(f => new FilterDefinition
                    {
                        Field = f.Column?.PropertyName ?? string.Empty,
                        Operator = f.Operator?.ToString() ?? "Contains",
                        Value = f.Value?.ToString()
                    })
                    .ToList();

            // Call the service to get paginated data
            var result = await AcquisitionService.GetAcquisitionsPagedAsync(
                page: state.Page,
                pageSize: state.PageSize,
                activeFilter: _activeFilter,
                sortDefinitions: sortDefinitions,
                filterDefinitions: filterDefinitions
            );

            _totalCount = result.TotalCount;

            return new GridData<AcquisitionEntity>
            {
                Items = result.Items,
                TotalItems = result.TotalCount
            };
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading data: {ex.Message}", Severity.Error);
            return new GridData<AcquisitionEntity>
            {
                Items = Enumerable.Empty<AcquisitionEntity>(),
                TotalItems = 0
            };
        }
    }

    /// <summary>
    /// Handle row click to select (double-click to edit)
    /// </summary>
    private void OnRowClick(DataGridRowClickEventArgs<AcquisitionEntity> args)
    {
        _selectedItem = args.Item;

        // Double-click navigates to edit
        if (args.MouseEventArgs.Detail == 2)
        {
            EditSelectedAcquisition();
        }
    }

    /// <summary>
    /// Row styling function - highlight overdue or special status
    /// </summary>
    private string RowStyleFunc(AcquisitionEntity item, int index)
    {
        // Highlight items with liens in light orange
        if (item.Liens)
            return "background-color: #fff3e0;";

        // Highlight overdue items in light red
        if (item.DueDate.HasValue && item.DueDate.Value < DateTime.Today && item.PaidDate == null)
            return "background-color: #ffebee;";

        // Highlight items in pay in light green
        if (item.SsrInPay == "Yes")
            return "background-color: #e8f5e9;";

        // Highlight selected row
        if (_selectedItem?.AcquisitionID == item.AcquisitionID)
            return "background-color: #e3f2fd;";

        return string.Empty;
    }

    /// <summary>
    /// Navigate to Letter Agreements page
    /// </summary>
    private void GotoLetterAgreements()
    {
        Navigation.NavigateTo("/letteragreements");
    }

    /// <summary>
    /// Apply a named filter from the dropdown
    /// </summary>
    private async Task ApplyFilter(string filterName)
    {
        _activeFilter = filterName;
        if (_dataGrid != null)
        {
            await _dataGrid.ReloadServerData();
        }
        Snackbar.Add($"Filter applied: {filterName}", Severity.Info);
    }

    /// <summary>
    /// Reset all filters to default
    /// </summary>
    private async Task ResetFilters()
    {
        _activeFilter = "All Records";
        if (_dataGrid != null)
        {
            await _dataGrid.ReloadServerData();
        }
        Snackbar.Add("Filters reset", Severity.Info);
    }

    /// <summary>
    /// Reset sort order to default (AcquisitionID desc)
    /// </summary>
    private async Task ResetSort()
    {
        if (_dataGrid != null)
        {
            await _dataGrid.ReloadServerData();
        }
        Snackbar.Add("Sort order reset", Severity.Info);
    }

    /// <summary>
    /// Navigate to new acquisition form
    /// </summary>
    private void AddNewAcquisition()
    {
        Navigation.NavigateTo("/acquisition/new");
    }

    /// <summary>
    /// Navigate to edit form for selected acquisition
    /// </summary>
    private void EditSelectedAcquisition()
    {
        if (_selectedItem != null)
        {
            Navigation.NavigateTo($"/acquisition/edit/{_selectedItem.AcquisitionID}");
        }
        else
        {
            Snackbar.Add("Please select an acquisition to edit", Severity.Warning);
        }
    }

    /// <summary>
    /// Copy selected acquisition using service
    /// </summary>
    /// <summary>
    /// Navigate to copy form for selected acquisition
    /// </summary>
    private void CopySelectedAcquisition()
    {
        if (_selectedItem != null)
        {
            Navigation.NavigateTo($"/acquisition/copy/{_selectedItem.AcquisitionID}");
        }
        else
        {
            Snackbar.Add("Please select an acquisition to copy", Severity.Warning);
        }
    }

    /// <summary>
    /// Delete selected acquisition using service
    /// </summary>
    private async Task DeleteSelectedAcquisition()
    {
        if (_selectedItem == null)
        {
            Snackbar.Add("Please select an acquisition to delete", Severity.Warning);
            return;
        }

        var confirm = await DialogService.ShowMessageBox(
            "Delete Acquisition",
            $"Are you sure you want to delete Acquisition #{_selectedItem.AcquisitionID}? This action cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel",
            options: new DialogOptions { CloseButton = true });

        if (confirm == true)
        {
            try
            {
                await AcquisitionService.DeleteAsync(_selectedItem.AcquisitionID);
                Snackbar.Add($"Acquisition #{_selectedItem.AcquisitionID} deleted", Severity.Success);
                _selectedItem = null;

                if (_dataGrid != null)
                {
                    await _dataGrid.ReloadServerData();
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error deleting acquisition: {ex.Message}", Severity.Error);
            }
        }
    }

    /// <summary>
    /// Show barcode cover sheet generation dialog
    /// </summary>
    private void GenerateBarcodeCoverSheet()
    {
        if (_selectedItem == null)
        {
            Snackbar.Add("Please select an acquisition", Severity.Warning);
            return;
        }

        _showBarcodeDialog = true;
    }

    /// <summary>
    /// Generate and open barcode cover sheet
    /// </summary>
    private async Task ConfirmGenerateBarcode()
    {
        if (_selectedItem == null) return;

        try
        {
            _showBarcodeDialog = false;

            // Get the URL for the barcode document from service
            var fileUrl = await AcquisitionService.GenerateBarcodeCoverSheetAsync(_selectedItem.AcquisitionID);

            // Open in new window
            await JSRuntime.InvokeVoidAsync("open", fileUrl, "_blank");

            Snackbar.Add("Cover sheet generated", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error generating cover sheet: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Export current grid data to Excel
    /// </summary>
    private async Task ExportToExcel()
    {
        try
        {
            Snackbar.Add("Preparing export...", Severity.Info);

            var fileBytes = await AcquisitionService.ExportToExcelAsync(_activeFilter);

            if (fileBytes.Length == 0)
            {
                Snackbar.Add("No data to export or export not implemented", Severity.Warning);
                return;
            }

            // Trigger download via JS interop
            var fileName = $"MineralAcquisitions_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
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
}