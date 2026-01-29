using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using SSRBlazor.Models;
using SSRBlazor.Services;
using SSRBusiness.BusinessClasses;
using SSRBusiness.Data;

namespace SSRBlazor.Services;

/// <summary>
/// Implementation of the action service that coordinates toolbar and menu actions.
/// </summary>
public class ActionService : IActionService
{
    private readonly NavigationManager _navigationManager;
    private readonly AcquisitionService _acquisitionService;
    private readonly LetterAgreementService _letterAgreementService;
    private readonly FilterService _filterService;
    private readonly ViewService _viewService;
    private readonly IDialogService _dialogService;
    private readonly IDbContextFactory<SsrDbContext> _contextFactory;

    // Track last viewed items for "Back" navigation
    private string? _lastAcquisitionId;
    private string? _lastLetterAgreementId;

    public event Action? OnStateChange;
    public event Func<Task>? OnSaveRequested;
    public event Func<Task>? OnDeleteRequested;
    public event Func<Task>? OnCopyRequested;
    public event Func<Task>? OnPrintRequested;

    public ActionState CurrentState { get; private set; } = ActionState.Default;

    public ActionService(
        NavigationManager navigationManager,
        AcquisitionService acquisitionService,
        LetterAgreementService letterAgreementService,
        FilterService filterService,
        ViewService viewService,
        IDialogService dialogService,
        IDbContextFactory<SsrDbContext> contextFactory)
    {
        _navigationManager = navigationManager;
        _acquisitionService = acquisitionService;
        _letterAgreementService = letterAgreementService;
        _filterService = filterService;
        _viewService = viewService;
        _dialogService = dialogService;
        _contextFactory = contextFactory;
    }

    public void UpdateState(ActionState state)
    {
        CurrentState = state;
        OnStateChange?.Invoke();
    }

