using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using UI.Models.Tag;

namespace UI.Pages.Board.Components;

public partial class AddTagModal : ComponentBase
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter, EditorRequired] public CreateTagRequestDto FormModel { get; set; }
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
