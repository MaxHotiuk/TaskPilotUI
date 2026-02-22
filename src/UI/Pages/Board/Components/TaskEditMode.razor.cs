using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using UI.Models.Task;
using UI.Models.State;
using UI.Models.Member;
using UI.Models.User;
using UI.Models.Tag;

namespace UI.Pages.Board.Components;

public partial class TaskEditMode : ComponentBase
{
    [Parameter, EditorRequired] public UpdateTaskRequest FormModel { get; set; } = default!;
    [Parameter, EditorRequired] public List<StateDto> States { get; set; } = new();
    [Parameter, EditorRequired] public List<TagDto> Tags { get; set; } = new();
    [Parameter, EditorRequired] public List<BoardMemberDto> BoardMembers { get; set; } = new();
    [Parameter, EditorRequired] public List<UserDto> AllUsers { get; set; } = new();
    [Parameter, EditorRequired] public bool CanManageTask { get; set; }
    [Parameter, EditorRequired] public EventCallback<EditContext> OnFormSubmit { get; set; }
    [Parameter, EditorRequired] public EventCallback<EditContext> OnFormSubmitFailed { get; set; }

    protected override void OnInitialized()
    {
        if (FormModel != null && FormModel.DueDate != null && FormModel.DueDate.Length >= 19)
        {
            FormModel.DueDate = FormModel.DueDate.Remove(10, 9);
        }
    }
}
