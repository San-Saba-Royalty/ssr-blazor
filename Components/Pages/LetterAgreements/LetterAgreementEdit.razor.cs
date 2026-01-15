#pragma warning disable CS8602
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SSRBusiness.Entities;

namespace SSRBlazor.Components.Pages.LetterAgreements;

public partial class LetterAgreementEdit : ComponentBase
{
    [Parameter]
    public int? Id { get; set; }

    // State
    private bool _isLoading = true;
    private bool _isSaving = false;
    private bool _isReadOnly = false;
    private string? _errorMessage;

    // Main entity
    private LetterAgreement? _letterAgreement;

    // Seller info (editable form fields)
    private LetterAgreementSellerModel _seller = new();

    // Form fields mapped from entity
    private DateTime? _effectiveDate;
    private DateTime? _receiptDate;
    private int? _bankingDays;
    private int? _landManId;
    private decimal? _totalBonus;
    private decimal? _considerationFee;
    private bool _takeConsiderationFromTotal;
    private bool _takeReferralFromTotal;
    private string? _newStatusCode;

    // Current status display
    private string? _currentStatus;

    // Referrer
    private LetterAgreementReferrer? _referrer;
    private string? _referrerName;

    // Collections
    private List<LetterAgreementUnit> _units = new();
    private List<LetterAgreementCounty> _counties = new();
    private List<LetterAgreementOperator> _operators = new();
    private List<LetterAgreementNote> _notes = new();
    private List<LetterAgreementChange> _auditHistory = new();

    // Counts for tab badges
    private int _unitCount => _units.Count;
    private int _countyCount => _counties.Count;
    private int _operatorCount => _operators.Count;
    private int _noteCount => _notes.Count;

    // Lookup data
    private List<StateItem> _states = new();
    private List<UserItem> _landmen = new();
    private List<LetterAgreementDealStatus> _dealStatuses = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadLookupsAsync();

        if (Id.HasValue)
        {
            await LoadLetterAgreementAsync(Id.Value);
        }
        else
        {
            InitializeNewLetterAgreement();
        }

