using Microsoft.AspNetCore.Components;
using UI.Interfaces.Services;

namespace UI.Pages.Organization.Components;

public partial class ManagerRequestModal : ComponentBase
{
    [Inject] private IOrganizationService OrganizationService { get; set; } = default!;

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public Guid OrganizationId { get; set; }
    [Parameter] public Guid UserId { get; set; }
    [Parameter] public EventCallback OnRequestSent { get; set; }

    private class FormModel
    {
        public string Message { get; set; } = string.Empty;
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
        if (string.IsNullOrWhiteSpace(_formModel.Message))
        {
            _error = UI.Resources.I18n.MessageRequired;
            return;
        }

        if (_formModel.Message.Length > 1000)
        {
            _error = UI.Resources.I18n.MessageTooLong;
            return;
        }

        // Перевірка UserId
        if (UserId == Guid.Empty)
        {
            _error = "User ID is not set. Please reload the page.";
            return;
        }

        try
        {
            _isLoading = true;
            _error = string.Empty;
            StateHasChanged();

            await OrganizationService.SendManagerRequestAsync(OrganizationId, UserId, _formModel.Message);

            await OnRequestSent.InvokeAsync();
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
