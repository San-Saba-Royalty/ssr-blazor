using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using SSRBusiness.BusinessClasses;
using SSRBusiness.Data;
using SSRBusiness.Entities;
using SSRBusiness.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace SSRBlazor.Components.Pages.LetterAgreements;

public partial class LetterAgreementDocuments
{
    #region Parameters

    [Parameter]
    public int LetterAgreementId { get; set; }

    [Parameter]
    public bool ShowTabs { get; set; } = true;

    #endregion

    #region Injected Services

    [Inject] private IDbContextFactory<SsrDbContext> DbContextFactory { get; set; } = default!;
    [Inject] private LetterAgreementTemplateEngine TemplateEngine { get; set; } = default!;
    [Inject] private IGeneratedDocumentService GeneratedDocumentService { get; set; } = default!;
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

    // Generation Selection
    private string _selectedStateCode = "TX";
    private int? _selectedSigningPartnerId;
    private string _selectedDocumentType = "LetterAgreement";
    private List<SigningPartnerModel> _signingPartners = new();

    // Documents Grid
    private MudDataGrid<LetterAgreementDocumentModel>? _dataGrid;
    private List<LetterAgreementDocumentModel> _documents = new();
    private LetterAgreementDocumentModel? _selectedDocument;
    private bool _isLoading;

