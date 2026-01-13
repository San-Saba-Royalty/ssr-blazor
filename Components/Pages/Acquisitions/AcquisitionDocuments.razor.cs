using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using SSRBlazor.Models;
using SSRBlazor.Services;

namespace SSRBlazor.Components.Pages.Acquisitions;

public partial class AcquisitionDocuments
{
    #region Parameters

    [Parameter]
    public int AcquisitionId { get; set; }

    [Parameter]
    public bool ShowTabs { get; set; } = true;

    #endregion

    #region Injected Services

    [Inject] private AcquisitionDocumentService DocumentService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    #endregion

    #region State

    // Tab
    private int _activeTabIndex = 2; // Documents tab

    // Messages
    private string? _displayMessage;
    private string? _errorMessage;

    // Document Type/Template Selection
    private List<DocumentTypeModel> _documentTypes = new();
    private List<DocumentTemplateModel> _documentTemplates = new();
    private List<(int Id, string Name)> _counties = new();
    private List<(int Id, string Name)> _operators = new();
    private List<DocumentCustomFieldModel> _customFields = new();

    private string? _selectedDocumentTypeCode;
    private int? _selectedDocumentTemplateId;
    private int? _selectedCountyId;
    private int? _selectedOperatorId;
    private int? _numberOfPagesToRecord;

    private bool _showCountySelector;
    private bool _showOperatorSelector;

    // Documents Grid
    private MudDataGrid<AcquisitionDocumentModel>? _dataGrid;
    private List<AcquisitionDocumentModel> _documents = new();
    private AcquisitionDocumentModel? _selectedDocument;
    private bool _isLoading;

    // Context Menu
    private MudMenu? _contextMenu;

