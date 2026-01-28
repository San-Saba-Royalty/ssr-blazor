namespace SSRBlazor.Models;

/// <summary>
/// Defines all action command constants for the toolbar and menu system.
/// Mirrors the legacy MenuCommand class from the WebForms application.
/// </summary>
public static class ActionCommands
{
    // Toolbar Commands
    public const string TLB_NEW = "TLB_NEW";
    public const string TLB_SAVE = "TLB_SAVE";
    public const string TLB_PRINT = "TLB_PRINT";
    public const string TLB_COPY = "TLB_COPY";
    public const string TLB_DELETE = "TLB_DELETE";
    public const string TLB_LIST = "TLB_LIST";
    public const string TLB_FORM = "TLB_FORM";
    public const string TLB_FILTER = "TLB_FILTER";
    public const string TLB_VIEW = "TLB_VIEW";
    public const string TLB_BACK = "TLB_BACK";
    public const string TLB_BACK_LETTERAGREEMENT = "TLB_BACK_LETTERAGREEMENT";
    public const string TLB_BACK_DOC_SEARCH = "TLB_BACK_DOC_SEARCH";

    // File Menu Commands
    public const string MNU_FILE = "MNU_FILE";
    public const string MNU_FILE_NEW = "MNU_FILE_NEW";
    public const string MNU_FILE_NEW_ACQUISITION = "MNU_FILE_NEW_ACQUISITION";
    public const string MNU_FILE_NEW_BUYER = "MNU_FILE_NEW_BUYER";
    public const string MNU_FILE_NEW_COUNTY = "MNU_FILE_NEW_COUNTY";
    public const string MNU_FILE_NEW_OPERATOR = "MNU_FILE_NEW_OPERATOR";
    public const string MNU_FILE_NEW_APPRAISALGROUP = "MNU_FILE_NEW_APPRAISALGROUP";
    public const string MNU_FILE_NEW_LETTERAGREEMENT = "MNU_FILE_NEW_LETTERAGREEMENT";
    public const string MNU_FILE_NEW_REFERRER = "MNU_FILE_NEW_REFERRER";

    public const string MNU_FILE_DISPLAY = "MNU_FILE_DISPLAY";
    public const string MNU_FILE_DISPLAY_ACQUISITIONS = "MNU_FILE_DISPLAY_ACQUISITIONS";
    public const string MNU_FILE_DISPLAY_BUYERS = "MNU_FILE_DISPLAY_BUYERS";
    public const string MNU_FILE_DISPLAY_COUNTIES = "MNU_FILE_DISPLAY_COUNTIES";
    public const string MNU_FILE_DISPLAY_OPERATORS = "MNU_FILE_DISPLAY_OPERATORS";
    public const string MNU_FILE_DISPLAY_APPRAISALGROUPS = "MNU_FILE_DISPLAY_APPRAISALGROUPS";
    public const string MNU_FILE_DISPLAY_LETTERAGREEMENTS = "MNU_FILE_DISPLAY_LETTERAGREEMENTS";
    public const string MNU_FILE_DISPLAY_REFERRERS = "MNU_FILE_DISPLAY_REFERRERS";

    public const string MNU_FILE_SAVE = "MNU_FILE_SAVE";
    public const string MNU_FILE_PRINT = "MNU_FILE_PRINT";
    public const string MNU_FILE_TAX_ROLL_SEARCH = "MNU_FILE_TAX_ROLL_SEARCH";
    public const string MNU_FILE_GOTO_LAST_ACQ = "MNU_FILE_GOTO_LAST_ACQ";
    public const string MNU_FILE_GOTO_LAST_LETTERAGREEMENT = "MNU_FILE_GOTO_LAST_LETTERAGREEMENT";
    public const string MNU_FILE_GOTO_LAST_DOC_SEARCH = "MNU_FILE_GOTO_LAST_DOC_SEARCH";
    public const string MNU_FILE_LOGOUT = "MNU_FILE_LOGOUT";

