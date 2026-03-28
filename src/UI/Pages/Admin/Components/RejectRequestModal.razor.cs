using Microsoft.AspNetCore.Components;
using UI.Models.Organization;
using UI.Interfaces.Services;

namespace UI.Pages.Admin.Components;

public partial class RejectRequestModal : ComponentBase
{
    [Inject] private IOrganizationService OrganizationService { get; set; } = default!;

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public ManagerRequestDto? Request { get; set; }
    [Parameter] public EventCallback OnRequestRejected { get; set; }

    private class FormModel
    {
        public string? ReviewNotes { get; set; }
    }

    private FormModel _formModel = new();
    private bool _isLoading = false;
    private string _error = string.Empty;

    protected override void OnParametersSet()
    {
        if (Visible)
        {
            ResetForm();
        }
    }

    private async Task HandleOk()
    {
        await HandleSubmit();
    }

    private async Task HandleCancel()
    {
        await CloseModal();
    }

    private async Task HandleSubmit()
    {
        if (Request == null)
        {
            _error = UI.Resources.I18n.NoRequestSelected;
            return;
        }

        try
        {
            _isLoading = true;
            _error = string.Empty;
            StateHasChanged();

            await OrganizationService.RejectManagerRequestAsync(Request.Id, _formModel.ReviewNotes);

            await OnRequestRejected.InvokeAsync();
            await CloseModal();
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task CloseModal()
    {
        await VisibleChanged.InvokeAsync(false);
        ResetForm();
    }

    private void ResetForm()
    {
        _formModel = new FormModel();
        _error = string.Empty;
        _isLoading = false;
    }
}
