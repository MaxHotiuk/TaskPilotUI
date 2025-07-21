using Microsoft.AspNetCore.Components;

namespace UI.Pages.Board.Components;

public partial class ArchiveBoardModal : ComponentBase
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public EventCallback OnOk { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private async Task OnOkClicked()
    {
        if (OnOk.HasDelegate)
        {
            await OnOk.InvokeAsync(null);
        }
    }

    private async Task OnCancelClicked()
    {
        if (OnCancel.HasDelegate)
        {
            await OnCancel.InvokeAsync(null);
        }
    }
}