    // Edit Menu Commands
    public const string MNU_EDIT = "MNU_EDIT";
    public const string MNU_EDIT_COPY = "MNU_EDIT_COPY";
    public const string MNU_EDIT_DELETE = "MNU_EDIT_DELETE";

    // Report Menu Commands
    public const string MNU_REPORT = "MNU_REPORT";
    public const string MNU_REPORT_CLOSING_REPORT = "MNU_REPORT_CLOSING_REPORT";
    public const string MNU_REPORT_DRAFTS_DUE = "MNU_REPORT_DRAFTS_DUE";
    public const string MNU_REPORT_INVENTORY = "MNU_REPORT_INVENTORY";
    public const string MNU_REPORT_BUYER_INVOICES_DUE = "MNU_REPORT_BUYER_INVOICES_DUE";
    public const string MNU_REPORT_PURCHASES = "MNU_REPORT_PURCHASES";
    public const string MNU_REPORT_MAP_DEED_PREP = "MNU_REPORT_MAP_DEED_PREP";
    public const string MNU_REPORT_CURATIVE_REQUIREMENTS = "MNU_REPORT_CURATIVE_REQUIREMENTS";
    public const string MNU_REPORT_LETTER_AGREEMENT_DEALS = "MNU_REPORT_LETTER_AGREEMENT_DEALS";
    public const string MNU_REPORT_REFERRER_1099_SUMMARY = "MNU_REPORT_REFERRER_1099_SUMMARY";

    // Documents Menu Commands
    public const string MNU_DOCUMENTS = "MNU_DOCUMENTS";
    public const string MNU_DOCUMENTS_TEMPLATES = "MNU_DOCUMENTS_TEMPLATES";
    public const string MNU_DOCUMENTS_ACQUISITION_COVER_SHEET = "MNU_DOCUMENTS_ACQUISITION_COVER_SHEET";
    public const string MNU_DOCUMENTS_BARCODE_ACQUISITION_DOCUMENT = "MNU_DOCUMENTS_BARCODE_ACQUISITION_DOCUMENT";
    public const string MNU_DOCUMENTS_BARCODE_CHECK_STATEMENT = "MNU_DOCUMENTS_BARCODE_CHECK_STATEMENT";
    public const string MNU_DOCUMENTS_SEARCH = "MNU_DOCUMENTS_SEARCH";

    // View Menu Commands
    public const string MNU_VIEW = "MNU_VIEW";
    public const string MNU_VIEW_LIST = "MNU_VIEW_LIST";
    public const string MNU_VIEW_FORM = "MNU_VIEW_FORM";
    public const string MNU_VIEW_TOOLBAR = "MNU_VIEW_TOOLBAR";
    public const string MNU_VIEW_TOOLBAR_SHOW_TOOLBAR = "MNU_VIEW_TOOLBAR_SHOW_TOOLBAR";
    public const string MNU_VIEW_APPLY_FILTER = "MNU_VIEW_APPLY_FILTER";
    public const string MNU_VIEW_APPLY_FILTER_ALL_RECORDS = "MNU_VIEW_APPLY_FILTER_ALL_RECORDS";
    public const string MNU_VIEW_APPLY_FILTER_SELECT = "MNU_VIEW_APPLY_FILTER_SELECT";
    public const string MNU_VIEW_APPLY_VIEW = "MNU_VIEW_APPLY_VIEW";
    public const string MNU_VIEW_APPLY_VIEW_ALL_FIELDS = "MNU_VIEW_APPLY_VIEW_ALL_FIELDS";
    public const string MNU_VIEW_APPLY_VIEW_SELECT = "MNU_VIEW_APPLY_VIEW_SELECT";

