using Microsoft.AspNetCore.Components;
using UI.Models.State;

namespace UI.Pages.Board.Components;

public partial class TaskQuickActions : ComponentBase
{
    [Parameter, EditorRequired] public List<StateDto> States { get; set; } = new();
    [Parameter, EditorRequired] public int CurrentStateId { get; set; }
    [Parameter, EditorRequired] public bool IsLoading { get; set; }
    [Parameter, EditorRequired] public EventCallback<int> OnStateChange { get; set; }
}