    public async Task InitializeAsync(int userId)
    {
        try
        {
            // Create a new context to avoid concurrency issues
            await using var context = await _contextFactory.CreateDbContextAsync();
            var userRepository = new UserRepository(context);

            var user = await userRepository.LoadUserByUserIdAsync(userId);
            if (user != null)
            {
                if (user.LastAcquisitionID.HasValue)
                {
                    _lastAcquisitionId = user.LastAcquisitionID.Value.ToString();
                }

                if (user.LastLetterAgreementID.HasValue)
                {
                    _lastLetterAgreementId = user.LastLetterAgreementID.Value.ToString();
                }

                // Trigger state change to update UI buttons enablement
                OnStateChange?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing ActionService: {ex.Message}");
        }
    }

    public async Task<ActionResult> ExecuteActionAsync(string actionCommand, Dictionary<string, object>? parameters = null)
    {
        try
        {
            // Handle Action Events first
            switch (actionCommand)
            {
                case ActionCommands.TLB_SAVE:
                case ActionCommands.MNU_FILE_SAVE:
                    if (OnSaveRequested != null) await OnSaveRequested.Invoke();
                    return ActionResult.SuccessResult();

                case ActionCommands.TLB_DELETE:
                case ActionCommands.MNU_EDIT_DELETE:
                    if (OnDeleteRequested != null) await OnDeleteRequested.Invoke();
                    return ActionResult.SuccessResult();

                case ActionCommands.TLB_COPY:
                case ActionCommands.MNU_EDIT_COPY:
                    if (OnCopyRequested != null) await OnCopyRequested.Invoke();
                    return ActionResult.SuccessResult();

                case ActionCommands.TLB_PRINT:
                case ActionCommands.MNU_FILE_PRINT:
                    if (OnPrintRequested != null) await OnPrintRequested.Invoke();
                    return ActionResult.SuccessResult();

                case ActionCommands.MNU_VIEW_LIST:
                case ActionCommands.TLB_LIST:
                    // Toggle to List View -> Usually handled by page, but we can set state
                    UpdateState(CurrentState with { IsEditMode = false });
                    return ActionResult.SuccessResult();

                case ActionCommands.MNU_VIEW_FORM:
                case ActionCommands.TLB_FORM:
                    // Toggle to Form View
                    UpdateState(CurrentState with { IsEditMode = true });
                    return ActionResult.SuccessResult();
            }

            return actionCommand switch
            {
                // Toolbar Navigation Actions
                ActionCommands.TLB_FILTER or ActionCommands.MNU_TOOLS_FILTERS =>
                    ActionResult.SuccessResult(navigateToUrl: "/tools/filters"),

                ActionCommands.TLB_VIEW or ActionCommands.MNU_TOOLS_VIEWS =>
                    ActionResult.SuccessResult(navigateToUrl: "/tools/views"),

                ActionCommands.TLB_BACK =>
                    HandleBackToAcquisition(),

                ActionCommands.TLB_BACK_LETTERAGREEMENT =>
                    HandleBackToLetterAgreement(),

                ActionCommands.MNU_FILE_GOTO_LAST_ACQ =>
                    HandleBackToAcquisition(),

                ActionCommands.MNU_FILE_GOTO_LAST_LETTERAGREEMENT =>
                    HandleBackToLetterAgreement(),

                ActionCommands.MNU_FILE_GOTO_LAST_DOC_SEARCH =>
                    ActionResult.SuccessResult(navigateToUrl: "/documents/search?back=Y"),

                // File Menu - New Actions
                ActionCommands.MNU_FILE_NEW_ACQUISITION =>
                    await HandleNewAcquisitionAsync(parameters),

                ActionCommands.MNU_FILE_NEW_LETTERAGREEMENT =>
                    await HandleNewLetterAgreementAsync(parameters),

                ActionCommands.MNU_FILE_NEW_BUYER =>
                    ActionResult.SuccessResult(navigateToUrl: "/buyers/add"),

                ActionCommands.MNU_FILE_NEW_COUNTY =>
                    ActionResult.SuccessResult(navigateToUrl: "/counties/add"),

                ActionCommands.MNU_FILE_NEW_OPERATOR =>
                    ActionResult.SuccessResult(navigateToUrl: "/operators/add"),

                ActionCommands.MNU_FILE_NEW_APPRAISALGROUP =>
                    ActionResult.SuccessResult(navigateToUrl: "/appraisalgroups/add"),

                ActionCommands.MNU_FILE_NEW_REFERRER =>
                    ActionResult.SuccessResult(navigateToUrl: "/referrers/add"),

                // File Menu - Display Actions
                ActionCommands.MNU_FILE_DISPLAY_ACQUISITIONS =>
                    ActionResult.SuccessResult(navigateToUrl: "/acquisitions"),

                ActionCommands.MNU_FILE_DISPLAY_BUYERS =>
                    ActionResult.SuccessResult(navigateToUrl: "/buyers"),

                ActionCommands.MNU_FILE_DISPLAY_COUNTIES =>
                    ActionResult.SuccessResult(navigateToUrl: "/counties"),

                ActionCommands.MNU_FILE_DISPLAY_OPERATORS =>
                    ActionResult.SuccessResult(navigateToUrl: "/operators"),

                ActionCommands.MNU_FILE_DISPLAY_APPRAISALGROUPS =>
                    ActionResult.SuccessResult(navigateToUrl: "/appraisalgroups"),

                ActionCommands.MNU_FILE_DISPLAY_LETTERAGREEMENTS =>
                    ActionResult.SuccessResult(navigateToUrl: "/letteragreements"),

                ActionCommands.MNU_FILE_DISPLAY_REFERRERS =>
                    ActionResult.SuccessResult(navigateToUrl: "/referrers"),

                // Tools Menu
                ActionCommands.MNU_TOOLS_LIEN_TYPES =>
                    ActionResult.SuccessResult(navigateToUrl: "/administration/lientypes"),

                ActionCommands.MNU_TOOLS_CURATIVE_TYPES =>
                    ActionResult.SuccessResult(navigateToUrl: "/administration/curativetypes"),

                ActionCommands.MNU_TOOLS_DEAL_STATUSES =>
                    ActionResult.SuccessResult(navigateToUrl: "/administration/dealstatuses"),

                ActionCommands.MNU_TOOLS_LETTERAGREEMENT_DEAL_STATUSES =>
                    ActionResult.SuccessResult(navigateToUrl: "/administration/letteragreementdealstatuses"),

                ActionCommands.MNU_TOOLS_FOLDER_LOCATIONS =>
                    ActionResult.SuccessResult(navigateToUrl: "/administration/folderlocations"),

                // Administration Menu
                ActionCommands.MNU_ADMINISTRATION_NEW_USER =>
                    ActionResult.SuccessResult(navigateToUrl: "/account/users/add"),

                ActionCommands.MNU_ADMINISTRATION_NEW_ROLE =>
                    ActionResult.SuccessResult(navigateToUrl: "/administration/roles/add"),

                ActionCommands.MNU_ADMINISTRATION_DISPLAY_USERS =>
                    ActionResult.SuccessResult(navigateToUrl: "/account/users"),

                ActionCommands.MNU_ADMINISTRATION_DISPLAY_ROLES =>
                    ActionResult.SuccessResult(navigateToUrl: "/administration/roles"),

                // Documents Menu
                ActionCommands.MNU_DOCUMENTS_TEMPLATES =>
                    ActionResult.SuccessResult(navigateToUrl: "/documents/templates"),

                ActionCommands.MNU_DOCUMENTS_ACQUISITION_COVER_SHEET =>
                    ActionResult.SuccessResult(navigateToUrl: "/documents/cover-sheet/acquisition"),

                ActionCommands.MNU_DOCUMENTS_BARCODE_ACQUISITION_DOCUMENT =>
                     ActionResult.SuccessResult(navigateToUrl: "/documents/barcodes/acquisition"),

                ActionCommands.MNU_DOCUMENTS_BARCODE_CHECK_STATEMENT =>
                     ActionResult.SuccessResult(navigateToUrl: "/documents/barcodes/check-statement"),

                ActionCommands.MNU_DOCUMENTS_SEARCH =>
                    ActionResult.SuccessResult(navigateToUrl: "/documents/search"),

                // Report Menu
                ActionCommands.MNU_REPORT_CLOSING_REPORT =>
                    ActionResult.SuccessResult(navigateToUrl: "/reports?type=closing"),

                ActionCommands.MNU_REPORT_DRAFTS_DUE =>
                    ActionResult.SuccessResult(navigateToUrl: "/reports?type=draftsdue"),

                ActionCommands.MNU_REPORT_INVENTORY =>
                    ActionResult.SuccessResult(navigateToUrl: "/reports?type=inventory"),

                ActionCommands.MNU_REPORT_BUYER_INVOICES_DUE =>
                    ActionResult.SuccessResult(navigateToUrl: "/reports?type=invoicesdue"),

                ActionCommands.MNU_REPORT_PURCHASES =>
                    ActionResult.SuccessResult(navigateToUrl: "/reports?type=purchases"),

                ActionCommands.MNU_REPORT_MAP_DEED_PREP =>
                    ActionResult.SuccessResult(navigateToUrl: "/reports?type=mapdeedprep"),

                ActionCommands.MNU_REPORT_CURATIVE_REQUIREMENTS =>
                     ActionResult.SuccessResult(navigateToUrl: "/reports?type=curative"),

                ActionCommands.MNU_REPORT_LETTER_AGREEMENT_DEALS =>
                     ActionResult.SuccessResult(navigateToUrl: "/reports?type=ladeals"),

                ActionCommands.MNU_REPORT_REFERRER_1099_SUMMARY =>
                     ActionResult.SuccessResult(navigateToUrl: "/reports?type=referrer1099"),

                ActionCommands.MNU_ADMINISTRATION_EDIT_USER =>
                     await HandleEditUserAsync(),

                _ => ActionResult.ErrorResult($"Action '{actionCommand}' is not implemented")
            };
        }
        catch (Exception ex)
        {
            return ActionResult.ErrorResult($"Error executing action: {ex.Message}");
        }
    }

    private async Task<ActionResult> HandleEditUserAsync()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        await _dialogService.ShowAsync<SSRBlazor.Components.Pages.Account.UserInfoEditDialog>("Edit User", options);
        return ActionResult.SuccessResult();
    }

