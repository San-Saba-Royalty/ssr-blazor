using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using SSRBusiness.Entities;
using SSRBlazor.Models;
using SSRBlazor.Services;

namespace SSRBlazor.Components.Pages.Reports;

public partial class ReportIndex : ComponentBase
{
    #region Injected Services

    [Inject] private ReportService ReportService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    #endregion

    #region State

    private Dictionary<string, List<Report>> _reportsByCategory = new();
    private Report? _selectedReport;
    private ReportParameters _parameters = new();
    private ReportFilterLookups _lookups = new();
    
    private bool _showSelectionCriteria;
    private bool _isGenerating;
    private string? _displayMessage;
    private List<string> _validationErrors = new();

    // Multi-select backing fields (MudSelect requires IEnumerable<T>)
    private IEnumerable<int> _selectedCountyIds = new HashSet<int>();
    private IEnumerable<int> _selectedOperatorIds = new HashSet<int>();
    private IEnumerable<int> _selectedBuyerIds = new HashSet<int>();
    private IEnumerable<int> _selectedDealStatusIds = new HashSet<int>();
    private IEnumerable<int> _selectedLandmanIds = new HashSet<int>();
    private IEnumerable<int> _selectedFieldLandmanIds = new HashSet<int>();

    #endregion

    #region Lifecycle

    protected override async Task OnInitializedAsync()
    {
        await LoadReportsAsync();
    }

    #endregion

    #region Data Loading

    private async Task LoadReportsAsync()
    {
        try
        {
            _reportsByCategory = await ReportService.GetReportsByCategoryAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading reports: {ex.Message}", Severity.Error);
        }
    }

    private async Task LoadLookupsAsync()
    {
        try
        {
            _lookups = await ReportService.GetAllLookupsAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading filter options: {ex.Message}", Severity.Error);
        }
    }

    #endregion

    #region Multi-Select Handlers

    private void OnCountySelectionChanged(IEnumerable<int> values)
    {
        _selectedCountyIds = values;
        _parameters.SelectedCountyIds = values.ToList();
    }

    private void OnOperatorSelectionChanged(IEnumerable<int> values)
    {
        _selectedOperatorIds = values;
        _parameters.SelectedOperatorIds = values.ToList();
    }

    private void OnBuyerSelectionChanged(IEnumerable<int> values)
    {
        _selectedBuyerIds = values;
        _parameters.SelectedBuyerIds = values.ToList();
    }

    private void OnDealStatusSelectionChanged(IEnumerable<int> values)
    {
        _selectedDealStatusIds = values;
        _parameters.SelectedDealStatusIds = values.ToList();
    }

    private void OnLandmanSelectionChanged(IEnumerable<int> values)
    {
        _selectedLandmanIds = values;
        _parameters.SelectedLandmanIds = values.ToList();
    }

    private void OnFieldLandmanSelectionChanged(IEnumerable<int> values)
    {
        _selectedFieldLandmanIds = values;
        _parameters.SelectedFieldLandmanIds = values.ToList();
    }

    #endregion

    #region Report Selection

    private async Task SelectReport(Report report)
    {
        _selectedReport = report;
        _parameters = new ReportParameters { ReportID = report.ReportID };
        _showSelectionCriteria = false;
        _validationErrors.Clear();
        
        // Reset multi-select backing fields
        _selectedCountyIds = new HashSet<int>();
        _selectedOperatorIds = new HashSet<int>();
        _selectedBuyerIds = new HashSet<int>();
        _selectedDealStatusIds = new HashSet<int>();
        _selectedLandmanIds = new HashSet<int>();
        _selectedFieldLandmanIds = new HashSet<int>();
        
        // Load lookup data for filters
        await LoadLookupsAsync();
    }

    private void ClearReportSelection()
    {
        _selectedReport = null;
        _parameters = new ReportParameters();
        _showSelectionCriteria = false;
        _validationErrors.Clear();
    }

    #endregion

    #region Report Generation

    private async Task GenerateReport()
    {
        if (_selectedReport == null) return;

        // Validate parameters
        _validationErrors = ReportService.ValidateParameters(_parameters);
        if (_validationErrors.Any())
        {
            return;
        }

        _isGenerating = true;
        StateHasChanged();

        try
        {
            // Get report URL
            var reportUrl = ReportService.GetReportUrl(_selectedReport, _parameters);

            // Open in new window (like original OpenReportWindow JavaScript) or navigate
            if (reportUrl.StartsWith("/"))
            {
                Navigation.NavigateTo(reportUrl);
            }
            else
            {
                await OpenReportWindow(reportUrl);
            }

            _displayMessage = "Report generated successfully";
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error generating report: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isGenerating = false;
        }
    }

    private async Task OpenReportWindow(string reportUrl)
    {
        // Calculate window size (80% of screen, minimum 700x500)
        var script = @"
            (function(url) {
                var winwidth = screen.width * 0.80;
                var winheight = screen.height * 0.80;
                if (winwidth < 700) winwidth = 700;
                if (winheight < 500) winheight = 500;
                var params = 'scrollbars=yes,toolbar=no,location=no,directories=no,menubar=no,resizable=yes,left=0,top=0,height=' + winheight + ',width=' + winwidth;
                var win = window.open(url, 'ReportWindow', params);
                if (win) win.focus();
            })('" + reportUrl + "');";

        await JS.InvokeVoidAsync("eval", script);
    }

    private void ResetFilters()
    {
        if (_selectedReport != null)
        {
            _parameters = new ReportParameters { ReportID = _selectedReport.ReportID };
            _validationErrors.Clear();
            
            // Reset multi-select backing fields
            _selectedCountyIds = new HashSet<int>();
            _selectedOperatorIds = new HashSet<int>();
            _selectedBuyerIds = new HashSet<int>();
            _selectedDealStatusIds = new HashSet<int>();
            _selectedLandmanIds = new HashSet<int>();
            _selectedFieldLandmanIds = new HashSet<int>();
        }
    }

    #endregion
}