    // Dialogs
    private bool _showDeleteDialog;
    private bool _showUploadDialog;
    private DialogOptions _dialogOptions = new() { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

    // Upload
    private IBrowserFile? _uploadFile;
    private bool _isUploading;
    private string? _uploadTargetHandle; // For uploading new version to existing document

    #endregion

    #region Lifecycle

    protected override async Task OnInitializedAsync()
    {
        await LoadDocumentTypesAsync();
        await LoadDocumentsAsync();
        await LoadCountiesAndOperatorsAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (AcquisitionId > 0)
        {
            await LoadDocumentsAsync();
        }
    }

    #endregion

    #region Data Loading

    private async Task LoadDocumentTypesAsync()
    {
        try
        {
            _documentTypes = await DocumentService.GetDocumentTypesAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading document types: {ex.Message}";
        }
    }

    private async Task LoadDocumentsAsync()
    {
        try
        {
            _isLoading = true;
            _documents = await DocumentService.GetAcquisitionDocumentsAsync(AcquisitionId);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading documents: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadCountiesAndOperatorsAsync()
    {
        try
        {
            _counties = await DocumentService.GetAcquisitionCountiesAsync(AcquisitionId);
            _operators = await DocumentService.GetAcquisitionOperatorsAsync(AcquisitionId);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading counties/operators: {ex.Message}";
        }
    }

    private async Task RefreshDocuments()
    {
        await LoadDocumentsAsync();
        _selectedDocument = null;
        Snackbar.Add("Documents refreshed", Severity.Info);
    }

    #endregion

    #region Document Type/Template Selection

    private string? SelectedDocumentTypeCode
    {
        get => _selectedDocumentTypeCode;
        set
        {
            if (_selectedDocumentTypeCode != value)
            {
                _selectedDocumentTypeCode = value;
                OnDocumentTypeChanged(value);
            }
        }
    }

    private async void OnDocumentTypeChanged(string? documentTypeCode)
    {
        _documentTemplates.Clear();
        _selectedDocumentTemplateId = null;
        _customFields.Clear();
        _showCountySelector = false;
        _showOperatorSelector = false;

        if (!string.IsNullOrEmpty(documentTypeCode))
        {
            try
            {
                _documentTemplates = await DocumentService.GetDocumentTemplatesByTypeAsync(documentTypeCode);
            }
            catch (Exception ex)
            {
                _errorMessage = $"Error loading document templates: {ex.Message}";
            }
        }

        StateHasChanged();
    }

    private int? SelectedDocumentTemplateId
    {
        get => _selectedDocumentTemplateId;
        set
        {
            if (_selectedDocumentTemplateId != value)
            {
                _selectedDocumentTemplateId = value;
                OnDocumentTemplateChanged(value);
            }
        }
    }

    private async void OnDocumentTemplateChanged(int? documentTemplateId)
    {
        _customFields.Clear();
        _showCountySelector = false;
        _showOperatorSelector = false;
        _selectedCountyId = null;
        _selectedOperatorId = null;

        if (documentTemplateId.HasValue)
        {
            try
            {
                var template = await DocumentService.GetDocumentTemplateAsync(documentTemplateId.Value);
                if (template != null)
                {
                    _showCountySelector = template.RequiresCounty;
                    _showOperatorSelector = template.RequiresOperator;
                    _customFields = template.CustomFields;
                }
            }
            catch (Exception ex)
            {
                _errorMessage = $"Error loading template details: {ex.Message}";
            }
        }

        StateHasChanged();
    }

    #endregion

    #region Document Generation

    private bool CanGenerateDocument()
    {
        if (string.IsNullOrEmpty(_selectedDocumentTypeCode) || _selectedDocumentTemplateId == null)
            return false;

        if (_showCountySelector && _selectedCountyId == null)
            return false;

        if (_showOperatorSelector && _selectedOperatorId == null)
            return false;

        return true;
    }

    private async Task GenerateDocument()
    {
        if (!CanGenerateDocument())
        {
            Snackbar.Add("Please fill in all required fields", Severity.Warning);
            return;
        }

        try
        {
            var request = new GenerateDocumentRequest
            {
                AcquisitionID = AcquisitionId,
                DocumentTemplateID = _selectedDocumentTemplateId!.Value,
                CountyID = _selectedCountyId,
                OperatorID = _selectedOperatorId,
                NumberOfPagesToRecord = _numberOfPagesToRecord,
                CustomFieldValues = _customFields
                    .Where(cf => !string.IsNullOrEmpty(cf.Value))
                    .ToDictionary(cf => cf.TagName, cf => cf.Value!)
            };

            var result = await DocumentService.GenerateDocumentAsync(request);

            if (result.Success)
            {
                _displayMessage = "Document generated successfully";
                await LoadDocumentsAsync();

                // Optionally open the generated document
                if (!string.IsNullOrEmpty(result.FilePath))
                {
                    await JS.InvokeVoidAsync("open", result.FilePath, "_blank");
                }
            }
            else
            {
                _errorMessage = result.Error ?? "Failed to generate document";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error generating document: {ex.Message}";
        }
    }

    #endregion

    #region Grid Events

    private void OnRowClick(DataGridRowClickEventArgs<AcquisitionDocumentModel> args)
    {
        _selectedDocument = args.Item;

        // Right-click opens context menu
        if (args.MouseEventArgs.Button == 2)
        {
            // Context menu handled by MudMenu
        }
    }

    private void OnSelectedItemChanged(AcquisitionDocumentModel? item)
    {
        _selectedDocument = item;
    }

    private string RowStyleFunc(AcquisitionDocumentModel item, int index)
    {
        if (_selectedDocument != null && item.Handle == _selectedDocument.Handle)
        {
            return "background-color: #e3f2fd;"; // Light blue for selected
        }
        return string.Empty;
    }

    #endregion

    #region Download

    private async Task DownloadDocument(AcquisitionDocumentModel? document)
    {
        if (document == null || string.IsNullOrEmpty(document.Url))
        {
            Snackbar.Add("No document available for download", Severity.Warning);
            return;
        }

        try
        {
            var result = await DocumentService.GetDocumentForDownloadAsync(document.Handle);
            if (result.Success && !string.IsNullOrEmpty(result.FilePath))
            {
                // Trigger download via JS
                await JS.InvokeVoidAsync("open", document.Url, "_blank");
            }
            else
            {
                Snackbar.Add("Document file not found", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error downloading document: {ex.Message}", Severity.Error);
        }
    }

    private async Task DownloadSelectedDocument()
    {
        await DownloadDocument(_selectedDocument);
    }

    #endregion

    #region Upload

    private void OpenUploadDialog()
    {
        _uploadFile = null;
        _uploadTargetHandle = null;
        _showUploadDialog = true;
    }

    private void UploadToDocument(AcquisitionDocumentModel? document)
    {
        _uploadFile = null;
        _uploadTargetHandle = document?.Handle;
        _showUploadDialog = true;
    }

    private void OnFileSelected(IBrowserFile file)
    {
        _uploadFile = file;
    }

    private void CancelUpload()
    {
        _uploadFile = null;
        _uploadTargetHandle = null;
        _showUploadDialog = false;
    }

    private async Task ConfirmUpload()
    {
        if (_uploadFile == null)
            return;

        try
        {
            _isUploading = true;

            // Max 50MB
            const long maxFileSize = 50 * 1024 * 1024;

            using var stream = _uploadFile.OpenReadStream(maxFileSize);

            var result = await DocumentService.UploadDocumentAsync(
                AcquisitionId,
                _uploadFile.Name,
                _uploadFile.ContentType,
                stream);

            if (result.Success)
            {
                _displayMessage = "Document uploaded successfully";
                _showUploadDialog = false;
                await LoadDocumentsAsync();
            }
            else
            {
                _errorMessage = result.Error ?? "Failed to upload document";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error uploading document: {ex.Message}";
        }
        finally
        {
            _isUploading = false;
            _uploadFile = null;
        }
    }

    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    #endregion

    #region Delete

    private void DeleteSelectedDocument()
    {
        if (_selectedDocument == null)
        {
            Snackbar.Add("Please select a document to delete", Severity.Warning);
            return;
        }

        _showDeleteDialog = true;
    }

    private async Task ConfirmDelete()
    {
        if (_selectedDocument == null)
            return;

        try
        {
            var success = await DocumentService.DeleteDocumentAsync(_selectedDocument.Handle);

            if (success)
            {
                _displayMessage = "Document deleted successfully";
                _selectedDocument = null;
                await LoadDocumentsAsync();
            }
            else
            {
                _errorMessage = "Failed to delete document";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error deleting document: {ex.Message}";
        }
        finally
        {
            _showDeleteDialog = false;
        }
    }

    #endregion

    #region Tab Navigation

    private void NavigateToTab(string tab)
    {
        var url = tab switch
        {
            "info" => $"/acquisition/{AcquisitionId}",
            "notes" => $"/acquisition/{AcquisitionId}/notes",
            "documents" => $"/acquisition/{AcquisitionId}/documents",
            "audit" => $"/acquisition/{AcquisitionId}/audit",
            _ => $"/acquisition/{AcquisitionId}"
        };

        Navigation.NavigateTo(url);
    }

    #endregion
}