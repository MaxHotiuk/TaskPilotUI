using Microsoft.AspNetCore.Components;
using UI.Models.Board;
using UI.Interfaces.Services;

namespace UI.Pages.Boards.Components;

public partial class CreateBoardModal : ComponentBase
{
    [Inject] private IBoardService BoardService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public EventCallback<string> OnBoardCreated { get; set; }
    [Parameter] public Guid? SelectedOrganizationId { get; set; }

    private CreateBoardRequest _formModel = new();
    private bool _isLoading = false;
    private string _error = string.Empty;
    private Guid? _selectedOrgId;

    protected override void OnParametersSet()
    {
        if (Visible)
        {
            ResetForm();
            if (SelectedOrganizationId.HasValue)
            {
                _selectedOrgId = SelectedOrganizationId.Value;
            }
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
        Console.WriteLine($"CreateBoardModal - HandleSubmit called");
        Console.WriteLine($"CreateBoardModal - _selectedOrgId: {(_selectedOrgId.HasValue ? _selectedOrgId.Value.ToString() : "NULL")}");

        if (string.IsNullOrWhiteSpace(_formModel.Name))
        {
            _error = UI.Resources.I18n.BoardNameRequired;
            return;
        }

        if (!_selectedOrgId.HasValue || _selectedOrgId.Value == Guid.Empty)
        {
            _error = "Please select an organization";
            Console.WriteLine($"CreateBoardModal - Organization validation failed");
            return;
        }

        try
        {
            _isLoading = true;
            _error = string.Empty;
            StateHasChanged();

            var currentUser = AuthService.GetCachedUser();
            if (currentUser == null)
            {
                _error = UI.Resources.I18n.UserNotAuthenticated;
                return;
            }

            _formModel.OwnerId = currentUser.Id.ToString();
            _formModel.OrganizationId = _selectedOrgId.Value;

            Console.WriteLine($"CreateBoardModal - Creating board with OrganizationId: {_formModel.OrganizationId}");

            var boardId = await BoardService.CreateAsync(_formModel);

            await OnBoardCreated.InvokeAsync(boardId);
            await CloseModal();
        }
        catch (Exception ex)
        {
            // Check for specific guest user error
            if (ex.Message.Contains("Guest users cannot create boards"))
            {
                _error = "You cannot create boards as a guest. You must be a Member or Manager of an organization to create boards.";
            }
            else
            {
                _error = ex.Message;
            }
            Console.WriteLine($"CreateBoardModal - Error: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private void HandleSubmitFailed()
    {
        _error = UI.Resources.I18n.PleaseCheckFormAndTryAgain;
        StateHasChanged();
    }

    private async Task CloseModal()
    {
        await VisibleChanged.InvokeAsync(false);
        ResetForm();
    }

    private void ResetForm()
    {
        _formModel = new CreateBoardRequest();
        _error = string.Empty;
        _isLoading = false;
    }
}
