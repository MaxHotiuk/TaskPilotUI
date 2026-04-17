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
    [Parameter] public Guid? OrganizationId { get; set; }
    [Parameter] public List<UserDto>? OrganizationUsers { get; set; }
    [Parameter] public EventCallback OnOk { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;

    private List<UserDto> AllUsers { get; set; } = new();
    private DateTime? ScheduledAtValue { get; set; }
    private IEnumerable<string> SelectedMemberIds { get; set; } = new List<string>();
    protected bool UseExternalLink { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {
        if (OrganizationUsers != null && OrganizationUsers.Any())
        {
            AllUsers = OrganizationUsers;
        }
        else
        {
            await LoadAllUsers();
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Update AllUsers when OrganizationUsers changes or is provided
        if (OrganizationUsers != null && OrganizationUsers.Any())
        {
            AllUsers = OrganizationUsers;
        }
        else if (IsVisible && OrganizationId.HasValue && AllUsers.Count == 0)
        {
            // Only reload if we don't have pre-loaded users
            _ = LoadAllUsers();
        }

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
            if (isAuthenticated && OrganizationId.HasValue)
            {
                AllUsers = await UserService.GetAllUsersAsync(OrganizationId.Value);
            }
            else if (!OrganizationId.HasValue)
            {
                Console.WriteLine($"AddMeetingModal - OrganizationId is not set, cannot load users");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AddMeetingModal - Error loading users: {ex.Message}");
        }
    }

    protected void HandleExternalLinkToggle(bool value)
    {
        UseExternalLink = value;
        if (!UseExternalLink)
        {
            FormModel.ExternalUrl = null;
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
        FormModel.Domain = Configuration["App:BaseUrl"]!;

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

    private async Task HandleSubmit(EditContext editContext)
    {
        await HandleOk();
    }

    private Task HandleSubmitFailed(EditContext editContext)
    {
        return Task.CompletedTask;
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
        UseExternalLink = false;
        FormModel.ExternalUrl = null;
    }
}