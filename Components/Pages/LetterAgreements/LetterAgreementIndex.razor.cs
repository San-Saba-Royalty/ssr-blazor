using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using MudBlazor;
using SSRBusiness.Entities;
using SSRBlazor.Services;
using SSRBlazor.Components.Pages.LetterAgreements.Models;
using SSRBlazor.Models;
using System.Security.Claims;

namespace SSRBlazor.Components.Pages.LetterAgreements;

public partial class LetterAgreementIndex : ComponentBase
{
    [Inject] private LetterAgreementService LetterAgreementService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private ViewCacheService ViewCacheService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    // Grid reference
    private MudDataGrid<LetterAgreementViewModel>? _dataGrid;
    private MudMenu? _contextMenu;

    // State
    private LetterAgreementViewModel? _selectedItem;
    private string _displayMessage = string.Empty;
    private string _errorMessage = string.Empty;
    private int _totalCount = 0;
    private bool _showCopyDialog = false;
    private bool _showDeleteDialog = false;
    private bool _showColumnChooser = false;

    // Column visibility state
    private ColumnVisibilityState _columnVisibility = new();
    private bool _isLoadingPreferences = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadUserViewPreferenceAsync();
        _isLoadingPreferences = false;
    }

    /// <summary>
    /// Server-side data loading for the grid
    /// </summary>
    private async Task<GridData<LetterAgreementViewModel>> LoadServerData(GridState<LetterAgreementViewModel> state)
    {
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
            var result = await LetterAgreementService.GetLetterAgreementsPagedAsync(
                page: state.Page,
                pageSize: state.PageSize,
                sortDefinitions: sortDefinitions,
                filterDefinitions: filterDefinitions
            );

            _totalCount = result.TotalCount;

            return new GridData<LetterAgreementViewModel>
            {
                Items = result.Items,
                TotalItems = result.TotalCount
            };
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading data: {ex.Message}", Severity.Error);
            return new GridData<LetterAgreementViewModel>
            {
                Items = Enumerable.Empty<LetterAgreementViewModel>(),
                TotalItems = 0
            };
        }
    }

    /// <summary>
    /// Handle row click for selection, double-click to edit
    /// </summary>
    private void OnRowClick(DataGridRowClickEventArgs<LetterAgreementViewModel> args)
    {
        _selectedItem = args.Item;

        // Double-click navigates to edit (matching OnRowDblClick behavior)
        if (args.MouseEventArgs.Detail == 2)
        {
            EditLetterAgreement(args.Item);
        }
    }

    /// <summary>
    /// Row styling function
    /// </summary>
    private string RowStyleFunc(LetterAgreementViewModel item, int index)
    {
        // Highlight selected row
        if (_selectedItem?.LetterAgreementID == item.LetterAgreementID)
            return "background-color: #e3f2fd;";

        return string.Empty;
    }

    /// <summary>
    /// Handle column visibility changes
    /// </summary>
    private void OnHiddenColumnChanged(bool hidden)
    {
        // This is called when a column's visibility changes
        // Can be used to persist column visibility preferences
    }

    /// <summary>
    /// Navigate to add new letter agreement
    /// </summary>
    private void AddNewLetterAgreement()
    {
        Navigation.NavigateTo("/letteragreement/edit");
    }

    /// <summary>
    /// Navigate to edit letter agreement
    /// </summary>
    private void EditLetterAgreement(LetterAgreementViewModel letterAgreement)
    {
        Navigation.NavigateTo($"/letteragreement/edit/{letterAgreement.LetterAgreementID}");
    }

    /// <summary>
    /// Edit selected letter agreement
    /// </summary>
    private void EditSelectedLetterAgreement()
    {
        if (_selectedItem != null)
        {
            EditLetterAgreement(_selectedItem);
        }
        else
        {
            Snackbar.Add("Please select a letter agreement to edit", Severity.Warning);
        }
    }

    /// <summary>
    /// Show copy confirmation dialog
    /// </summary>
    private void CopySelectedLetterAgreement()
    {
        if (_selectedItem == null)
        {
            Snackbar.Add("Please select a letter agreement to copy", Severity.Warning);
            return;
        }

        _showCopyDialog = true;
    }

    /// <summary>
    /// Confirm and execute copy
    /// </summary>
    private async Task ConfirmCopy()
    {
        if (_selectedItem == null) return;

        try
        {
            var newId = await LetterAgreementService.CopyAsync(_selectedItem.LetterAgreementID);

            _showCopyDialog = false;

            Snackbar.Add($"Letter agreement copied successfully. New ID: {newId}", Severity.Success);

            // Navigate to edit the new copy
            Navigation.NavigateTo($"/letteragreement/edit/{newId}");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error copying letter agreement: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Show delete confirmation dialog
    /// </summary>
    private void DeleteSelectedLetterAgreement()
    {
        if (_selectedItem == null)
        {
            Snackbar.Add("Please select a letter agreement to delete", Severity.Warning);
            return;
        }

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
            await LetterAgreementService.DeleteAsync(_selectedItem.LetterAgreementID);

            Snackbar.Add("Letter agreement deleted successfully", Severity.Success);

            _showDeleteDialog = false;
            _selectedItem = null;

            if (_dataGrid != null)
            {
                await _dataGrid.ReloadServerData();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error deleting letter agreement: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Navigate to Mineral Acquisitions page
    /// </summary>
    private void NavigateToAcquisitions()
    {
        Navigation.NavigateTo("/acquisitions");
    }

    /// <summary>
    /// Reset all filters
    /// </summary>
    private async Task ResetFilters()
    {
        _selectedItem = null;

        if (_dataGrid != null)
        {
            await _dataGrid.ReloadServerData();
        }

        Snackbar.Add("Filters reset", Severity.Info);
    }

    /// <summary>
    /// Reset sort order
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
    /// Toggle column chooser dialog
    /// </summary>
    private void ToggleColumnChooser()
    {
        _showColumnChooser = !_showColumnChooser;
    }

    /// <summary>
    /// Reset column visibility to defaults
    /// </summary>
    private void ResetColumnVisibility()
    {
        _columnVisibility = new ColumnVisibilityState();
    }

    /// <summary>
    /// Apply column visibility changes and save to database
    /// </summary>
    private async Task ApplyColumnVisibility()
    {
        _showColumnChooser = false;

        try
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Convert ColumnVisibilityState to ViewConfiguration
                var viewConfig = await MapColumnVisibilityToViewConfigAsync(_columnVisibility);

                // Save via cache service (which persists to DB and updates cache)
                await ViewCacheService.SaveUserViewPreferenceAsync(
                    userId,
                    "LetterAgreementIndex",
                    viewConfig
                );

                Snackbar.Add("Column preferences saved", Severity.Success);
            }
            else
            {
                Snackbar.Add("Column visibility updated (not saved - no user)", Severity.Info);
            }

            // Force grid to re-render with new column visibility
            if (_dataGrid != null)
            {
                await _dataGrid.ReloadServerData();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error saving column preferences: {ex.Message}";
            Snackbar.Add("Error saving column preferences", Severity.Error);
        }
    }

    /// <summary>
    /// Load user's view preference from cache
    /// </summary>
    private async Task LoadUserViewPreferenceAsync()
    {
        try
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                var viewConfig = await ViewCacheService.GetUserViewForPageAsync(
                    userId,
                    "LetterAgreementIndex",
                    "LetterAgreement"
                );

                if (viewConfig != null)
                {
                    // Map ViewConfiguration fields to ColumnVisibilityState
                    _columnVisibility = MapViewConfigToColumnVisibility(viewConfig);
                }
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading column preferences: {ex.Message}";
        }
    }

    /// <summary>
    /// Map ViewConfiguration to ColumnVisibilityState
    /// </summary>
    private ColumnVisibilityState MapViewConfigToColumnVisibility(ViewConfiguration viewConfig)
    {
        var visibility = new ColumnVisibilityState();
        var selectedFields = viewConfig.Fields.Where(f => f.IsSelected).Select(f => f.FieldName).ToHashSet();

        visibility.SellerLastName = selectedFields.Contains("SellerLastName");
        visibility.SellerName = selectedFields.Contains("SellerName");
        visibility.SellerEmail = selectedFields.Contains("SellerEmail");
        visibility.SellerPhone = selectedFields.Contains("SellerPhone");
        visibility.SellerCity = selectedFields.Contains("SellerCity");
        visibility.SellerState = selectedFields.Contains("SellerState");
        visibility.SellerZipCode = selectedFields.Contains("SellerZipCode");
        visibility.CreatedOn = selectedFields.Contains("CreatedOn");
        visibility.EffectiveDate = selectedFields.Contains("EffectiveDate");
        visibility.TotalBonus = selectedFields.Contains("TotalBonus");
        visibility.ConsiderationFee = selectedFields.Contains("ConsiderationFee");
        visibility.ReferralFee = selectedFields.Contains("ReferralFee");
        visibility.TotalGrossAcres = selectedFields.Contains("TotalGrossAcres");
        visibility.TotalNetAcres = selectedFields.Contains("TotalNetAcres");
        visibility.LandMan = selectedFields.Contains("LandMan");
        visibility.DealStatus = selectedFields.Contains("DealStatus");
        visibility.CountyName = selectedFields.Contains("CountyName");
        visibility.OperatorName = selectedFields.Contains("OperatorName");
        visibility.UnitName = selectedFields.Contains("UnitName");

        return visibility;
    }

    /// <summary>
    /// Map ColumnVisibilityState to ViewConfiguration
    /// </summary>
    private async Task<ViewConfiguration> MapColumnVisibilityToViewConfigAsync(ColumnVisibilityState visibility)
    {
        // Get all DisplayFields for LetterAgreement module from cache
        var allFields = await ViewCacheService.GetDisplayFieldsForModuleAsync("LetterAgreement");

        var viewConfig = new ViewConfiguration
        {
            Module = "LetterAgreement",
            ViewName = "User Custom View",
            Fields = allFields.Select((field, index) => new ViewFieldSelection
            {
                FieldID = field.FieldID,
                FieldName = field.FieldName,
                DisplayName = field.ColumnName,
                IsSelected = IsFieldSelected(field.FieldName, visibility),
                DisplayOrder = index + 1
            }).ToList()
        };

        return viewConfig;
    }

    /// <summary>
    /// Check if a field is selected based on column visibility state
    /// </summary>
    private bool IsFieldSelected(string fieldName, ColumnVisibilityState visibility)
    {
        return fieldName switch
        {
            "LetterAgreementID" => true, // Always visible
            "BankingDays" => true, // Always visible
            "AcquisitionID" => true, // Always visible
            "SellerLastName" => visibility.SellerLastName,
            "SellerName" => visibility.SellerName,
            "SellerEmail" => visibility.SellerEmail,
            "SellerPhone" => visibility.SellerPhone,
            "SellerCity" => visibility.SellerCity,
            "SellerState" => visibility.SellerState,
            "SellerZipCode" => visibility.SellerZipCode,
            "CreatedOn" => visibility.CreatedOn,
            "EffectiveDate" => visibility.EffectiveDate,
            "TotalBonus" => visibility.TotalBonus,
            "ConsiderationFee" => visibility.ConsiderationFee,
            "ReferralFee" => visibility.ReferralFee,
            "TotalGrossAcres" => visibility.TotalGrossAcres,
            "TotalNetAcres" => visibility.TotalNetAcres,
            "LandMan" => visibility.LandMan,
            "DealStatus" => visibility.DealStatus,
            "CountyName" => visibility.CountyName,
            "OperatorName" => visibility.OperatorName,
            "UnitName" => visibility.UnitName,
            _ => false
        };
    }

    /// <summary>
    /// Export to Excel
    /// </summary>
    private async Task ExportToExcel()
    {
        try
        {
            Snackbar.Add("Preparing export...", Severity.Info);

            var fileBytes = await LetterAgreementService.ExportToExcelAsync();

            if (fileBytes.Length == 0)
            {
                Snackbar.Add("No data to export or export not implemented", Severity.Warning);
                return;
            }

            var fileName = $"LetterAgreements_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
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

    #region Column Visibility State

    /// <summary>
    /// Tracks visibility state for all hideable columns
    /// </summary>
    private class ColumnVisibilityState
    {
        // Seller info (all hidden by default)
        public bool SellerLastName { get; set; } = false;
        public bool SellerName { get; set; } = false;
        public bool SellerEmail { get; set; } = false;
        public bool SellerPhone { get; set; } = false;
        public bool SellerCity { get; set; } = false;
        public bool SellerState { get; set; } = false;
        public bool SellerZipCode { get; set; } = false;

        // Dates (all hidden by default)
        public bool CreatedOn { get; set; } = false;
        public bool EffectiveDate { get; set; } = false;

        // Financial (all hidden by default)
        public bool TotalBonus { get; set; } = false;
        public bool ConsiderationFee { get; set; } = false;
        public bool ReferralFee { get; set; } = false;

        // Acreage (all hidden by default)
        public bool TotalGrossAcres { get; set; } = false;
        public bool TotalNetAcres { get; set; } = false;

        // Other (all hidden by default)
        public bool LandMan { get; set; } = false;
        public bool DealStatus { get; set; } = false;
        public bool CountyName { get; set; } = false;
        public bool OperatorName { get; set; } = false;
        public bool UnitName { get; set; } = false;
    }

    #endregion
}