        _isLoading = false;
    }

    private async Task LoadLookupsAsync()
    {
        try
        {
            // Load states
            _states = GetStates();

            // Load landmen
            _landmen = await LetterAgreementService.GetLandmenAsync();

            // Load deal statuses
            _dealStatuses = await LetterAgreementService.GetDealStatusesAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading lookup data: {ex.Message}";
        }
    }

    private async Task LoadLetterAgreementAsync(int id)
    {
        try
        {
            _letterAgreement = await LetterAgreementService.GetByIdWithDetailsAsync(id);

            if (_letterAgreement == null)
            {
                _errorMessage = $"Letter Agreement #{id} not found.";
                return;
            }

            // Check if linked to acquisition (read-only mode)
            _isReadOnly = _letterAgreement.AcquisitionID.HasValue;

            // Map entity to form fields
            MapEntityToForm();

            // Load related collections
            await LoadRelatedDataAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading letter agreement: {ex.Message}";
        }
    }

    private void InitializeNewLetterAgreement()
    {
        _letterAgreement = null;
        _seller = new LetterAgreementSellerModel();
        _effectiveDate = null;
        _receiptDate = null;
        _bankingDays = null;
        _landManId = null;
        _totalBonus = null;
        _considerationFee = null;
        _takeConsiderationFromTotal = false;
        _isReadOnly = false;
    }

    private void MapEntityToForm()
    {
        if (_letterAgreement == null) return;

        // Map dates
        _effectiveDate = _letterAgreement.EffectiveDate;
        _receiptDate = _letterAgreement.ReceiptDate;
        _bankingDays = _letterAgreement.BankingDays;
        _landManId = _letterAgreement.LandManID;

        // Map financial
        _totalBonus = _letterAgreement.TotalBonus;
        _considerationFee = _letterAgreement.ConsiderationFee;
        _takeConsiderationFromTotal = _letterAgreement.TakeConsiderationFromTotal;

        // Map seller
        var seller = _letterAgreement.LetterAgreementSellers.FirstOrDefault();
        if (seller != null)
        {
            _seller = new LetterAgreementSellerModel
            {
                LetterAgreementSellerID = seller.LetterAgreementSellerID,
                CompanyIndicator = seller.CompanyIndicator,
                SellerLastName = seller.SellerLastName,
                SellerName = seller.SellerName,
                AddressLine1 = seller.AddressLine1,
                AddressLine2 = seller.AddressLine2,
                City = seller.City,
                StateCode = seller.StateCode,
                ZipCode = seller.ZipCode,
                ContactPhone = seller.ContactPhone,
                ContactFax = seller.ContactFax,
                ContactEmail = seller.ContactEmail
            };
        }

        // Map referrer
        _referrer = _letterAgreement.LetterAgreementReferrers.FirstOrDefault();
        if (_referrer != null)
        {
            _referrerName = _referrer.Referrer?.ReferrerName ?? "Unknown";
            _takeReferralFromTotal = _referrer.SellerPaysReferralAmount;
        }

        // Get current status
        var lastStatus = _letterAgreement.LetterAgreementStatuses
            .OrderByDescending(s => s.StatusDate)
            .FirstOrDefault();
        _currentStatus = lastStatus?.LetterAgreementDealStatus?.StatusName;
    }

    private async Task LoadRelatedDataAsync()
    {
        if (_letterAgreement == null) return;

        _units = _letterAgreement.LetterAgreementUnits.ToList();
        _counties = _letterAgreement.LetterAgreementCounties.ToList();
        _operators = _letterAgreement.LetterAgreementOperators.ToList();
        _notes = _letterAgreement.LetterAgreementNotes.ToList();
        _auditHistory = _letterAgreement.LetterAgreementChanges
            .OrderByDescending(c => c.ChangeDate)
            .ToList();

        await Task.CompletedTask;
    }

    private decimal CalculateTotalBonusAndFee()
    {
        var total = (_totalBonus ?? 0);
        if (!_takeConsiderationFromTotal)
        {
            total += (_considerationFee ?? 0);
        }
        return total;
    }

    private async Task SaveAsync()
    {
        try
        {
            _isSaving = true;
            _errorMessage = null;
            StateHasChanged();

            if (Id.HasValue && _letterAgreement != null)
            {
                // Update existing
                MapFormToEntity();
                await LetterAgreementService.UpdateAsync(_letterAgreement);
                await LetterAgreementService.SaveSellerAsync(Id.Value, _seller);
                Snackbar.Add("Letter Agreement saved successfully.", Severity.Success);
            }
            else
            {
                // Create new
                var newLetterAgreement = new LetterAgreement
                {
                    CreatedOn = DateTime.Now,
                    LastUpdatedOn = DateTime.Now
                };
                MapFormToEntity(newLetterAgreement);

                var newId = await LetterAgreementService.CreateAsync(newLetterAgreement);
                await LetterAgreementService.SaveSellerAsync(newId, _seller);

                Snackbar.Add("Letter Agreement created successfully.", Severity.Success);
                NavigationManager.NavigateTo($"/letteragreement/edit/{newId}");
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error saving: {ex.Message}";
            Snackbar.Add(_errorMessage, Severity.Error);
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }

    private void MapFormToEntity(LetterAgreement? target = null)
    {
        var entity = target ?? _letterAgreement;
        if (entity == null) return;

        entity.EffectiveDate = _effectiveDate;
        entity.ReceiptDate = _receiptDate;
        entity.BankingDays = _bankingDays;
        entity.LandManID = _landManId;
        entity.TotalBonus = _totalBonus;
        entity.ConsiderationFee = _considerationFee;
        entity.TakeConsiderationFromTotal = _takeConsiderationFromTotal;
        entity.TotalBonusAndFee = CalculateTotalBonusAndFee();
        entity.LastUpdatedOn = DateTime.Now;
    }

    private async Task ApplyStatus()
    {
        if (string.IsNullOrEmpty(_newStatusCode) || !Id.HasValue) return;

        try
        {
            await LetterAgreementService.AddStatusAsync(Id.Value, _newStatusCode);

            // Refresh status display
            _currentStatus = _dealStatuses.FirstOrDefault(s => s.DealStatusCode == _newStatusCode)?.StatusName;
            _newStatusCode = null;

            Snackbar.Add("Status applied successfully.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error applying status: {ex.Message}", Severity.Error);
        }
    }

    private async Task ConvertToAcquisition()
    {
        if (!Id.HasValue) return;

        var confirm = await DialogService.ShowMessageBox(
            "Convert to Acquisition",
            "This will create a new Acquisition from this Letter Agreement. The Letter Agreement will become read-only. Continue?",
            yesText: "Convert", cancelText: "Cancel");

        if (confirm != true) return;

        try
        {
            var acquisitionId = await LetterAgreementService.ConvertToAcquisitionAsync(Id.Value);
            Snackbar.Add($"Created Acquisition #{acquisitionId}. Redirecting...", Severity.Success);
            NavigationManager.NavigateTo($"/acquisition/edit/{acquisitionId}");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error converting: {ex.Message}", Severity.Error);
        }
    }

    #region Unit Actions

    private async Task AddUnit()
    {
        var letterAgreement = _letterAgreement;
        if (letterAgreement == null) return;

        var parameters = new DialogParameters<Dialogs.LetterAgreementUnitDialog>
        {
            { x => x.LetterAgreementId, letterAgreement.LetterAgreementID }
        };

        var dialog = await DialogService.ShowAsync<Dialogs.LetterAgreementUnitDialog>("Add Unit", parameters);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is LetterAgreementUnit unit)
        {
            try
            {
                await LetterAgreementService.CreateUnitAsync(unit);
                _units = await LetterAgreementService.GetUnitsWithDetailsAsync(_letterAgreement.LetterAgreementID);
                Snackbar.Add("Unit added successfully.", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error adding unit: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task EditUnit(LetterAgreementUnit unit)
    {
        var letterAgreement = _letterAgreement;
        if (letterAgreement == null) return;

        var parameters = new DialogParameters<Dialogs.LetterAgreementUnitDialog>
        {
            { x => x.Unit, unit },
            { x => x.LetterAgreementId, letterAgreement.LetterAgreementID }
        };

        var dialog = await DialogService.ShowAsync<Dialogs.LetterAgreementUnitDialog>("Edit Unit", parameters);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is LetterAgreementUnit updatedUnit)
        {
            try
            {
                await LetterAgreementService.UpdateUnitAsync(updatedUnit);
                _units = await LetterAgreementService.GetUnitsWithDetailsAsync(_letterAgreement.LetterAgreementID);
                Snackbar.Add("Unit updated successfully.", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error updating unit: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task DeleteUnit(LetterAgreementUnit unit)
    {
        var confirm = await DialogService.ShowMessageBox(
            "Delete Unit",
            "Are you sure you want to delete this unit?",
            yesText: "Delete", cancelText: "Cancel");

        if (confirm != true) return;

        try
        {
            await LetterAgreementService.DeleteUnitAsync(unit.LetterAgreementUnitID);
            _units.Remove(unit);
            Snackbar.Add("Unit deleted.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error deleting unit: {ex.Message}", Severity.Error);
        }
    }

    #endregion

    #region County Actions

    private async Task AddCounty()
    {
        // TODO: Open LetterAgreementCountyDialog
        Snackbar.Add("County dialog not yet implemented.", Severity.Info);
        await Task.CompletedTask;
    }

    private async Task DeleteCounty(LetterAgreementCounty county)
    {
        var confirm = await DialogService.ShowMessageBox(
            "Delete County",
            "Are you sure you want to remove this county?",
            yesText: "Delete", cancelText: "Cancel");

        if (confirm != true) return;

        try
        {
            await LetterAgreementService.DeleteCountyAsync(county.LetterAgreementCountyID);
            _counties.Remove(county);
            Snackbar.Add("County removed.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error removing county: {ex.Message}", Severity.Error);
        }
    }

    #endregion

    #region Operator Actions

    private async Task AddOperator()
    {
        // TODO: Open LetterAgreementOperatorDialog
        Snackbar.Add("Operator dialog not yet implemented.", Severity.Info);
        await Task.CompletedTask;
    }

    private async Task DeleteOperator(LetterAgreementOperator op)
    {
        var confirm = await DialogService.ShowMessageBox(
            "Delete Operator",
            "Are you sure you want to remove this operator?",
            yesText: "Delete", cancelText: "Cancel");

        if (confirm != true) return;

        try
        {
            await LetterAgreementService.DeleteOperatorAsync(op.LetterAgreementOperatorID);
            _operators.Remove(op);
            Snackbar.Add("Operator removed.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error removing operator: {ex.Message}", Severity.Error);
        }
    }

    #endregion

    #region Note Actions

    private async Task AddNote()
    {
        var letterAgreement = _letterAgreement;
        if (letterAgreement == null) return;

        var noteTypes = await LetterAgreementService.GetNoteTypesAsync();
        var parameters = new DialogParameters<Dialogs.LetterAgreementNoteDialog>
        {
            { x => x.LetterAgreementId, letterAgreement.LetterAgreementID },
            { x => x.NoteTypes, noteTypes }
        };

        var dialog = await DialogService.ShowAsync<Dialogs.LetterAgreementNoteDialog>("Add Note", parameters);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is LetterAgreementNote note)
        {
            try
            {
                await LetterAgreementService.CreateNoteAsync(note);
                _notes = await LetterAgreementService.GetNotesAsync(_letterAgreement.LetterAgreementID);
                Snackbar.Add("Note added successfully.", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error adding note: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task EditNote(LetterAgreementNote note)
    {
        var letterAgreement = _letterAgreement;
        if (letterAgreement == null) return;

        var noteTypes = await LetterAgreementService.GetNoteTypesAsync();
        var parameters = new DialogParameters<Dialogs.LetterAgreementNoteDialog>
        {
            { x => x.Note, note },
            { x => x.LetterAgreementId, letterAgreement.LetterAgreementID },
            { x => x.NoteTypes, noteTypes }
        };

        var dialog = await DialogService.ShowAsync<Dialogs.LetterAgreementNoteDialog>("Edit Note", parameters);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is LetterAgreementNote updatedNote)
        {
            try
            {
                await LetterAgreementService.UpdateNoteAsync(updatedNote);
                _notes = await LetterAgreementService.GetNotesAsync(_letterAgreement.LetterAgreementID);
                Snackbar.Add("Note updated successfully.", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error updating note: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task DeleteNote(LetterAgreementNote note)
    {
        var confirm = await DialogService.ShowMessageBox(
            "Delete Note",
            "Are you sure you want to delete this note?",
            yesText: "Delete", cancelText: "Cancel");

        if (confirm != true) return;

        try
        {
            await LetterAgreementService.DeleteNoteAsync(note.LetterAgreementNoteID);
            _notes.Remove(note);
            Snackbar.Add("Note deleted.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error deleting note: {ex.Message}", Severity.Error);
        }
    }

    #endregion

    #region Referrer Actions

    private async Task AddReferrer()
    {
        var letterAgreement = _letterAgreement;
        if (letterAgreement == null) return;

        var referrers = await LetterAgreementService.GetAllReferrersAsync();
        var parameters = new DialogParameters<Dialogs.LetterAgreementReferrerDialog>
        {
            { x => x.LetterAgreementId, letterAgreement.LetterAgreementID },
            { x => x.Referrers, referrers }
        };

        var dialog = await DialogService.ShowAsync<Dialogs.LetterAgreementReferrerDialog>("Add Referrer", parameters);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is LetterAgreementReferrer referrer)
        {
            try
            {
                await LetterAgreementService.SaveReferrerAsync(referrer);
                _referrer = await LetterAgreementService.GetReferrerAsync(_letterAgreement.LetterAgreementID);
                _referrerName = _referrer?.Referrer?.ReferrerName;
                Snackbar.Add("Referrer added successfully.", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error adding referrer: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task EditReferrer()
    {
        var letterAgreement = _letterAgreement;
        if (letterAgreement == null) return;

        var referrers = await LetterAgreementService.GetAllReferrersAsync();
        var parameters = new DialogParameters<Dialogs.LetterAgreementReferrerDialog>
        {
            { x => x.Referrer, _referrer },
            { x => x.LetterAgreementId, letterAgreement.LetterAgreementID },
            { x => x.Referrers, referrers }
        };

        var dialog = await DialogService.ShowAsync<Dialogs.LetterAgreementReferrerDialog>("Edit Referrer", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            try
            {
                if (result.Data == null)
                {
                    // Delete was requested
                    await LetterAgreementService.DeleteReferrerAsync(letterAgreement.LetterAgreementID);
                    _referrer = null;
                    _referrerName = null;
                    Snackbar.Add("Referrer removed.", Severity.Success);
                }
                else if (result.Data is LetterAgreementReferrer updatedReferrer)
                {
                    await LetterAgreementService.SaveReferrerAsync(updatedReferrer);
                    _referrer = await LetterAgreementService.GetReferrerAsync(letterAgreement.LetterAgreementID);
                    _referrerName = _referrer?.Referrer?.ReferrerName;
                    Snackbar.Add("Referrer updated successfully.", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error updating referrer: {ex.Message}", Severity.Error);
            }
        }
    }

    #endregion

    #region Lookup Helpers

    private static List<StateItem> GetStates()
    {
        // US States - could be loaded from database
        return new List<StateItem>
        {
            new("AL", "Alabama"), new("AK", "Alaska"), new("AZ", "Arizona"), new("AR", "Arkansas"),
            new("CA", "California"), new("CO", "Colorado"), new("CT", "Connecticut"), new("DE", "Delaware"),
            new("FL", "Florida"), new("GA", "Georgia"), new("HI", "Hawaii"), new("ID", "Idaho"),
            new("IL", "Illinois"), new("IN", "Indiana"), new("IA", "Iowa"), new("KS", "Kansas"),
            new("KY", "Kentucky"), new("LA", "Louisiana"), new("ME", "Maine"), new("MD", "Maryland"),
            new("MA", "Massachusetts"), new("MI", "Michigan"), new("MN", "Minnesota"), new("MS", "Mississippi"),
            new("MO", "Missouri"), new("MT", "Montana"), new("NE", "Nebraska"), new("NV", "Nevada"),
            new("NH", "New Hampshire"), new("NJ", "New Jersey"), new("NM", "New Mexico"), new("NY", "New York"),
            new("NC", "North Carolina"), new("ND", "North Dakota"), new("OH", "Ohio"), new("OK", "Oklahoma"),
            new("OR", "Oregon"), new("PA", "Pennsylvania"), new("RI", "Rhode Island"), new("SC", "South Carolina"),
            new("SD", "South Dakota"), new("TN", "Tennessee"), new("TX", "Texas"), new("UT", "Utah"),
            new("VT", "Vermont"), new("VA", "Virginia"), new("WA", "Washington"), new("WV", "West Virginia"),
            new("WI", "Wisconsin"), new("WY", "Wyoming")
        };
    }

    #endregion
}

// Helper classes
public record StateItem(string StateCode, string StateName);
public record UserItem(int UserID, string FullName);

public class LetterAgreementSellerModel
{
    public int LetterAgreementSellerID { get; set; }
    public bool CompanyIndicator { get; set; }
    public string? SellerLastName { get; set; }
    public string? SellerName { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateCode { get; set; }
    public string? ZipCode { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactFax { get; set; }
    public string? ContactEmail { get; set; }
}
