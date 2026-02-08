using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using UI.Models.Meeting;
using UI.Models.Member;
using UI.Models.User;
using UI.Interfaces.Services;
using AntDesign;

namespace UI.Pages.Board.Components;

public partial class ManageMeetingsModal : ComponentBase
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public List<BoardMemberDto> BoardMembers { get; set; } = new();
    [Parameter] public string BoardId { get; set; } = string.Empty;
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback OnMeetingChanged { get; set; }

    [Inject] private IMeetingService MeetingService { get; set; } = default!;
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private IMessageService Message { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;

    private List<MeetingDto> Meetings { get; set; } = new();
    private List<UserDto> AllUsers { get; set; } = new();
    
    // Form state
    private bool _showMeetingForm = false;
    private bool _isSubmittingForm = false;
    private bool IsEditMode => _editingMeetingId != null;
    private Guid? _editingMeetingId = null;
    private Guid? DeletingMeetingId = null;
    
    // Form data
    private CreateMeetingRequestDto _meetingForm = new();
    private DateTime? _scheduledAtValue;
    private IEnumerable<string> _selectedMemberIds = new List<string>();

    protected override async Task OnInitializedAsync()
    {
        await LoadAllUsers();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (IsVisible && !string.IsNullOrEmpty(BoardId))
        {
            await LoadMeetings();
        }
        
        if (!IsVisible)
        {
            _showMeetingForm = false;
            _editingMeetingId = null;
        }
    }

    private async Task LoadAllUsers()
    {
        try
        {
            var isAuthenticated = await AuthService.IsAuthenticatedAsync();
            if (isAuthenticated)
            {
                AllUsers = await UserService.GetAllUsersAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading users: {ex.Message}");
            Message.Error(UI.Resources.I18n.FailedToLoadUsers);
        }
    }

    private async Task LoadMeetings()
    {
        try
        {
            if (Guid.TryParse(BoardId, out var boardGuid))
            {
                Meetings = await MeetingService.GetMeetingsByBoardIdAsync(boardGuid);
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading meetings: {ex.Message}");
            Message.Error(UI.Resources.I18n.FailedToLoadMeetings);
        }
    }

    private void ShowAddMeetingForm()
    {
        _editingMeetingId = null;
        ResetMeetingForm();
        _showMeetingForm = true;
    }

    private void ShowEditMeetingForm(MeetingDto meeting)
    {
        _editingMeetingId = meeting.Id;
        PopulateMeetingForm(meeting);
        _showMeetingForm = true;
    }

    private void ResetMeetingForm()
    {
        var currentUser = AuthService.GetCachedUser();
        _meetingForm = new CreateMeetingRequestDto
        {
            BoardId = Guid.Parse(BoardId),
            CreatedBy = currentUser != null ? currentUser.Id : Guid.Empty,
            Domain = Configuration["App:BaseUrl"]!,
            Duration = 60
        };
        _scheduledAtValue = null;
        _selectedMemberIds = new List<string>();
    }

    private void PopulateMeetingForm(MeetingDto meeting)
    {
        _meetingForm = new CreateMeetingRequestDto
        {
            Title = meeting.Title,
            Description = meeting.Description,
            BoardId = Guid.Parse(BoardId),
            CreatedBy = meeting.CreatedBy,
            Duration = meeting.Duration,
            Domain = Configuration["App:BaseUrl"]!,
        };
        
        _scheduledAtValue = meeting.ScheduledAt;
        _selectedMemberIds = meeting.MemberIds?.Select(id => id.ToString()) ?? new List<string>();
    }

    private async Task HandleMeetingFormOk()
    {
        await HandleMeetingFormSubmit(new EditContext(_meetingForm));
    }

    private void HandleMeetingFormCancel()
    {
        _showMeetingForm = false;
        _editingMeetingId = null;
        ResetMeetingForm();
    }

    private async Task HandleMeetingFormSubmit(EditContext editContext)
    {
        if (string.IsNullOrWhiteSpace(_meetingForm.Title))
        {
            Message.Error(UI.Resources.I18n.PleaseEnterMeetingTitle);
            return;
        }

        if (string.IsNullOrWhiteSpace(_meetingForm.Domain))
        {
            Message.Error(UI.Resources.I18n.PleaseEnterMeetingDomain);
            return;
        }

        try
        {
            _isSubmittingForm = true;
            StateHasChanged();

            _meetingForm.ScheduledAt = _scheduledAtValue;
            _meetingForm.MemberIds = _selectedMemberIds.Select(Guid.Parse).ToList();

            if (IsEditMode)
            {
                await MeetingService.UpdateMeetingAsync(
                    _editingMeetingId!.Value,
                    _meetingForm.Title,
                    _meetingForm.Description ?? string.Empty,
                    _meetingForm.ScheduledAt ?? DateTime.Now,
                    _meetingForm.Duration ?? 60
                );
                Message.Success(UI.Resources.I18n.MeetingUpdatedSuccess);
            }
            else
            {
                await MeetingService.CreateMeetingAsync(_meetingForm);
                Message.Success(string.Format(UI.Resources.I18n.MeetingScheduledSuccess, _meetingForm.Title));
            }

            _showMeetingForm = false;
            _editingMeetingId = null;
            await LoadMeetings();

            if (OnMeetingChanged.HasDelegate)
            {
                await OnMeetingChanged.InvokeAsync();
            }
        }
        catch (Exception ex)
        {
            Message.Error(string.Format(UI.Resources.I18n.FailedToUpdateMeeting, ex.Message));
        }
        finally
        {
            _isSubmittingForm = false;
            StateHasChanged();
        }
    }

    private async Task DeleteMeeting(string meetingId)
    {
        try
        {
            DeletingMeetingId = Guid.Parse(meetingId);
            StateHasChanged();

            await MeetingService.DeleteMeetingAsync(Guid.Parse(meetingId));
            Message.Success(UI.Resources.I18n.MeetingDeletedSuccess);
            
            await LoadMeetings();
            
            if (OnMeetingChanged.HasDelegate)
            {
                await OnMeetingChanged.InvokeAsync();
            }
        }
        catch (Exception ex)
        {
            Message.Error(string.Format(UI.Resources.I18n.FailedToDeleteMeeting, ex.Message));
        }
        finally
        {
            DeletingMeetingId = null;
            StateHasChanged();
        }
    }

    private async Task HandleCancel()
    {
        _showMeetingForm = false;
        _editingMeetingId = null;
        
        if (OnCancel.HasDelegate)
        {
            await OnCancel.InvokeAsync();
        }
    }

    private string GetMeetingStatus(MeetingDto meeting)
    {
        if (!meeting.ScheduledAt.HasValue)
            return "Unscheduled";

        var now = DateTime.Now;
        var meetingStart = meeting.ScheduledAt.Value;
        var meetingEnd = meeting.Duration.HasValue 
            ? meetingStart.AddMinutes(meeting.Duration.Value) 
            : meetingStart.AddHours(1);

        if (now < meetingStart)
            return "Upcoming";
        else if (now >= meetingStart && now <= meetingEnd)
            return "In Progress";
        else
            return "Completed";
    }

    private string GetMeetingStatusColor(MeetingDto meeting)
    {
        var status = GetMeetingStatus(meeting);
        return status switch
        {
            "Upcoming" => "blue",
            "In Progress" => "green",
            "Completed" => "default",
            "Unscheduled" => "orange",
            _ => "default"
        };
    }
}