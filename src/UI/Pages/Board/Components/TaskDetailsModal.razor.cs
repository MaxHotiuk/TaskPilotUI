using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using AntDesign;
using UI.Models.Task;
using UI.Models.State;
using UI.Models.Member;
using UI.Models.User;
using UI.Interfaces.Services;

namespace UI.Pages.Board.Components;

public partial class TaskDetailsModal : ComponentBase
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public TaskItemDto? CurrentTask { get; set; }
    [Parameter] public List<StateDto> States { get; set; } = new();
    [Parameter] public List<BoardMemberDto> BoardMembers { get; set; } = new();
    [Parameter] public bool CanManageTask { get; set; }
    [Parameter] public EventCallback<TaskItemDto> OnTaskUpdated { get; set; }
    [Parameter] public EventCallback<string> OnTaskDeleted { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private ITaskService TaskService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;

    private List<UserDto> AllUsers { get; set; } = new();
    private UpdateTaskRequest FormModel { get; set; } = new();
    private bool IsEditing { get; set; } = false;
    private DateTime? DueDateValue { get; set; }
    private string DueDateString { get; set; } = string.Empty;
    private bool _internalLoading = false;
    private string? CurrentUserId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadAllUsers();
        await LoadCurrentUser();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        
        if (CurrentTask != null && IsVisible)
        {
            InitializeForm();
        }

        if (!IsVisible)
        {
            IsEditing = false;
        }
    }

    private void InitializeForm()
    {
        if (CurrentTask == null) return;

        FormModel = new UpdateTaskRequest
        {
            Title = CurrentTask.Title,
            Description = CurrentTask.Description,
            StateId = CurrentTask.StateId,
            AssigneeId = CurrentTask.AssigneeId,
            DueDate = CurrentTask.DueDate
        };

        if (!string.IsNullOrEmpty(CurrentTask.DueDate) && DateTime.TryParse(CurrentTask.DueDate, out var dueDate))
        {
            DueDateValue = dueDate;
            DueDateString = dueDate.ToString("yyyy-MM-dd");
        }
        else
        {
            DueDateValue = null;
            DueDateString = string.Empty;
        }
    }

    private async Task LoadAllUsers()
    {
        try
        {
            var currentUser = await AuthService.GetCurrentUserAsync();
            if (currentUser != null)
            {
                AllUsers = await UserService.GetAllUsersAsync();
            }
        }
        catch (Exception)
        {
            AllUsers = new List<UserDto>();
        }
    }

    private async Task LoadCurrentUser()
    {
        try
        {
            var currentUser = await AuthService.GetCurrentUserAsync();
            CurrentUserId = currentUser?.Id;
        }
        catch (Exception)
        {
            CurrentUserId = null;
        }
    }

    private RenderFragment GetModalFooter()
    {
        return builder =>
        {
            builder.OpenComponent<TaskModalFooter>(0);
            builder.AddAttribute(1, "IsEditing", IsEditing);
            builder.AddAttribute(2, "IsLoading", IsLoading || _internalLoading);
            builder.AddAttribute(3, "CanManageTask", CanManageTask);
            builder.AddAttribute(4, "OnCancelEdit", EventCallback.Factory.Create<MouseEventArgs>(this, _ => CancelEdit()));
            builder.AddAttribute(5, "OnSaveChanges", EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await SaveChanges()));
            builder.AddAttribute(6, "OnClose", EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await HandleCancel()));
            builder.AddAttribute(7, "OnEdit", EventCallback.Factory.Create<MouseEventArgs>(this, _ => StartEdit()));
            builder.AddAttribute(8, "OnDelete", EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await DeleteTask()));
            builder.CloseComponent();
        };
    }

    private void StartEdit()
    {
        IsEditing = true;
        InitializeForm();
        StateHasChanged();
    }

    private void CancelEdit()
    {
        IsEditing = false;
        InitializeForm();
        StateHasChanged();
    }

    private async Task HandleOk()
    {
        if (IsEditing)
        {
            await SaveChanges();
        }
        else
        {
            await HandleCancel();
        }
    }

    private async Task HandleCancel()
    {
        IsEditing = false;
        if (OnCancel.HasDelegate)
        {
            await OnCancel.InvokeAsync();
        }
    }

    private async Task SaveChanges()
    {
        if (CurrentTask == null) return;

        try
        {
            _internalLoading = true;
            StateHasChanged();

            if (!string.IsNullOrWhiteSpace(DueDateString) && DateTime.TryParse(DueDateString, out var dueDate))
            {
                FormModel.DueDate = dueDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            }
            else
            {
                FormModel.DueDate = null;
            }

            await TaskService.UpdateAsync(CurrentTask.Id, FormModel);

            CurrentTask.Title = FormModel.Title;
            CurrentTask.Description = FormModel.Description;
            CurrentTask.StateId = FormModel.StateId;
            CurrentTask.AssigneeId = FormModel.AssigneeId;
            CurrentTask.DueDate = FormModel.DueDate;
            CurrentTask.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            IsEditing = false;

            await NotificationService.Success(new NotificationConfig()
            {
                Message = "Success",
                Description = "Task updated successfully!"
            });

            if (OnTaskUpdated.HasDelegate)
            {
                await OnTaskUpdated.InvokeAsync(CurrentTask);
            }
        }
        catch (Exception ex)
        {
            await NotificationService.Error(new NotificationConfig()
            {
                Message = "Error",
                Description = $"Failed to update task: {ex.Message}"
            });
        }
        finally
        {
            _internalLoading = false;
            StateHasChanged();
        }
    }

    private async Task DeleteTask()
    {
        if (CurrentTask == null) return;

        try
        {
            _internalLoading = true;
            StateHasChanged();

            await TaskService.DeleteAsync(CurrentTask.Id);

            await NotificationService.Success(new NotificationConfig()
            {
                Message = "Success",
                Description = "Task deleted successfully!"
            });

            if (OnTaskDeleted.HasDelegate)
            {
                await OnTaskDeleted.InvokeAsync(CurrentTask.Id);
            }

            await HandleCancel();
        }
        catch (Exception ex)
        {
            await NotificationService.Error(new NotificationConfig()
            {
                Message = "Error",
                Description = $"Failed to delete task: {ex.Message}"
            });
        }
        finally
        {
            _internalLoading = false;
            StateHasChanged();
        }
    }

    private async Task MoveTaskToState(int newStateId)
    {
        if (CurrentTask == null || CurrentTask.StateId == newStateId) return;

        try
        {
            _internalLoading = true;
            StateHasChanged();

            var updateRequest = new UpdateTaskRequest
            {
                Title = CurrentTask.Title,
                Description = CurrentTask.Description,
                StateId = newStateId,
                AssigneeId = CurrentTask.AssigneeId,
                DueDate = CurrentTask.DueDate
            };

            await TaskService.UpdateAsync(CurrentTask.Id, updateRequest);

            CurrentTask.StateId = newStateId;
            CurrentTask.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            await NotificationService.Success(new NotificationConfig()
            {
                Message = "Success",
                Description = $"Task moved to {States.FirstOrDefault(s => s.Id == newStateId)?.Name ?? "Unknown"} successfully!"
            });

            if (OnTaskUpdated.HasDelegate)
            {
                await OnTaskUpdated.InvokeAsync(CurrentTask);
            }
        }
        catch (Exception ex)
        {
            await NotificationService.Error(new NotificationConfig()
            {
                Message = "Error",
                Description = $"Failed to move task: {ex.Message}"
            });
        }
        finally
        {
            _internalLoading = false;
            StateHasChanged();
        }
    }

    private async Task HandleSubmit()
    {
        await SaveChanges();
    }

    private void HandleSubmitFailed(EditContext editContext)
    {
        // Form validation failed - could add logic here if needed
    }

    private string FormatDate(string dateString)
    {
        if (DateTime.TryParse(dateString, out var date))
        {
            return date.ToString("MMM dd, yyyy HH:mm");
        }
        return dateString;
    }
}
