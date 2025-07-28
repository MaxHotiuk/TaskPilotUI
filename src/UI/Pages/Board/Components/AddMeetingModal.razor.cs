using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using UI.Models.Meeting;
using UI.Models.Member;
using UI.Models.User;
using UI.Interfaces.Services;
using AntDesign;

namespace UI.Pages.Board.Components;

public partial class AddMeetingModal : ComponentBase
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public CreateMeetingRequestDto FormModel { get; set; } = new();
    [Parameter] public List<BoardMemberDto> BoardMembers { get; set; } = new();
    [Parameter] public string BoardId { get; set; } = string.Empty;
    [Parameter] public EventCallback OnOk { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;

    private List<UserDto> AllUsers { get; set; } = new();
    private DateTime? ScheduledAtValue { get; set; }
    private IEnumerable<string> SelectedMemberIds { get; set; } = new List<string>();

    protected override async Task OnInitializedAsync()
    {
        await LoadAllUsers();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        
        if (!string.IsNullOrEmpty(BoardId) && FormModel.BoardId != Guid.Parse(BoardId))
        {
            FormModel.BoardId = Guid.Parse(BoardId);
        }

        if (!IsVisible)
        {
            ResetForm();
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
        }
    }

    private async Task HandleOk()
    {
        if (ScheduledAtValue.HasValue)
        {
            FormModel.ScheduledAt = ScheduledAtValue.Value;
        }
        else
        {
            FormModel.ScheduledAt = null;
        }

        FormModel.MemberIds = SelectedMemberIds.Select(Guid.Parse).ToList();

        if (OnOk.HasDelegate)
        {
            await OnOk.InvokeAsync();
        }
    }

    private async Task HandleCancel()
    {
        ResetForm();
        
        if (OnCancel.HasDelegate)
        {
            await OnCancel.InvokeAsync();
        }
    }

    private async Task HandleSubmit()
    {
        await HandleOk();
    }

    private void HandleSubmitFailed()
    {
    }

    private void ResetForm()
    {
        FormModel.Title = string.Empty;
        FormModel.Description = string.Empty;
        FormModel.Domain = string.Empty;
        FormModel.ScheduledAt = null;
        FormModel.Duration = 60; // Default to 1 hour
        FormModel.MemberIds = new List<Guid>();
        ScheduledAtValue = null;
        SelectedMemberIds = new List<string>();
    }
}