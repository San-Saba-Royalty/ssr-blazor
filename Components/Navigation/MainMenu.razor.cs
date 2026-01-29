using Microsoft.AspNetCore.Components;
using SSRBlazor.Services;

namespace SSRBlazor.Components.Navigation;

public partial class MainMenu : ComponentBase
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IActionService ActionService { get; set; } = default!;

    /// <summary>
    /// Whether to show the toolbar buttons
    /// </summary>
    [Parameter]
    public bool ShowToolbar { get; set; } = true;

    /// <summary>
    /// Current active filter name
    /// </summary>
    [Parameter]
    public string CurrentFilter { get; set; } = "All Records";

    /// <summary>
    /// Current active view name
    /// </summary>
    [Parameter]
    public string CurrentView { get; set; } = "All Fields";

    /// <summary>
    /// List of available filters to display in the Apply Filter submenu
    /// </summary>
    [Parameter]
    public List<string>? AvailableFilters { get; set; }

    /// <summary>
    /// List of available views to display in the Apply View submenu
    /// </summary>
    [Parameter]
    public List<string>? AvailableViews { get; set; }

    /// <summary>
    /// Event callback when a menu action is triggered
    /// </summary>
    [Parameter]
    public EventCallback<string> OnMenuActionTriggered { get; set; }

    /// <summary>
    /// Event callback when a filter is applied
    /// </summary>
    [Parameter]
    public EventCallback<string> OnFilterApplied { get; set; }

    /// <summary>
    /// Event callback when a view is applied
    /// </summary>
    [Parameter]
    public EventCallback<string> OnViewApplied { get; set; }

    /// <summary>
    /// Handle menu action and trigger navigation or callback
    /// </summary>
    private async Task OnMenuAction(string menuValue)
    {
        // Trigger the callback first
        if (OnMenuActionTriggered.HasDelegate)
        {
            await OnMenuActionTriggered.InvokeAsync(menuValue);
        }

        // Handle navigation based on menu value
        switch (menuValue)
        {
            // File > New
            case "MNU_FILE_NEW_ACQUISITION":
                Navigation.NavigateTo("/acquisition/new");
                break;
            case "MNU_FILE_NEW_BUYER":
                Navigation.NavigateTo("/buyer/new");
                break;
            case "MNU_FILE_NEW_COUNTY":
                Navigation.NavigateTo("/county/new");
                break;
            case "MNU_FILE_NEW_OPERATOR":
                Navigation.NavigateTo("/operator/new");
                break;
            case "MNU_FILE_NEW_APPRAISALGROUP":
                Navigation.NavigateTo("/appraisalgroup/new");
                break;
            case "MNU_FILE_NEW_LETTERAGREEMENT":
                Navigation.NavigateTo("/letteragreement/new");
                break;
            case "MNU_FILE_NEW_REFERRER":
                Navigation.NavigateTo("/referrer/new");
                break;

            // File > Display
            case "MNU_FILE_DISPLAY_ACQUISITIONS":
                Navigation.NavigateTo("/acquisitions");
                break;
            case "MNU_FILE_DISPLAY_BUYERS":
                Navigation.NavigateTo("/buyers");
                break;
            case "MNU_FILE_DISPLAY_COUNTIES":
                Navigation.NavigateTo("/counties");
                break;
            case "MNU_FILE_DISPLAY_OPERATORS":
                Navigation.NavigateTo("/operators");
                break;
            case "MNU_FILE_DISPLAY_APPRAISALGROUPS":
                Navigation.NavigateTo("/appraisalgroups");
                break;
            case "MNU_FILE_DISPLAY_LETTERAGREEMENTS":
                Navigation.NavigateTo("/letteragreements");
                break;
            case "MNU_FILE_DISPLAY_REFERRERS":
                Navigation.NavigateTo("/referrers");
                break;

            // File > Other
            case "MNU_FILE_TAX_ROLL_SEARCH":
                Navigation.NavigateTo("/taxrollsearch");
                break;
            case "MNU_FILE_LOGOUT":
                Navigation.NavigateTo("/logout");
                break;

            // Reports
            case "MNU_REPORT_DRAFTS_DUE":
                Navigation.NavigateTo("/reports/drafts-due");
                break;
            case "MNU_REPORT_BUYER_INVOICES_DUE":
                Navigation.NavigateTo("/reports/invoicesdue");
                break;
            case "MNU_REPORT_PURCHASES":
                Navigation.NavigateTo("/reports/purchases");
                break;
            case "MNU_REPORT_CURATIVE_REQUIREMENTS":
                Navigation.NavigateTo("/reports/curativerequirements");
                break;
            case "MNU_REPORT_LETTER_AGREEMENT_DEALS":
                Navigation.NavigateTo("/reports/letteragreementdeals");
                break;
            case "MNU_REPORT_REFERRER_1099_SUMMARY":
                Navigation.NavigateTo("/reports/referrer1099summary");
                break;

            // Documents
            case "MNU_DOCUMENTS_TEMPLATES":
                Navigation.NavigateTo("/documents/templates");
                break;
            case "MNU_DOCUMENTS_SEARCH":
                Navigation.NavigateTo("/documents/search");
                break;

            // Tools
            case "MNU_TOOLS_FILTERS":
                Navigation.NavigateTo("/tools/filters");
                break;
            case "MNU_TOOLS_VIEWS":
                Navigation.NavigateTo("/tools/views");
                break;
            case "MNU_TOOLS_LIEN_TYPES":
                Navigation.NavigateTo("/system/lientypes");
                break;
            case "MNU_TOOLS_CURATIVE_TYPES":
                Navigation.NavigateTo("/system/curativetypes");
                break;
            case "MNU_TOOLS_DEAL_STATUSES":
                Navigation.NavigateTo("/system/dealstatuses");
                break;
            case "MNU_TOOLS_LETTERAGREEMENT_DEAL_STATUSES":
                Navigation.NavigateTo("/system/letteragreementdealstatuses");
                break;
            case "MNU_TOOLS_FOLDER_LOCATIONS":
                Navigation.NavigateTo("/system/folderlocations");
                break;
            case "MNU_ADMINISTRATION_ROLES":
                Navigation.NavigateTo("/system/roles");
                break;

            // Administration
            case "MNU_ADMINISTRATION_NEW_USER":
                Navigation.NavigateTo("/admin/users/new");
                break;
            case "MNU_ADMINISTRATION_DISPLAY_USERS":
                Navigation.NavigateTo("/admin/users");
                break;
            case "MNU_ADMINISTRATION_EDIT_USER":
                await ActionService.ExecuteActionAsync(menuValue);
                break;

            // View
            case "MNU_VIEW_TOOLBAR_SHOW_TOOLBAR":
                ShowToolbar = !ShowToolbar;
                await InvokeAsync(StateHasChanged);
                break;

            default:
                // For toolbar and other actions, just trigger the callback
                break;
        }
    }

    /// <summary>
    /// Apply a filter from the submenu
    /// </summary>
    private async Task OnApplyFilter(string filterName)
    {
        CurrentFilter = filterName;

        if (OnFilterApplied.HasDelegate)
        {
            await OnFilterApplied.InvokeAsync(filterName);
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Apply a view from the submenu
    /// </summary>
    private async Task OnApplyView(string viewName)
    {
        CurrentView = viewName;

        if (OnViewApplied.HasDelegate)
        {
            await OnViewApplied.InvokeAsync(viewName);
        }

        await InvokeAsync(StateHasChanged);
    }
}
