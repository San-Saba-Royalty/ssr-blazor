using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using SSRBusiness.Entities;
using SSRBlazor.Services;
using SSRBlazor.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace SSRBlazor.Components.Pages.Operators;

public partial class OperatorIndex : ComponentBase
{
    #region Injected Services

    [Inject] private OperatorService OperatorService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ViewService ViewService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    #endregion

    #region State

    // Grid
    private MudDataGrid<Operator>? _dataGrid;
    private Operator? _selectedItem;
    private Dictionary<string, string> _columnFilters = new();
    private int _totalCount = 0;
    private bool _isInitialized = false;

    // View State
    private int? _selectedViewId;
    private List<View> _availableViews = new();
    private string _activeView = "Default";
    private HashSet<string> _visibleColumns = new();

    // Messages
    private string? _displayMessage = string.Empty;
    private string? _errorMessage = string.Empty;

    // Context Menu
    private MudMenu? _contextMenu;

    // Delete Dialog
    private bool _showDeleteDialog;
    private bool _hasAssociatedAcquisitions;

    // Check Statement Dialog
    private bool _showCheckStatementDialog;
    private bool _isGeneratingCheckStatement;
    private string? _checkStatementUrl;

    #endregion

    #region Grid Data Loading

    protected override async Task OnInitializedAsync()
    {
        await LoadViewsAsync();

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            _selectedViewId = await ViewService.GetUserDefaultViewAsync(userId, "OperatorIndex");
            if (_selectedViewId.HasValue)
            {
                var view = _availableViews.FirstOrDefault(v => v.ViewID == _selectedViewId.Value);
                if (view != null)
                {
                    _activeView = view.ViewName ?? "Default";
                    await LoadDataWithViewAsync(_selectedViewId.Value);
                    _isInitialized = true;
                    return;
                }
            }
        }

