using Microsoft.AspNetCore.Components;
using MudBlazor;
using SSRBlazor.Services;
using SSRBusiness.Entities;

namespace SSRBlazor.Components.Pages.Referrers;

public partial class ReferrerEdit : ComponentBase
{
    [Parameter] public int ReferrerId { get; set; }

    private Referrer _referrer = new();
    private List<ReferrerForm> _forms = new();
    private MudForm _form = default!;

    [Inject] private ReferrerUiService ReferrerService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    // DialogService injected in razor file, but we can also inject here or use the one from razor if protected

    protected override async Task OnInitializedAsync()
    {
        if (ReferrerId != 0)
        {
            // Edit Mode
            var existing = await ReferrerService.GetByIdAsync(ReferrerId);
            if (existing != null)
            {
                _referrer = existing;
                await LoadForms();
            }
            else
            {
                Snackbar.Add($"Referrer {ReferrerId} not found", Severity.Error);
                Navigation.NavigateTo("/referrers");
            }
        }
        else
        {
            // Add Mode: defaults already set
            _referrer.CreatedOn = DateTime.Now;
        }
    }

    private async Task LoadForms()
    {
        if (ReferrerId != 0)
        {
            _forms = await ReferrerService.GetReferrerFormsAsync(ReferrerId);
        }
    }

    private async Task Save()
    {
        await _form.Validate();
        if (!_form.IsValid) return;

        try
        {
            if (ReferrerId == 0)
            {
                var newId = await ReferrerService.CreateAsync(_referrer);
                Snackbar.Add("Referrer created successfully", Severity.Success);
                // Optionally navigate to edit page to allow uploading forms immediately?
                // Or just go back to index. 
                // Let's go to Edit page so they can add forms if they want.
                Navigation.NavigateTo($"/referrer/edit/{newId}");
            }
            else
            {
                await ReferrerService.UpdateAsync(_referrer);
                Snackbar.Add("Referrer updated successfully", Severity.Success);
                Navigation.NavigateTo("/referrers");
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving referrer: {ex.Message}", Severity.Error);
        }
    }

    private void Cancel()
    {
        Navigation.NavigateTo("/referrers");
    }

    private async Task UploadForm()
    {
        var parameters = new DialogParameters<ReferrerFormDialog> { { x => x.ReferrerId, ReferrerId } };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        
        var dialog = await DialogService.ShowAsync<ReferrerFormDialog>("Upload Form", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await LoadForms();
        }
    }
}