    public bool IsActionEnabled(string actionCommand, string? currentModule = null)
    {
        return actionCommand switch
        {
            // Back navigation is only enabled if we have a last viewed item
            ActionCommands.TLB_BACK => !string.IsNullOrEmpty(_lastAcquisitionId),
            ActionCommands.TLB_BACK_LETTERAGREEMENT => !string.IsNullOrEmpty(_lastLetterAgreementId),

            // State-dependent actions
            ActionCommands.TLB_SAVE or ActionCommands.MNU_FILE_SAVE => CurrentState.CanSave,
            ActionCommands.TLB_DELETE or ActionCommands.MNU_EDIT_DELETE => CurrentState.CanDelete,
            ActionCommands.TLB_COPY or ActionCommands.MNU_EDIT_COPY => CurrentState.CanCopy,
            ActionCommands.TLB_PRINT or ActionCommands.MNU_FILE_PRINT => CurrentState.CanPrint,

            // View/Form toggles
            ActionCommands.TLB_FORM or ActionCommands.MNU_VIEW_FORM => !CurrentState.IsEditMode, // Enable Form/Edit if NOT in edit mode
            ActionCommands.TLB_LIST or ActionCommands.MNU_VIEW_LIST => CurrentState.IsEditMode, // Enable List if IN edit mode

            // Most toolbar actions are enabled by default
            ActionCommands.TLB_NEW => true,
            ActionCommands.TLB_FILTER => true,
            ActionCommands.TLB_VIEW => true,

            _ => true
        };
    }