        InitializeDefaultLegacyView();
        _isInitialized = true;
    }

    private void InitializeDefaultLegacyView()
    {
        _visibleColumns = new HashSet<string>
        {
            "OperatorName", "ContactName", "Phone", "Email", "City", "State"
        };
    }

    private async Task<GridData<Operator>> LoadServerData(GridState<Operator> state)
    {
        if (!_isInitialized) return new GridData<Operator> { Items = new List<Operator>(), TotalItems = 0 };

        try
        {
            var result = await OperatorService.GetOperatorsPagedAsync(state, _columnFilters);
            _totalCount = result.TotalItems;
            return result;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading operators: {ex.Message}";
            return new GridData<Operator> { Items = new List<Operator>(), TotalItems = 0 };
        }
    }

    private async Task LoadViewsAsync()
    {
        _availableViews = await ViewService.GetAllViewsAsync("Operator");
    }

    private async Task OnViewSelected(int? viewId)
    {
        _selectedViewId = viewId;

        if (viewId.HasValue)
        {
            var view = _availableViews.FirstOrDefault(v => v.ViewID == viewId.Value);
            _activeView = view?.ViewName ?? "Default";
        }
        else
        {
            _activeView = "Default";
            InitializeDefaultLegacyView();
        }

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId) && viewId.HasValue)
        {
            await ViewService.SetUserDefaultViewAsync(userId, viewId.Value, "OperatorIndex");
        }

        await LoadDataWithViewAsync(viewId);
    }

    private async Task LoadDataWithViewAsync(int? viewId)
    {
        if (!viewId.HasValue)
        {
            if (_dataGrid != null) await _dataGrid.ReloadServerData();
            return;
        }

        var viewConfig = await ViewService.GetViewConfigurationAsync(viewId.Value);
        if (viewConfig != null)
        {
            _visibleColumns = new HashSet<string>(
                viewConfig.Fields.Where(f => f.IsSelected).Select(f => f.FieldName)
            );
        }

        if (_dataGrid != null) await _dataGrid.ReloadServerData();
    }

    private bool IsColumnVisible(string columnName)
    {
        if (_activeView == "All Fields" || _visibleColumns.Count == 0) return true;
        return _visibleColumns.Contains(columnName);
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

        var dialog = DialogService.Show<SSRBlazor.Components.Dialogs.ColumnOrderingDialog>("Column Ordering", parameters);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is List<ViewFieldSelection> orderedFields)
        {
            viewConfig.Fields = orderedFields;
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

    #endregion

    #region Row Events

    private void OnRowClick(DataGridRowClickEventArgs<Operator> args)
    {
        _selectedItem = args.Item;

        // Double-click to edit
        if (args.MouseEventArgs.Detail == 2)
        {
            EditSelectedOperator();
        }
    }

    private string RowStyleFunc(Operator item, int index)
    {
        if (_selectedItem != null && item.OperatorID == _selectedItem.OperatorID)
        {
            return "background-color: #e3f2fd;"; // Light blue for selected
        }
        return string.Empty;
    }

    #endregion

    #region CRUD Operations

    private void AddNewOperator()
    {
        Navigation.NavigateTo("/operator/edit");
    }

    private void EditSelectedOperator()
    {
        if (_selectedItem == null)
        {
            Snackbar.Add("Please select an operator to edit", Severity.Warning);
            return;
        }

        Navigation.NavigateTo($"/operator/edit/{_selectedItem.OperatorID}");
    }

    private async Task DeleteSelectedOperator()
    {
        if (_selectedItem == null)
        {
            Snackbar.Add("Please select an operator to delete", Severity.Warning);
            return;
        }

        // Check for associated acquisitions
        _hasAssociatedAcquisitions = await OperatorService.HasAssociatedAcquisitionsAsync(_selectedItem.OperatorID);
        _showDeleteDialog = true;
    }

    private async Task ConfirmDelete()
    {
        if (_selectedItem == null) return;

        try
        {
            var result = await OperatorService.DeleteAsync(_selectedItem.OperatorID);

            if (result.Success)
            {
                _displayMessage = $"Operator '{_selectedItem.OperatorName}' deleted successfully";
                _selectedItem = null;
                await _dataGrid!.ReloadServerData();
            }
            else
            {
                _errorMessage = result.Error ?? "Failed to delete operator";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error deleting operator: {ex.Message}";
        }
        finally
        {
            _showDeleteDialog = false;
        }
    }

    #endregion

    #region Check Statement

    private void OpenCheckStatement()
    {
        if (_selectedItem == null)
        {
            Snackbar.Add("Please select an operator", Severity.Warning);
            return;
        }

        _checkStatementUrl = null;
        _isGeneratingCheckStatement = false;
        _showCheckStatementDialog = true;
    }

    private async Task GenerateCheckStatement()
    {
        if (_selectedItem == null) return;

        try
        {
            _isGeneratingCheckStatement = true;
            StateHasChanged();

            var result = await OperatorService.GenerateCheckStatementAsync(_selectedItem.OperatorID);

            if (result.Success && !string.IsNullOrEmpty(result.FilePath))
            {
                _checkStatementUrl = result.FilePath;
                Snackbar.Add("Check statement generated successfully", Severity.Success);
            }
            else
            {
                _errorMessage = result.Error ?? "Failed to generate check statement";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error generating check statement: {ex.Message}";
        }
        finally
        {
            _isGeneratingCheckStatement = false;
        }
    }

    private async Task DownloadCheckStatement()
    {
        if (!string.IsNullOrEmpty(_checkStatementUrl))
        {
            await JS.InvokeVoidAsync("open", _checkStatementUrl, "_blank");
        }
    }

    private void CloseCheckStatementDialog()
    {
        _showCheckStatementDialog = false;
        _checkStatementUrl = null;
        _isGeneratingCheckStatement = false;
    }

    #endregion

    #region Filter and Sort

    private async Task ResetFilters()
    {
        _columnFilters.Clear();
        _selectedItem = null;

        if (_dataGrid != null)
        {
            await _dataGrid.ClearFiltersAsync();
            await _dataGrid.ReloadServerData();
        }

        Snackbar.Add("Filters reset", Severity.Info);
    }

    private async Task ResetSort()
    {
        if (_dataGrid != null)
        {
            foreach (var column in _dataGrid.RenderedColumns)
            {
                await _dataGrid.RemoveSortAsync(column.PropertyName);
            }
            await _dataGrid.ReloadServerData();
        }

        Snackbar.Add("Sort reset", Severity.Info);
    }

    #endregion

    #region Export

    private async Task ExportToExcel()
    {
        try
        {
            Snackbar.Add("Generating Excel file...", Severity.Info);

            var bytes = await OperatorService.ExportToExcelAsync(_columnFilters);
            var fileName = $"Operators_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            // Download via JS
            var base64 = Convert.ToBase64String(bytes);
            await JS.InvokeVoidAsync("downloadFile", fileName, base64, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            Snackbar.Add("Excel file downloaded", Severity.Success);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error exporting to Excel: {ex.Message}";
        }
    }

    #endregion

    #region Helpers

    private static string GetFullAddress(Operator operatorEntity)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(operatorEntity.AddressLine1))
            parts.Add(operatorEntity.AddressLine1);
        if (!string.IsNullOrWhiteSpace(operatorEntity.AddressLine2))
            parts.Add(operatorEntity.AddressLine2);

        return string.Join(", ", parts);
    }

    #endregion
}