using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using UI.Models.State;

namespace UI.Pages.Board.Components;

public partial class AddStateModal
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public CreateStateRequest FormModel { get; set; } = new();
    [Parameter] public int NextOrder { get; set; }
    [Parameter] public EventCallback OnOk { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private async Task HandleOk()
    {
        if (OnOk.HasDelegate)
        {
            await OnOk.InvokeAsync();
        }
    }

    private async Task HandleCancel()
    {
        if (OnCancel.HasDelegate)
        {
            await OnCancel.InvokeAsync();
        }
    }

    private async Task HandleSubmit()
    {
        await HandleOk();
    }

    private void HandleSubmitFailed(EditContext editContext)
    {
    }
}