    public string? GetActionUrl(string actionCommand, Dictionary<string, object>? parameters = null)
    {
        return actionCommand switch
        {
            // Tools/View Targets
            ActionCommands.TLB_FILTER or ActionCommands.MNU_TOOLS_FILTERS or ActionCommands.MNU_VIEW_APPLY_FILTER => "/tools/filters",
            ActionCommands.TLB_VIEW or ActionCommands.MNU_TOOLS_VIEWS or ActionCommands.MNU_VIEW_APPLY_VIEW => "/tools/views",

            // File -> New Targets
            ActionCommands.MNU_FILE_NEW_ACQUISITION => "/acquisitions/add",
            ActionCommands.MNU_FILE_NEW_BUYER => "/buyers/add",
            ActionCommands.MNU_FILE_NEW_COUNTY => "/counties/add",
            ActionCommands.MNU_FILE_NEW_OPERATOR => "/operators/add",
            ActionCommands.MNU_FILE_NEW_APPRAISALGROUP => "/appraisalgroups/add",
            ActionCommands.MNU_FILE_NEW_LETTERAGREEMENT => "/letteragreements/add",
            ActionCommands.MNU_FILE_NEW_REFERRER => "/referrers/add",

            // File -> Display Targets
            ActionCommands.MNU_FILE_DISPLAY_ACQUISITIONS => "/acquisitions",
            ActionCommands.MNU_FILE_DISPLAY_BUYERS => "/buyers",
            ActionCommands.MNU_FILE_DISPLAY_COUNTIES => "/counties",
            ActionCommands.MNU_FILE_DISPLAY_OPERATORS => "/operators",
            ActionCommands.MNU_FILE_DISPLAY_APPRAISALGROUPS => "/appraisalgroups",
            ActionCommands.MNU_FILE_DISPLAY_LETTERAGREEMENTS => "/letteragreements",
            ActionCommands.MNU_FILE_DISPLAY_REFERRERS => "/referrers",

            // Report Targets
            ActionCommands.MNU_REPORT_CLOSING_REPORT => "/reports/closing-report",
            ActionCommands.MNU_REPORT_DRAFTS_DUE => "/reports/drafts-due",
            ActionCommands.MNU_REPORT_INVENTORY => "/reports/inventory",
            ActionCommands.MNU_REPORT_BUYER_INVOICES_DUE => "/reports/invoices-due",
            ActionCommands.MNU_REPORT_PURCHASES => "/reports/purchases",
            ActionCommands.MNU_REPORT_MAP_DEED_PREP => "/reports/map-deed-prep",
            ActionCommands.MNU_REPORT_CURATIVE_REQUIREMENTS => "/reports/curative",
            ActionCommands.MNU_REPORT_LETTER_AGREEMENT_DEALS => "/reports/la-deals",
            ActionCommands.MNU_REPORT_REFERRER_1099_SUMMARY => "/reports/referrer-1099",

            // Admin Targets
            ActionCommands.MNU_ADMINISTRATION_NEW_USER => "/account/users/add",
            ActionCommands.MNU_ADMINISTRATION_NEW_ROLE => "/system/roles/add",
            ActionCommands.MNU_ADMINISTRATION_DISPLAY_USERS => "/account/users",
            ActionCommands.MNU_ADMINISTRATION_DISPLAY_ROLES => "/system/roles",
            ActionCommands.MNU_ADMINISTRATION_EDIT_USER => "/account/user-info",

            // Tools Targets
            ActionCommands.MNU_TOOLS_LIEN_TYPES => "/system/lien-types",
            ActionCommands.MNU_TOOLS_CURATIVE_TYPES => "/system/curative-types",
            ActionCommands.MNU_TOOLS_DEAL_STATUSES => "/system/deal-statuses",
            ActionCommands.MNU_TOOLS_LETTERAGREEMENT_DEAL_STATUSES => "/system/la-deal-statuses",
            ActionCommands.MNU_TOOLS_FOLDER_LOCATIONS => "/system/folder-locations",

            // Document Targets
            ActionCommands.MNU_DOCUMENTS_TEMPLATES => "/documents",
            ActionCommands.MNU_DOCUMENTS_SEARCH => "/documents/search",

            // Toolbar/Misc
            ActionCommands.TLB_BACK when !string.IsNullOrEmpty(_lastAcquisitionId) =>
                $"/acquisitions/edit/{_lastAcquisitionId}",
            ActionCommands.TLB_BACK_LETTERAGREEMENT when !string.IsNullOrEmpty(_lastLetterAgreementId) =>
                $"/letteragreements/edit/{_lastLetterAgreementId}",
            ActionCommands.MNU_FILE_GOTO_LAST_ACQ when !string.IsNullOrEmpty(_lastAcquisitionId) =>
                $"/acquisitions/edit/{_lastAcquisitionId}",
            ActionCommands.MNU_FILE_GOTO_LAST_LETTERAGREEMENT when !string.IsNullOrEmpty(_lastLetterAgreementId) =>
                $"/letteragreements/edit/{_lastLetterAgreementId}",

            _ => null
        };
    }