    // Dialogs
    private bool _showDeleteDialog;
    private DialogOptions _dialogOptions = new() { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

    // Letter Agreement data
    private LetterAgreement? _letterAgreement;

    #endregion

    #region Lifecycle

    protected override async Task OnInitializedAsync()
    {
        await LoadLetterAgreementAsync();
        await LoadSigningPartnersAsync();
        await LoadDocumentsAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (LetterAgreementId > 0 && (_letterAgreement == null || _letterAgreement.LetterAgreementID != LetterAgreementId))
        {
            await LoadLetterAgreementAsync();
            await LoadSigningPartnersAsync();
            await LoadDocumentsAsync();
        }
    }

    #endregion

    #region Data Loading

    private async Task LoadLetterAgreementAsync()
    {
        try
        {
            await using var context = await DbContextFactory.CreateDbContextAsync();
            _letterAgreement = await context.LetterAgreements
                .FirstOrDefaultAsync(la => la.LetterAgreementID == LetterAgreementId);

            if (_letterAgreement != null && !string.IsNullOrEmpty(_letterAgreement.StateCode))
            {
                _selectedStateCode = _letterAgreement.StateCode.ToUpperInvariant();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading letter agreement: {ex.Message}";
        }
    }

    private async Task LoadSigningPartnersAsync()
    {
        try
        {
            await using var context = await DbContextFactory.CreateDbContextAsync();
            var partners = await context.LetterAgreementSellers
                .Where(sp => sp.LetterAgreementID == LetterAgreementId)
                .OrderBy(sp => sp.SellerName)
                .ToListAsync();

            _signingPartners = partners.Select((sp, index) => new SigningPartnerModel
            {
                Id = index, // Using index as ID since entity doesn't have a primary key
                Name = sp.SellerName ?? $"Partner {index}",
                IsCompany = sp.CompanyIndicator
            }).ToList();

            // Auto-select first partner if only one
            if (_signingPartners.Count == 1)
            {
                _selectedSigningPartnerId = _signingPartners[0].Id;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading signing partners: {ex.Message}";
        }
    }

    private async Task LoadDocumentsAsync()
    {
        try
        {
            _isLoading = true;
            // Load generated documents from Azure File Share listing
            // For now, we'll use a placeholder implementation
            _documents = new List<LetterAgreementDocumentModel>();
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

    private async Task RefreshDocuments()
    {
        await LoadDocumentsAsync();
        _selectedDocument = null;
        Snackbar.Add("Documents refreshed", Severity.Info);
    }

    #endregion

    #region Document Generation

    private bool CanGenerateDocument()
    {
        return _selectedSigningPartnerId.HasValue && !string.IsNullOrEmpty(_selectedDocumentType);
    }

    private async Task GenerateDocument()
    {
        if (!CanGenerateDocument())
        {
            Snackbar.Add("Please select a signing partner and document type", Severity.Warning);
            return;
        }

        try
        {
            // Get template paths based on state
            var templatePaths = GetTemplatePathsForState(_selectedStateCode);

            var criteria = new LetterAgreementCriteria
            {
                LetterAgreementId = LetterAgreementId,
                SigningPartnerId = _selectedSigningPartnerId!.Value,
                StateCode = _selectedStateCode,
                UserId = 1, // TODO: Get from authentication context
                LetterAgreementSource = templatePaths.LetterAgreementTemplate,
                LetterAgreementSignatureIndividualSource = templatePaths.SignatureIndividualTemplate,
                LetterAgreementSignatureCompanySource = templatePaths.SignatureCompanyTemplate,
                ConveyanceExhibitASource = templatePaths.ConveyanceExhibitATemplate,
                ConveyanceProducingSource = templatePaths.ConveyanceProducingTemplate,
                ConveyanceNonProducingSource = templatePaths.ConveyanceNonProducingTemplate
            };

            var result = await TemplateEngine.CreateLetterAgreementAsync(criteria);

            _displayMessage = "Document generated successfully";
            await LoadDocumentsAsync();

            // Add to local documents list for immediate display
            _documents.Add(new LetterAgreementDocumentModel
            {
                DocumentType = _selectedDocumentType,
                SigningPartnerName = _signingPartners.FirstOrDefault(p => p.Id == _selectedSigningPartnerId)?.Name ?? "",
                StateCode = _selectedStateCode,
                CreatedBy = "Current User",
                CreatedDate = DateTime.Now,
                FileName = Path.GetFileName(result.DocumentPath),
                DocumentPath = result.DocumentPath
            });

            // Open the generated document
            if (!string.IsNullOrEmpty(result.DownloadUrl))
            {
                await JS.InvokeVoidAsync("open", result.DownloadUrl, "_blank");
            }

            Snackbar.Add("Letter Agreement document generated successfully!", Severity.Success);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error generating document: {ex.Message}";
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Gets state-specific template paths (TX vs LA).
    /// </summary>
    private StateTemplatePaths GetTemplatePathsForState(string stateCode)
    {
        // State-specific template path patterns from legacy system
        return stateCode.ToUpperInvariant() switch
        {
            "LA" => new StateTemplatePaths
            {
                LetterAgreementTemplate = "LetterAgreement/Louisiana/Letter Agreement Template.doc",
                SignatureIndividualTemplate = "LetterAgreement/Louisiana/Letter Agreement Signature - Individual.doc",
                SignatureCompanyTemplate = "LetterAgreement/Louisiana/Letter Agreement Signature - Company.doc",
                ConveyanceExhibitATemplate = "LetterAgreement/Louisiana/Conveyance - Exhibit A.doc",
                ConveyanceProducingTemplate = "LetterAgreement/Louisiana/Conveyance - Producing.doc",
                ConveyanceNonProducingTemplate = "LetterAgreement/Louisiana/Conveyance - Non-Producing.doc"
            },
            _ => new StateTemplatePaths // Default to TX
            {
                LetterAgreementTemplate = "LetterAgreement/Texas/Letter Agreement Template.doc",
                SignatureIndividualTemplate = "LetterAgreement/Texas/Letter Agreement Signature - Individual.doc",
                SignatureCompanyTemplate = "LetterAgreement/Texas/Letter Agreement Signature - Company.doc",
                ConveyanceExhibitATemplate = "LetterAgreement/Texas/Conveyance - Exhibit A.doc",
                ConveyanceProducingTemplate = "LetterAgreement/Texas/Conveyance - Producing.doc",
                ConveyanceNonProducingTemplate = "LetterAgreement/Texas/Conveyance - Non-Producing.doc"
            }
        };
    }

    #endregion

    #region Grid Events

    private void OnRowClick(DataGridRowClickEventArgs<LetterAgreementDocumentModel> args)
    {
        _selectedDocument = args.Item;
    }

    private void OnSelectedItemChanged(LetterAgreementDocumentModel? item)
    {
        _selectedDocument = item;
    }

    private string RowStyleFunc(LetterAgreementDocumentModel item, int index)
    {
        if (_selectedDocument != null && item.DocumentPath == _selectedDocument.DocumentPath)
        {
            return "background-color: #e3f2fd;";
        }
        return string.Empty;
    }

    #endregion

    #region Download

    private async Task DownloadDocument(LetterAgreementDocumentModel document)
    {
        if (string.IsNullOrEmpty(document.DocumentPath))
        {
            Snackbar.Add("No document available for download", Severity.Warning);
            return;
        }

        try
        {
            var downloadUrl = await GeneratedDocumentService.GetDownloadUrlAsync(document.DocumentPath);
            if (!string.IsNullOrEmpty(downloadUrl))
            {
                await JS.InvokeVoidAsync("open", downloadUrl, "_blank");
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
            // Delete from Azure File Share
            // For now, just remove from local list
            _documents.Remove(_selectedDocument);
            _displayMessage = "Document deleted successfully";
            _selectedDocument = null;
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
            "info" => $"/letteragreement/{LetterAgreementId}",
            "notes" => $"/letteragreement/{LetterAgreementId}/notes",
            "documents" => $"/letteragreement/{LetterAgreementId}/documents",
            "audit" => $"/letteragreement/{LetterAgreementId}/audit",
            _ => $"/letteragreement/{LetterAgreementId}"
        };

        Navigation.NavigateTo(url);
    }

    #endregion

    #region Models

    private class SigningPartnerModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsCompany { get; set; }
    }

    private class StateTemplatePaths
    {
        public string LetterAgreementTemplate { get; set; } = string.Empty;
        public string SignatureIndividualTemplate { get; set; } = string.Empty;
        public string SignatureCompanyTemplate { get; set; } = string.Empty;
        public string ConveyanceExhibitATemplate { get; set; } = string.Empty;
        public string ConveyanceProducingTemplate { get; set; } = string.Empty;
        public string ConveyanceNonProducingTemplate { get; set; } = string.Empty;
    }

    #endregion
}

/// <summary>
/// Model for displaying Letter Agreement generated documents.
/// </summary>
public class LetterAgreementDocumentModel
{
    public string DocumentType { get; set; } = string.Empty;
    public string SigningPartnerName { get; set; } = string.Empty;
    public string StateCode { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string DocumentPath { get; set; } = string.Empty;
}
