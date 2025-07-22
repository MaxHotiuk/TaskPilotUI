using Microsoft.AspNetCore.Components;
using UI.Models.Task;
using UI.Models.User;
using UI.Interfaces.Services;
using UI.Extensions;
using System.Globalization;

namespace UI.Pages;

public partial class Calendar : ComponentBase
{
    [CascadingParameter] public IGlobalLoadingService LoadingService { get; set; } = default!;
    [Inject] private ITaskService TaskService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private List<TaskCalendarItemDto> _tasks = new();
    private DateTime _selectedDate = DateTime.Today;
    private DateTime _currentMonth = DateTime.Today;
    private bool _isLoading = false;
    private Dictionary<DateTime, List<TaskCalendarItemDto>> _tasksByDate = new();

    protected bool IsLoading => LoadingService?.IsLoading ?? false;

    protected override async Task OnInitializedAsync()
    {
        await LoadInitialData();
    }

    private async Task LoadInitialData()
    {
        await AuthService.ExecuteWithGlobalLoadingAsync(LoadingService, async service =>
        {
            try
            {
                var isAuthenticated = await service.IsAuthenticatedAsync();
                if (!isAuthenticated)
                {
                    Navigation.NavigateTo("/login");
                    return;
                }

                var currentUser = service.GetCachedUser();
                if (currentUser == null)
                {
                    currentUser = await service.GetCurrentUserAsync();
                    if (currentUser == null)
                    {
                        Navigation.NavigateTo("/login");
                        return;
                    }
                }

                await LoadCalendarTasks();
            }
            catch (Exception)
            {
            }
            finally
            {
                StateHasChanged();
            }
        });
    }

    private async Task LoadCalendarTasks()
    {
        try
        {
            _isLoading = true;
            StateHasChanged();

            var currentUser = AuthService.GetCachedUser();
            if (currentUser == null) return;

            var userId = Guid.Parse(currentUser.Id);
            var tasks = await TaskService.GetForCalendarMonthAsync(userId, _currentMonth);

            _tasks = tasks.ToList();
            _tasksByDate = _tasks
                .Where(t => t.DueDate.HasValue)
                .GroupBy(t => t.DueDate!.Value.Date)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        catch (Exception)
        {
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task OnMonthChanged(DateTime month)
    {
        _currentMonth = month;
        await LoadCalendarTasks();
    }

    private void OnDateSelect(DateTime date)
    {
        _selectedDate = date;
        StateHasChanged();
    }

    private void HandleTaskClick(string taskId)
    {
        var task = _tasks.FirstOrDefault(t => t.Id.ToString() == taskId);
        if (task != null)
        {
            Navigation.NavigateTo($"/board/{task.BoardId}");
        }
    }

    private async Task RefreshCalendar()
    {
        await LoadCalendarTasks();
    }

    private List<TaskCalendarItemDto> GetTasksForDate(DateTime date)
    {
        _tasksByDate.TryGetValue(date.Date, out var tasks);
        return tasks ?? new List<TaskCalendarItemDto>();
    }

    private List<TaskCalendarItemDto> GetSelectedDateTasks()
    {
        return GetTasksForDate(_selectedDate);
    }

    private void NavigateToToday()
    {
        var today = DateTime.Today;
        _selectedDate = today;
        _currentMonth = today;
        InvokeAsync(LoadCalendarTasks);
    }

    private int GetTasksThisWeek()
    {
        var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(7);
        return _tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date >= startOfWeek && t.DueDate.Value.Date < endOfWeek);
    }

    private int GetOverdueTasks()
    {
        var today = DateTime.Today;
        return _tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date < today);
    }

    private BadgeStatus GetTaskBadgeStatus(TaskCalendarItemDto task)
    {
        if (!task.DueDate.HasValue)
            return BadgeStatus.Default;

        var today = DateTime.Today;
        var dueDate = task.DueDate.Value.Date;

        if (dueDate < today)
            return BadgeStatus.Error; // Overdue
        else if (dueDate == today)
            return BadgeStatus.Warning; // Due today
        else
            return BadgeStatus.Success; // Future task
    }

    private string GetTruncatedTitle(string title)
    {
        return title.Length > 15 ? title.Substring(0, 15) + "..." : title;
    }
}