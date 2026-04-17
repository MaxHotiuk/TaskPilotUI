using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using UI.Models.Task;
using UI.Models.State;
using UI.Models.Member;
using UI.Models.User;
using UI.Interfaces.Services;
using UI.Models.Tag;

namespace UI.Pages.Board.Components;

public partial class AddTaskModal : ComponentBase
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public CreateTaskRequest FormModel { get; set; } = new();
    [Parameter] public List<StateDto> States { get; set; } = new();
    [Parameter] public List<TagDto> Tags { get; set; } = new();
    [Parameter] public List<BoardMemberDto> BoardMembers { get; set; } = new();
    [Parameter] public string BoardId { get; set; } = string.Empty;
    [Parameter] public Guid? OrganizationId { get; set; }
    [Parameter] public List<UserDto>? OrganizationUsers { get; set; }
    [Parameter] public EventCallback OnOk { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;

    private List<UserDto> AllUsers { get; set; } = new();
    private DateTime? DueDateValue { get; set; }
    private string DueDateString { get; set; } = string.Empty;

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

        if (!string.IsNullOrEmpty(BoardId) && FormModel.BoardId != BoardId)
        {
            FormModel.BoardId = BoardId;
        }

        if (States.Any() && FormModel.StateId == 0)
        {
            FormModel.StateId = States.First().Id;
        }

        if (Tags.Any() && FormModel.TagId == null)
        {
            FormModel.TagId = Tags.First().Id;
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
                Console.WriteLine($"AddTaskModal - OrganizationId is not set, cannot load users");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading users: {ex.Message}");
        }
    }

    private async Task HandleOk()
    {
        if (!string.IsNullOrEmpty(DueDateString))
        {
            if (DateTime.TryParse(DueDateString, out var dateValue))
            {
                FormModel.DueDate = dateValue.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            }
        }
        else
        {
            FormModel.DueDate = null;
        }

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
        FormModel.Description = null;
        FormModel.AssigneeId = null;
        FormModel.DueDate = null;
        FormModel.Priority = 2;
        DueDateValue = null;
        DueDateString = string.Empty;

        if (States.Any())
        {
            FormModel.StateId = States.First().Id;
        }
        else
        {
            FormModel.StateId = 0;
        }

        if (Tags.Any())
        {
            FormModel.TagId = Tags.First().Id;
        }
        else
        {
            FormModel.TagId = null;
        }
    }

    private void OnDueDateChanged(DateTime? value)
    {
        DueDateValue = value;
    }
}
