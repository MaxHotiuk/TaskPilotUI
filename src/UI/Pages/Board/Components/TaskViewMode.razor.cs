using Microsoft.AspNetCore.Components;
using UI.Models.Task;
using UI.Models.State;
using UI.Models.User;

namespace UI.Pages.Board.Components;

public partial class TaskViewMode : ComponentBase
{
    [Parameter, EditorRequired] public TaskItemDto Task { get; set; } = default!;
    [Parameter, EditorRequired] public List<StateDto> States { get; set; } = new();
    [Parameter, EditorRequired] public List<UserDto> AllUsers { get; set; } = new();

    private string GetStateName(int stateId)
    {
        return States.FirstOrDefault(s => s.Id == stateId)?.Name ?? "Unknown";
    }

    private string GetAssigneeName(string assigneeId)
    {
        var user = AllUsers.FirstOrDefault(u => u.Id == assigneeId);
        return user?.Username ?? "Unknown User";
    }

    private string FormatDueDate(string dueDateString)
    {
        if (DateTime.TryParse(dueDateString, out var dueDate))
        {
            return dueDate.ToString("MMM dd, yyyy");
        }
        return dueDateString;
    }

    private string GetDueDateColor(string dueDateString)
    {
        if (DateTime.TryParse(dueDateString, out var dueDate))
        {
            var now = DateTime.Now;
            var timeDiff = dueDate - now;

            if (timeDiff.TotalDays < 0)
                return "red"; // Overdue
            else if (timeDiff.TotalDays <= 3)
                return "orange"; // Due soon
            else
                return "green"; // Good
        }
        return "default";
    }

    private bool IsDueSoon(string dueDateString)
    {
        if (DateTime.TryParse(dueDateString, out var dueDate))
        {
            var now = DateTime.Now;
            var timeDiff = dueDate - now;
            return timeDiff.TotalDays <= 3 && timeDiff.TotalDays >= 0;
        }
        return false;
    }

    private string GetDueIndicator(string dueDateString)
    {
        if (DateTime.TryParse(dueDateString, out var dueDate))
        {
            var now = DateTime.Now;
            var timeDiff = dueDate - now;

            if (timeDiff.TotalDays < 0)
                return "⚠️ Overdue";
            else if (timeDiff.TotalDays <= 1)
                return "🔥 Due today";
            else if (timeDiff.TotalDays <= 3)
                return "⏰ Due soon";
        }
        return string.Empty;
    }

    private string GetCreatedTimeString()
    {
        if (!string.IsNullOrEmpty(Task.CreatedAt) && DateTime.TryParse(Task.CreatedAt, out var createdDate))
        {
            var now = DateTime.Now;
            var timeDiff = now - createdDate;

            if (timeDiff.TotalDays < 1)
                return "today";
            else if (timeDiff.TotalDays < 7)
                return $"{(int)timeDiff.TotalDays} days ago";
            else
                return createdDate.ToString("MMM dd, yyyy");
        }
        return "recently";
    }

    private string GetUpdatedTimeString()
    {
        if (!string.IsNullOrEmpty(Task.UpdatedAt) && DateTime.TryParse(Task.UpdatedAt, out var updatedDate))
        {
            var now = DateTime.Now;
            var timeDiff = now - updatedDate;

            if (timeDiff.TotalDays < 1)
                return "today";
            else if (timeDiff.TotalDays < 7)
                return $"{(int)timeDiff.TotalDays} days ago";
            else
                return updatedDate.ToString("MMM dd, yyyy");
        }
        return "recently";
    }
}
