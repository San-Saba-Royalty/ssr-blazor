using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using SSRBlazor.Services;
using SSRBusiness.Entities;

namespace SSRBlazor.Components.Pages.Counties;

public partial class CountyIndex
{
    #region Injected Services

    [Inject] private CountyService CountyService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    #endregion

    #region State

    // Grid
    private MudDataGrid<County>? _dataGrid;
    private County? _selectedCounty;
    private Dictionary<string, string> _columnFilters = new();

    // Messages
    private string? _displayMessage;
    private string? _errorMessage;

    // Context Menu
    private MudMenu? _contextMenu;

    // Delete Dialog
    private bool _showDeleteDialog;
    private bool _hasAssociatedAcquisitions;
    private DialogOptions _dialogOptions = new() 
    { 
        CloseButton = true, 
        MaxWidth = MaxWidth.Small, 
        FullWidth = true 
    };

    #endregion

    #region Grid Data Loading

    private async Task<GridData<County>> LoadServerData(GridState<County> state)
    {
        try
        {
            return await CountyService.GetCountiesPagedAsync(state, _columnFilters);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading counties: {ex.Message}";
            return new GridData<County> { Items = new List<County>(), TotalItems = 0 };
        }
    }

    #endregion

    #region Row Events

    private void OnRowClick(DataGridRowClickEventArgs<County> args)
    {
        _selectedCounty = args.Item;

        // Double-click to edit
        if (args.MouseEventArgs.Detail == 2)
        {
            EditSelectedCounty();
        }

        // Right-click for context menu
        if (args.MouseEventArgs.Button == 2)
        {
            // Context menu is handled by MudMenu
        }
    }

    private void OnSelectedItemChanged(County? item)
    {
        _selectedCounty = item;
    }

    private string RowStyleFunc(County item, int index)
    {
        if (_selectedCounty != null && item.CountyID == _selectedCounty.CountyID)
        {
            return "background-color: #e3f2fd;"; // Light blue for selected
        }
        return string.Empty;
    }

    #endregion

    #region CRUD Operations

    private void AddNewCounty()
    {
        Navigation.NavigateTo("/county/edit");
    }

    private void EditSelectedCounty()
    {
        if (_selectedCounty == null)
        {
            Snackbar.Add("Please select a county to edit", Severity.Warning);
            return;
        }

        Navigation.NavigateTo($"/county/edit/{_selectedCounty.CountyID}");
    }

    private async Task DeleteSelectedCounty()
    {
        if (_selectedCounty == null)
        {
            Snackbar.Add("Please select a county to delete", Severity.Warning);
            return;
        }

        // Check for associated acquisitions
        _hasAssociatedAcquisitions = await CountyService.HasAssociatedAcquisitionsAsync(_selectedCounty.CountyID);
        _showDeleteDialog = true;
    }

    private async Task ConfirmDelete()
    {
        if (_selectedCounty == null) return;

        try
        {
            var result = await CountyService.DeleteAsync(_selectedCounty.CountyID);

            if (result.Success)
            {
                _displayMessage = $"County '{_selectedCounty.CountyName}' deleted successfully";
                _selectedCounty = null;
                await _dataGrid!.ReloadServerData();
            }
            else
            {
                _errorMessage = result.Error ?? "Failed to delete county";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error deleting county: {ex.Message}";
        }
        finally
        {
            _showDeleteDialog = false;
        }
    }

    #endregion

    #region Filter and Sort

    private async Task ResetFilters()
    {
        _columnFilters.Clear();
        
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
            // Clear sort definitions
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
            
            var bytes = await CountyService.ExportToExcelAsync(_columnFilters);
            var fileName = $"Counties_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            
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

    private static string GetFullAddress(County county)
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(county.AddressLine1))
            parts.Add(county.AddressLine1);
        if (!string.IsNullOrWhiteSpace(county.AddressLine2))
            parts.Add(county.AddressLine2);
            
        return string.Join(", ", parts);
    }

    #endregion
}