using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace UI.Pages.Board.Components;

public partial class TaskModalFooter : ComponentBase
{
    [Parameter, EditorRequired] public bool IsEditing { get; set; }
    [Parameter, EditorRequired] public bool IsLoading { get; set; }
    [Parameter, EditorRequired] public bool CanManageTask { get; set; }
    [Parameter, EditorRequired] public EventCallback<MouseEventArgs> OnCancelEdit { get; set; }
    [Parameter, EditorRequired] public EventCallback<MouseEventArgs> OnSaveChanges { get; set; }
    [Parameter, EditorRequired] public EventCallback<MouseEventArgs> OnClose { get; set; }
    [Parameter, EditorRequired] public EventCallback<MouseEventArgs> OnEdit { get; set; }
    [Parameter, EditorRequired] public EventCallback<MouseEventArgs> OnDelete { get; set; }
}
