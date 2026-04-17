using Microsoft.AspNetCore.Components;
using UI.Models.Task;
using UI.Models.User;
using UI.Models.Meeting;
using UI.Interfaces.Services;
using UI.Extensions;
using System.Globalization;
using AntDesign;

namespace UI.Pages;

public partial class Calendar : ComponentBase
{
    [CascadingParameter] public IGlobalLoadingService LoadingService { get; set; } = default!;

    [Inject] private ITaskService TaskService { get; set; } = default!;
    [Inject] private IMeetingService MeetingService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IGoogleCalendarService GoogleCalendarService { get; set; } = default!;
    [Inject] private IMessageService Message { get; set; } = default!;


    private List<TaskCalendarItemDto> _tasks = new();
    private List<MeetingCalendarItemDto> _meetings = new();
    private DateTime _selectedDate = DateTime.Today;
    private DateTime _currentMonth = DateTime.Today;
    private bool _isLoading = false;
    private bool _isSyncing = false;
    private UserDto? _currentUser;
    private Dictionary<DateTime, List<TaskCalendarItemDto>> _tasksByDate = new();
    private Dictionary<DateTime, List<MeetingCalendarItemDto>> _meetingsByDate = new();

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

                _currentUser = currentUser;
                await LoadCalendarTasksAndMeetings();
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


    private async Task LoadCalendarTasksAndMeetings()
    {
        try
        {
            _isLoading = true;
            StateHasChanged();

            var currentUser = AuthService.GetCachedUser();
            if (currentUser == null) return;

            var userId = currentUser.Id;
            var tasks = await TaskService.GetForCalendarMonthAsync(userId, _currentMonth);
            var startDate = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            var meetings = await MeetingService.GetMeetingCalendarItemsAsync(userId, startDate, endDate);

            _tasks = tasks.ToList();
            _meetings = meetings.ToList();
            _tasksByDate = _tasks
                .Where(t => t.DueDate.HasValue)
                .GroupBy(t => t.DueDate!.Value.Date)
                .ToDictionary(g => g.Key, g => g.ToList());
            _meetingsByDate = _meetings
                .Where(m => m.ScheduledAt.HasValue)
                .GroupBy(m => m.ScheduledAt!.Value.Date)
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
        await LoadCalendarTasksAndMeetings();
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
        await LoadCalendarTasksAndMeetings();
    }


    private List<TaskCalendarItemDto> GetTasksForDate(DateTime date)
    {
        _tasksByDate.TryGetValue(date.Date, out var tasks);
        return tasks ?? new List<TaskCalendarItemDto>();
    }

    private List<MeetingCalendarItemDto> GetMeetingsForDate(DateTime date)
    {
        _meetingsByDate.TryGetValue(date.Date, out var meetings);
        return meetings ?? new List<MeetingCalendarItemDto>();
    }

    private List<TaskCalendarItemDto> GetSelectedDateTasks()
    {
        return GetTasksForDate(_selectedDate);
    }

    private List<MeetingCalendarItemDto> GetSelectedDateMeetings()
    {
        return GetMeetingsForDate(_selectedDate);
    }


    private void NavigateToToday()
    {
        var today = DateTime.Today;
        _selectedDate = today;
        _currentMonth = today;
        InvokeAsync(LoadCalendarTasksAndMeetings);
    }

    private async Task SyncToGoogleCalendar()
    {
        if (_currentUser == null) return;

        _isSyncing = true;
        StateHasChanged();
        try
        {
            await GoogleCalendarService.SyncCalendarAsync(_currentUser.Id, _currentMonth);
            Message.Success("Calendar synced to Google Calendar successfully.");
        }
        catch (Exception ex)
        {
            Message.Error($"Failed to sync calendar: {ex.Message}");
        }
        finally
        {
            _isSyncing = false;
            StateHasChanged();
        }
    }

    private async Task ConnectGoogleCalendar()
    {
        if (_currentUser == null) return;

        try
        {
            var url = await GoogleCalendarService.GetAuthorizationUrlAsync(_currentUser.Id);
            Navigation.NavigateTo(url, forceLoad: true);
        }
        catch (Exception ex)
        {
            Message.Error($"Failed to connect Google Calendar: {ex.Message}");
        }
    }
    private void HandleMeetingClick(string meetingId)
    {
        // Navigate to a meeting details page or board, adjust as needed
        var meeting = _meetings.FirstOrDefault(m => m.Id.ToString() == meetingId);
        if (meeting != null)
        {
            Navigation.NavigateTo($"/board/{meeting.BoardId}/meeting/{meeting.Id}");
        }
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