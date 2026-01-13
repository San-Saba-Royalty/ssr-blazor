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
    private County? _selectedCounty;
    private Dictionary<string, string> _columnFilters = new();
    private string? _errorMessage;
    private DialogOptions _dialogOptions = new() { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
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
        if (args.MouseEventArgs.Detail == 2)
        {
            EditSelectedCounty();
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
            return "background-color: #e3f2fd;"; 
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
    #endregion

}