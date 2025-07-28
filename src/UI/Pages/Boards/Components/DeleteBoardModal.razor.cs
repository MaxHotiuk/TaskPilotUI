using Microsoft.AspNetCore.Components;
using UI.Models.Board;

namespace UI.Pages.Boards.Components;

public partial class DeleteBoardModal
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public BoardSearchDto? SelectedBoard { get; set; }
    [Parameter] public bool IsDeleting { get; set; }
    [Parameter] public string DeleteConfirmation { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> DeleteConfirmationChanged { get; set; }
    [Parameter] public EventCallback OnConfirmDelete { get; set; }
    [Parameter] public EventCallback OnCancelDelete { get; set; }

    private async Task ConfirmDelete()
    {
        await OnConfirmDelete.InvokeAsync();
    }

    private async Task HandleInput(ChangeEventArgs e)
    {
        if (e?.Value is string value)
        {
            await DeleteConfirmationChanged.InvokeAsync(value);
        }
    }

    private async Task CancelDelete()
    {
        await OnCancelDelete.InvokeAsync();
    }
}