    public void SetLastAcquisitionId(string acquisitionId)
    {
        _lastAcquisitionId = acquisitionId;
    }

    public void SetLastLetterAgreementId(string letterAgreementId)
    {
        _lastLetterAgreementId = letterAgreementId;
    }

    public string? GetLastAcquisitionId() => _lastAcquisitionId;

    public string? GetLastLetterAgreementId() => _lastLetterAgreementId;

    // Private helper methods

    private ActionResult HandleBackToAcquisition()
    {
        if (string.IsNullOrEmpty(_lastAcquisitionId))
        {
            return ActionResult.ErrorResult("No previous acquisition to return to");
        }

        return ActionResult.SuccessResult(navigateToUrl: $"/acquisitions/edit/{_lastAcquisitionId}");
    }

    private ActionResult HandleBackToLetterAgreement()
    {
        if (string.IsNullOrEmpty(_lastLetterAgreementId))
        {
            return ActionResult.ErrorResult("No previous letter agreement to return to");
        }

        return ActionResult.SuccessResult(navigateToUrl: $"/letteragreements/edit/{_lastLetterAgreementId}");
    }

    private Task<ActionResult> HandleNewAcquisitionAsync(Dictionary<string, object>? parameters)
    {
        // Navigate to the acquisitions add page
        // The page will handle creating the new acquisition
        return Task.FromResult(ActionResult.SuccessResult(navigateToUrl: "/acquisitions/add"));
    }

    private Task<ActionResult> HandleNewLetterAgreementAsync(Dictionary<string, object>? parameters)
    {
        // Navigate to the letter agreements add page
        // The page will handle creating the new letter agreement
        return Task.FromResult(ActionResult.SuccessResult(navigateToUrl: "/letteragreements/add"));
    }
}