    // Tools Menu Commands
    public const string MNU_TOOLS = "MNU_TOOLS";
    public const string MNU_TOOLS_FILTERS = "MNU_TOOLS_FILTERS";
    public const string MNU_TOOLS_VIEWS = "MNU_TOOLS_VIEWS";
    public const string MNU_TOOLS_LIEN_TYPES = "MNU_TOOLS_LIEN_TYPES";
    public const string MNU_TOOLS_CURATIVE_TYPES = "MNU_TOOLS_CURATIVE_TYPES";
    public const string MNU_TOOLS_DEAL_STATUSES = "MNU_TOOLS_DEAL_STATUSES";
    public const string MNU_TOOLS_LETTERAGREEMENT_DEAL_STATUSES = "MNU_TOOLS_LETTERAGREEMENT_DEAL_STATUSES";
    public const string MNU_TOOLS_FOLDER_LOCATIONS = "MNU_TOOLS_FOLDER_LOCATIONS";

    // Administration Menu Commands
    public const string MNU_ADMINISTRATION = "MNU_ADMINISTRATION";
    public const string MNU_ADMINISTRATION_NEW = "MNU_ADMINISTRATION_NEW";
    public const string MNU_ADMINISTRATION_NEW_USER = "MNU_ADMINISTRATION_NEW_USER";
    public const string MNU_ADMINISTRATION_NEW_ROLE = "MNU_ADMINISTRATION_NEW_ROLE";
    public const string MNU_ADMINISTRATION_DISPLAY = "MNU_ADMINISTRATION_DISPLAY";
    public const string MNU_ADMINISTRATION_DISPLAY_USERS = "MNU_ADMINISTRATION_DISPLAY_USERS";
    public const string MNU_ADMINISTRATION_DISPLAY_ROLES = "MNU_ADMINISTRATION_DISPLAY_ROLES";
    public const string MNU_ADMINISTRATION_EDIT_USER = "MNU_ADMINISTRATION_EDIT_USER";

    // Legacy Hidden/Extra Commands
    public const string MNU_DOCUMENTS_SEPARATOR = "MNU_DOCUMENTS_SEPARATOR";
    public const string MNU_DOCUMENTS_ACCOUNTING = "MNU_DOCUMENTS_ACCOUNTING";
    public const string MNU_DOCUMENTS_ACQUISITION = "MNU_DOCUMENTS_ACQUISITION";
    public const string MNU_DOCUMENTS_ATTORNEY = "MNU_DOCUMENTS_ATTORNEY";
    public const string MNU_DOCUMENTS_BUYER = "MNU_DOCUMENTS_BUYER";
    public const string MNU_DOCUMENTS_COUNTY = "MNU_DOCUMENTS_COUNTY";
    public const string MNU_DOCUMENTS_OPERATOR = "MNU_DOCUMENTS_OPERATOR";
    public const string MNU_DOCUMENTS_SELLER = "MNU_DOCUMENTS_SELLER";
    public const string MNU_DOCUMENTS_LETTERAGREEMENT = "MNU_DOCUMENTS_LETTERAGREEMENT";
    public const string MNU_DOCUMENTS_REFERRER = "MNU_DOCUMENTS_REFERRER";

    public const string MNU_DOCUMENTS_ACCOUNTING_SELECT = "MNU_DOCUMENTS_ACCOUNTING_SELECT";
    public const string MNU_DOCUMENTS_ACQUISITION_SELECT = "MNU_DOCUMENTS_ACQUISITION_SELECT";
    public const string MNU_DOCUMENTS_ATTORNEY_SELECT = "MNU_DOCUMENTS_ATTORNEY_SELECT";
    public const string MNU_DOCUMENTS_BUYER_SELECT = "MNU_DOCUMENTS_BUYER_SELECT";
    public const string MNU_DOCUMENTS_COUNTY_SELECT = "MNU_DOCUMENTS_COUNTY_SELECT";
    public const string MNU_DOCUMENTS_OPERATOR_SELECT = "MNU_DOCUMENTS_OPERATOR_SELECT";
    public const string MNU_DOCUMENTS_SELLER_SELECT = "MNU_DOCUMENTS_SELLER_SELECT";
    public const string MNU_DOCUMENTS_LETTERAGREEMENT_SELECT = "MNU_DOCUMENTS_LETTERAGREEMENT_SELECT";
    public const string MNU_DOCUMENTS_REFERRER_SELECT = "MNU_DOCUMENTS_REFERRER_SELECT";
}
