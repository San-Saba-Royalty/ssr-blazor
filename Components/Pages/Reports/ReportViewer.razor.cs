#pragma warning disable CS8601, CS8629
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using MudBlazor;
using SSRBlazor.Models;
using SSRBlazor.Services;
using SSRBusiness.Entities;

namespace SSRBlazor.Components.Pages.Reports;

public partial class ReportViewer : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ReportService ReportService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private bool _isLoading = true;
    private string? _errorMessage;
    private byte[]? _reportData;
    private string? _contentType;
    private string? _fileName;
    private string? _dataUrl;

    private bool _isPdf => _contentType == "application/pdf";
    private bool _isHtml => _contentType == "text/html";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            var query = QueryHelpers.ParseQuery(uri.Query);

            if (!query.TryGetValue("reportId", out var reportIdVal) || !int.TryParse(reportIdVal, out var reportId))
            {
                _errorMessage = "Report ID parameter is missing or invalid.";
                _isLoading = false;
                return;
            }

            var report = await ReportService.GetByIdAsync(reportId);
            if (report == null)
            {
                _errorMessage = "Report not found.";
                _isLoading = false;
                return;
            }

            var parameters = ParseParameters(query);

            // Special handling for Drafts Due legacy route support if needed, 
            // but ReportService.GetReportUrl standardizes on this viewer now (except specific DraftsDue routing check in Service)
            // If Service routed to /reports/drafts-due, we wouldn't be here. 
            // If Service routed here, we handle it generically.

            var result = await ReportService.GenerateReportAsync(report, parameters);

            if (result.Success)
            {
                _reportData = result.Data;
                _contentType = result.ContentType;
                _fileName = result.FileName;

                if (_reportData != null)
                {
                    var base64 = Convert.ToBase64String(_reportData);
                    _dataUrl = $"data:{_contentType};base64,{base64}";
                }
            }
            else
            {
                _errorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private ReportParameters ParseParameters(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query)
    {
        var p = new ReportParameters();

        if (query.TryGetValue("format", out var format)) p.OutputFormat = format;

        // Multi-selects
        if (query.TryGetValue("counties", out var counties)) p.SelectedCountyIds = ParseIntList(counties);
        if (query.TryGetValue("operators", out var operators)) p.SelectedOperatorIds = ParseIntList(operators);
        if (query.TryGetValue("buyers", out var buyers)) p.SelectedBuyerIds = ParseIntList(buyers);
        if (query.TryGetValue("landmen", out var landmen)) p.SelectedLandmanIds = ParseIntList(landmen);
        if (query.TryGetValue("fieldLandmen", out var fieldLandmen)) p.SelectedFieldLandmanIds = ParseIntList(fieldLandmen);

        if (query.TryGetValue("dealStatuses", out var dealStatuses)) p.SelectedDealStatusIds = ParseIntList(dealStatuses);
        if (query.TryGetValue("dealStatusQueryType", out var dsqt) && int.TryParse(dsqt, out var type)) p.DealStatusQueryType = type;

        // Dates
        ParseDateRange(query, "effectiveDate", p.EffectiveDate);
        ParseDateRange(query, "buyerEffectiveDate", p.BuyerEffectiveDate);
        ParseDateRange(query, "dueDate", p.DueDate);
        ParseDateRange(query, "paidDate", p.PaidDate);
        ParseDateRange(query, "titleOpinionDate", p.TitleOpinionReceivedDate);
        ParseDateRange(query, "closingLetterDate", p.ClosingLetterDate);
        ParseDateRange(query, "deedDate", p.DeedDate);
        ParseDateRange(query, "invoiceDate", p.InvoiceDate);
        ParseDateRange(query, "invoiceDueDate", p.InvoiceDueDate);
        ParseDateRange(query, "invoicePaidDate", p.InvoicePaidDate);

        // Booleans
        ParseBooleanFilter(query, "invoiceNumber", p.InvoiceNumber);
        ParseBooleanFilter(query, "isReferral", p.IsReferral);
        ParseBooleanFilter(query, "hasCuratives", p.HasCuratives);
        ParseBooleanFilter(query, "hasLiens", p.HasLiens);
        ParseBooleanFilter(query, "hasLetterAgreement", p.HasLetterAgreement);

        // Options
        if (query.TryGetValue("sortBy", out var sortBy)) p.SortBy = sortBy;
        if (query.TryGetValue("subtotalBy", out var subtotalBy)) p.SubtotalBy = subtotalBy;

        if (query.TryGetValue("summaryOnly", out var summaryOnly) && bool.TryParse(summaryOnly, out var summaryOnlyVal)) p.SummaryLevelOnly = summaryOnlyVal;
        if (query.TryGetValue("includeNew", out var includeNew) && bool.TryParse(includeNew, out var includeNewVal)) p.IncludeNewAcquisitions = includeNewVal;
        if (query.TryGetValue("includeAmountPaid", out var includeAmountPaid) && bool.TryParse(includeAmountPaid, out var includeAmountPaidVal)) p.IncludeAmountPaidSellerSubtotal = includeAmountPaidVal;
        if (query.TryGetValue("includeNotes", out var includeNotes) && bool.TryParse(includeNotes, out var includeNotesVal)) p.IncludeNotes = includeNotesVal;

        return p;
    }

    private List<int> ParseIntList(string? value)
    {
        if (string.IsNullOrEmpty(value)) return new List<int>();
        return value.Split(',')
            .Select(s => int.TryParse(s, out var i) ? i : (int?)null)
            .Where(i => i.HasValue)
            .Select(i => i.Value)
            .ToList();
    }

    private void ParseDateRange(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query, string prefix, DateRangeFilter filter)
    {
        if (query.TryGetValue($"{prefix}From", out var from) && DateTime.TryParse(from, out var fromDate)) filter.FromDate = fromDate;
        if (query.TryGetValue($"{prefix}To", out var to) && DateTime.TryParse(to, out var toDate)) filter.ToDate = toDate;
        if (query.TryGetValue($"{prefix}IsEmpty", out var isEmpty) && bool.TryParse(isEmpty, out var isEmptyVal)) filter.IsEmpty = isEmptyVal;
        if (query.TryGetValue($"{prefix}IsNotEmpty", out var isNotEmpty) && bool.TryParse(isNotEmpty, out var isNotEmptyVal)) filter.IsNotEmpty = isNotEmptyVal;
    }

    private void ParseBooleanFilter(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query, string prefix, BooleanFilter filter)
    {
        if (query.TryGetValue(prefix, out var valStr) && bool.TryParse(valStr, out var val))
        {
            if (val) filter.IsTrue = true;
            else filter.IsFalse = true;
        }
    }

    private async Task DownloadReport()
    {
        if (_reportData == null || _fileName == null || _contentType == null) return;

        // Use JS to trigger download
        await JS.InvokeVoidAsync("downloadFileFromStream", _fileName, _dataUrl);
    }

    private async Task CloseWindow()
    {
        await JS.InvokeVoidAsync("window.close");
    }
}
