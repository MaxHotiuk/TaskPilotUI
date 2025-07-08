using Microsoft.AspNetCore.Components;
using UI.Models.Board;
using UI.Models.Task;
using UI.Models.State;
using UI.Models.Member;

namespace UI.Pages.Board.Components;

public partial class BoardColumns : ComponentBase
{
    [Parameter] public BoardDetailDto? BoardDetail { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public EventCallback<TaskItemDto> OnTaskClick { get; set; }
    [Parameter] public EventCallback OnGoBack { get; set; }

    private List<TaskItemDto> GetTasksForState(int stateId)
    {
        return BoardDetail?.Tasks.Where(t => t.StateId == stateId).ToList() ?? new List<TaskItemDto>();
    }

    private int GetTaskCountForState(int stateId)
    {
        return BoardDetail?.Tasks.Count(t => t.StateId == stateId) ?? 0;
    }

    private string GetAssigneeName(string assigneeId)
    {
        var member = BoardDetail?.Members.FirstOrDefault(m => m.UserId == assigneeId);
        return member != null ? "User" : "Unknown";
    }

    private string TruncateDescription(string description)
    {
        return description.Length > 100 ? $"{description[..100]}..." : description;
    }

    private string FormatDueDate(string dueDate)
    {
        if (DateTime.TryParse(dueDate, out var date))
        {
            return date.ToString("MMM dd");
        }
        return dueDate;
    }
}
