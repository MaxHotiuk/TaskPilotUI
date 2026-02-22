using Microsoft.AspNetCore.Components;
using UI.Models.Board;
using UI.Models.Task;
using UI.Models.State;
using UI.Models.Member;
using UI.Models.User;
using UI.Interfaces.Services;

namespace UI.Pages.Board.Components;

public partial class BoardColumns : ComponentBase
{
    [Parameter] public BoardDetailDto? BoardDetail { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public EventCallback<TaskItemDto> OnTaskClick { get; set; }
    [Parameter] public EventCallback OnGoBack { get; set; }

    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IColorService ColorService { get; set; } = default!;

    private Dictionary<string, UserDto> _userCache = new();

    protected override async Task OnParametersSetAsync()
    {
        if (BoardDetail?.Members != null)
        {
            if (_userCache.Count == 0 || !BoardDetail.Members.All(m => _userCache.ContainsKey(m.UserId)))
            {
                _userCache.Clear();
                await LoadUserData();
            }
        }
    }

    private async Task LoadUserData()
    {
        if (BoardDetail?.Members == null) return;

        var userIds = BoardDetail.Members.Select(m => m.UserId).Distinct();
        _userCache = await UserService.GetByIdsAsync(userIds);
    }

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
        if (_userCache.TryGetValue(assigneeId, out var user))
        {
            return user.Username;
        }

        var member = BoardDetail?.Members.FirstOrDefault(m => m.UserId == assigneeId);
        return member != null ? UI.Resources.I18n.UserLabel : UI.Resources.I18n.UnknownLabel;
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
    
    private string GetPriorityColor(int priority)
    {
        return priority switch
        {
            1 => "gray",      // Low
            2 => "blue",      // Normal
            3 => "orange",    // High
            4 => "red",       // Immediate
            _ => "default"
        };
    }

    private string GetPriorityLabel(int priority)
    {
        return priority switch
        {
            1 => UI.Resources.I18n.PriorityLowLabel,
            2 => UI.Resources.I18n.PriorityNormalLabel,
            3 => UI.Resources.I18n.PriorityHighLabel,
            4 => UI.Resources.I18n.PriorityImmediateLabel,
            _ => UI.Resources.I18n.UnknownLabel
        };
    }

    private string GetTagTextColor(string? hexColor)
    {
        return ColorService.GetTagTextColor(hexColor);
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
}
