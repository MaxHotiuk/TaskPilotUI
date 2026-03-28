using Microsoft.AspNetCore.Components;
using UI.Interfaces.Services;
using UI.Services;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Web;

namespace UI.Pages.Organization.Components;

public partial class AddGuestModal : ComponentBase
{
    [Inject] private IOrganizationService OrganizationService { get; set; } = default!;
    [Inject] private IPublicDomainService PublicDomainService { get; set; } = default!;

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public Guid OrganizationId { get; set; }
    [Parameter] public EventCallback OnGuestAdded { get; set; }

    private class FormModel
    {
        public string UserEmail { get; set; } = string.Empty;
    }

    private FormModel _formModel = new();
    private bool _isLoading = false;
    private string _error = string.Empty;
    private bool _isPublicDomain = false;
    private string _emailDomain = string.Empty;

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
        // Валідація email
        if (string.IsNullOrWhiteSpace(_formModel.UserEmail))
        {
            _error = UI.Resources.I18n.EmailRequired;
            return;
        }

        if (!EmailRegex.IsMatch(_formModel.UserEmail))
        {
            _error = UI.Resources.I18n.InvalidEmailFormat;
            return;
        }

        try
        {
            _isLoading = true;
            _error = string.Empty;
            StateHasChanged();

            await OrganizationService.AddGuestAsync(OrganizationId, _formModel.UserEmail.Trim());

            await OnGuestAdded.InvokeAsync();
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
        _isPublicDomain = false;
        _emailDomain = string.Empty;
    }

    private void HandleEmailInput(ChangeEventArgs e)
    {
        var email = e.Value?.ToString() ?? string.Empty;

        if (EmailRegex.IsMatch(email))
        {
            var domain = email.Split('@').LastOrDefault();
            if (!string.IsNullOrEmpty(domain))
            {
                _emailDomain = domain;
                _isPublicDomain = PublicDomainService.IsPublicDomain(domain);
                StateHasChanged();
            }
        }
        else
        {
            _isPublicDomain = false;
            _emailDomain = string.Empty;
        }
    }
}
