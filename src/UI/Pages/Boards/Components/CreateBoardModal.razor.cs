using Microsoft.AspNetCore.Components;
using UI.Models;
using UI.Services;

namespace UI.Pages.Boards.Components;

public partial class CreateBoardModal : ComponentBase
{
    [Inject] private IBoardService BoardService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public EventCallback<string> OnBoardCreated { get; set; }

    private CreateBoardRequest _formModel = new();
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
        if (string.IsNullOrWhiteSpace(_formModel.Name))
        {
            _error = "Board name is required";
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
                _error = "User not authenticated";
                return;
            }

            _formModel.OwnerId = currentUser.Id;
            var boardId = await BoardService.CreateBoardAsync(_formModel);

            await OnBoardCreated.InvokeAsync(boardId);
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

    private void HandleSubmitFailed()
    {
        _error = "Please check the form and try again";
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
