using UI.Interfaces.SignalR;
using Microsoft.AspNetCore.Components;
using UI.Models.Board;
using UI.Models.Task;
using UI.Models.State;
using UI.Models.Member;
using UI.Models.User;
using UI.Interfaces.Services;
using UI.Pages.Board.Components;
using UI.Extensions;
using AntDesign;
using UI.Models.Tag;
using UI.Models.Meeting;

namespace UI.Pages.Board;

public partial class BoardDetail : ComponentBase, IDisposable
{
    [Inject] private ISignalRService SignalRService { get; set; } = default!;
    [Parameter] public string BoardId { get; set; } = string.Empty;
    [SupplyParameterFromQuery(Name = "taskId")] public string? TaskId { get; set; }
    [CascadingParameter] public IGlobalLoadingService LoadingService { get; set; } = default!;

    [Inject] private IBoardService BoardService { get; set; } = default!;
    [Inject] private IBoardMemberService BoardMemberService { get; set; } = default!;
    [Inject] private ITaskStateService TaskStateService { get; set; } = default!;
    [Inject] private ITaskService TaskService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IMessageService Message { get; set; } = default!;
    [Inject] private IMeetingService MeetingService { get; set; } = default!;

    private BoardDetailDto? _boardDetail;
    private UserDto? _currentUser;
    private bool _showMembersModal = false;
    private bool _showAddMemberModal = false;
    private bool _isAddingMember = false;
    private bool _showAddTaskModal = false;
    private bool _isAddingTask = false;
    private bool _showTaskDetailsModal = false;
    private bool _isTaskDetailsLoading = false;
    private bool _showManageStatesModal = false;
    private bool _showManageTagsModal = false;
    private bool _showAddMeetingModal = false;
    private bool _isAddingMeeting = false;
    private bool _showManageMeetingsModal = false;
    private CreateMeetingRequestDto _addMeetingForm = new();
    private TaskItemDto? _selectedTask = null;
    private AddMemberModal.AddMemberForm _addMemberForm = new();
    private CreateStateRequest _addStateForm = new();
    private CreateTaskRequest _addTaskForm = new();
    public bool IsOnlyMine { get; set; } = false;
    private string? _lastTaskId;

    private bool _showArchiveBoardModal = false;
    private bool _isArchivingBoard = false;

    protected bool IsLoading => LoadingService?.IsLoading ?? false;

    protected override void OnInitialized()
    {
        if (LoadingService != null)
        {
            LoadingService.OnLoadingChanged += StateHasChanged;
        }
        base.OnInitialized();
    }

    private async Task OnOnlyMineToggle()
    {
        IsOnlyMine = !IsOnlyMine;
        if (IsOnlyMine)
        {
            await LoadBoardDetailOnlyMine();
        }
        else
        {
            await LoadBoardDetail();
        }
    }

    private async Task LoadBoardDetailOrMine()
    {
        if (IsOnlyMine)
        {
            await LoadBoardDetailOnlyMine();
        }
        else
        {
            await LoadBoardDetail();
        }
    }

    private void HandleTagsChanged(List<TagDto> tags)
    {
        if (_boardDetail != null)
        {
            _boardDetail.Tags = tags;
            StateHasChanged();
        }
    }

    private void HandleStatesChanged(List<StateDto> states)
    {
        if (_boardDetail != null)
        {
            _boardDetail.States = states;
            StateHasChanged();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadCurrentUser();
        await LoadBoardDetail();
        await TryOpenTaskFromRouteAsync(TaskId);

        await SignalRService.ConnectAsync();
        await SignalRService.JoinBoardGroupAsync(BoardId);

        SignalRService.OnBoardUpdated(async payload =>
        {
            await InvokeAsync(async () => await LoadBoardDetail());
        });
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (!string.IsNullOrWhiteSpace(TaskId) && TaskId != _lastTaskId)
        {
            await TryOpenTaskFromRouteAsync(TaskId);
        }
    }

    public void Dispose()
    {
        if (LoadingService != null)
        {
            LoadingService.OnLoadingChanged -= StateHasChanged;
        }

        _ = SignalRService.LeaveBoardGroupAsync(BoardId);
        _ = SignalRService.DisconnectAsync();
    }

    private async Task LoadCurrentUser()
    {
        var isAuthenticated = await AuthService.IsAuthenticatedAsync();
        if (!isAuthenticated)
        {
            Navigation.NavigateTo("/login");
            return;
        }

        _currentUser = AuthService.GetCachedUser();
        if (_currentUser == null)
        {
            _currentUser = await AuthService.GetCurrentUserAsync();
            if (_currentUser == null)
            {
                Navigation.NavigateTo("/login");
                return;
            }
        }
    }

    private async Task LoadBoardDetailOnlyMine()
    {
        await BoardService.ExecuteWithGlobalLoadingAndErrorHandlingAsync(
            LoadingService,
            async service =>
            {
                _boardDetail = await service.GetDetailAsync(BoardId);

                if (_boardDetail == null)
                {
                    Message.Error(UI.Resources.I18n.BoardNotFoundOrAccessDeniedMessage);
                    return;
                }

                if (!HasBoardAccess())
                {
                    Message.Error(UI.Resources.I18n.YouDontHaveAccessToBoard);
                    Navigation.NavigateTo("/boards");
                    return;
                }

                for (int i = 0; i < _boardDetail.Tasks.Count; i++)
                {
                    var task = _boardDetail.Tasks[i];
                    if (task.AssigneeId != _currentUser?.Id.ToString())
                    {
                        _boardDetail.Tasks.RemoveAt(i);
                        i--;
                    }
                }

                await TryOpenTaskFromRouteAsync(TaskId);
            },
            onError: ex =>
            {
                Message.Error(string.Format(UI.Resources.I18n.FailedToLoadBoard, ex.Message));
                return Task.CompletedTask;
            },
            onFinally: () =>
            {
                StateHasChanged();
                return Task.CompletedTask;
            });
    }

    private async Task LoadBoardDetail()
    {
        await BoardService.ExecuteWithGlobalLoadingAndErrorHandlingAsync(
            LoadingService,
            async service =>
            {
                _boardDetail = await service.GetDetailAsync(BoardId);

                if (_boardDetail == null)
                {
                    Message.Error("Board not found or access denied");
                    return;
                }

                if (!HasBoardAccess())
                {
                    Message.Error("You don't have access to this board");
                    Navigation.NavigateTo("/boards");
                    return;
                }

                await TryOpenTaskFromRouteAsync(TaskId);
            },
            onError: ex =>
            {
                Message.Error($"Failed to load board: {ex.Message}");
                return Task.CompletedTask;
            },
            onFinally: () =>
            {
                StateHasChanged();
                return Task.CompletedTask;
            });
    }

    private async Task TryOpenTaskFromRouteAsync(string? taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId) || _boardDetail == null)
            return;

        if (taskId == _lastTaskId && _showTaskDetailsModal)
            return;

        _lastTaskId = taskId;
        TaskItemDto? task = _boardDetail.Tasks.FirstOrDefault(item => item.Id == taskId);

        if (task == null)
        {
            try
            {
                _isTaskDetailsLoading = true;
                StateHasChanged();
                task = await TaskService.GetByIdAsync(taskId);
            }
            catch (Exception ex)
            {
                _isTaskDetailsLoading = false;
                Message.Error($"Failed to load task: {ex.Message}");
                return;
            }
        }

        _isTaskDetailsLoading = false;

        if (task == null || !string.Equals(task.BoardId, BoardId, StringComparison.OrdinalIgnoreCase))
            return;

        _selectedTask = task;
        _showTaskDetailsModal = true;
        StateHasChanged();
    }

    private bool HasBoardAccess()
    {
        if (_boardDetail == null || _currentUser == null)
            return false;

        if (_boardDetail.OwnerId == _currentUser.Id.ToString())
            return true;

        return _boardDetail.Members.Any(m => m.UserId == _currentUser.Id.ToString());
    }

    private bool CanManageMembers()
    {
        if (_boardDetail == null || _currentUser == null)
            return false;

        return _boardDetail.OwnerId == _currentUser.Id.ToString();
    }

    private async Task RefreshBoard()
    {
        await LoadBoardDetailOrMine();
    }

    private void GoBack()
    {
        Navigation.NavigateTo("/boards");
    }

    private void ShowMembersModal()
    {
        _showMembersModal = true;
    }

    private void ShowAddMemberModal()
    {
        if (!CanManageMembers())
        {
            Message.Warning(UI.Resources.I18n.OnlyBoardOwnersCanAddMembers);
            return;
        }

        _showAddMemberModal = true;
    }

    private void ShowCreateTaskModal()
    {
        if (!CanManageTasks())
        {
            Message.Warning(UI.Resources.I18n.OnlyOwnersAndMembersCanAddTasks);
            return;
        }

        ResetAddTaskForm();
        _showAddTaskModal = true;
    }

    private void ShowManageStatesModal()
    {
        if (!CanManageStates())
        {
            Message.Warning(UI.Resources.I18n.OnlyOwnersAndMembersCanManageStates);
            return;
        }
        _showManageStatesModal = true;
    }

    private void ShowManageTagsModal()
    {
        if (!CanManageStates())
        {
            Message.Warning(UI.Resources.I18n.OnlyOwnersAndMembersCanManageTags);
            return;
        }
        _showManageTagsModal = true;
    }

    private void ShowArchiveBoardModal()
    {
        if (_boardDetail == null || _currentUser == null || _boardDetail.OwnerId != _currentUser.Id.ToString())
        {
            Message.Warning(UI.Resources.I18n.OnlyBoardOwnerCanArchive);
            return;
        }
        _showArchiveBoardModal = true;
    }

    private async Task ArchiveBoard()
    {
        if (_boardDetail == null)
            return;

        try
        {
            _isArchivingBoard = true;
            await BoardService.ArchiveBoardAsync(_boardDetail.Id);
            Message.Success(UI.Resources.I18n.BoardArchivedSuccess);
            Navigation.NavigateTo("/boards");
        }
        catch (Exception ex)
        {
            Message.Error(string.Format(UI.Resources.I18n.FailedToArchiveBoard, ex.Message));
        }
        finally
        {
            _isArchivingBoard = false;
            _showArchiveBoardModal = false;
            StateHasChanged();
        }
    }

    private void ShowTaskDetails(TaskItemDto task)
    {
        _selectedTask = task;
        _showTaskDetailsModal = true;
    }

    private void HandleTaskUpdated(TaskItemDto updatedTask)
    {
        if (_boardDetail?.Tasks != null)
        {
            var taskIndex = _boardDetail.Tasks.FindIndex(t => t.Id == updatedTask.Id);
            if (taskIndex >= 0)
            {
                _boardDetail.Tasks[taskIndex] = updatedTask;
                StateHasChanged();
            }
        }
    }

    private void HandleTaskDeleted(string taskId)
    {
        if (_boardDetail?.Tasks != null)
        {
            _boardDetail.Tasks.RemoveAll(t => t.Id == taskId);
            _showTaskDetailsModal = false;
            _selectedTask = null;
            StateHasChanged();
        }
    }

    private async Task AddMember()
    {
        if (!_addMemberForm.SelectedUsers.Any())
        {
            Message.Error("Please select at least one user to add");
            return;
        }

        try
        {
            _isAddingMember = true;
            var successCount = 0;
            var errorMessages = new List<string>();

            foreach (var user in _addMemberForm.SelectedUsers)
            {
                try
                {
                    if (_boardDetail?.Members.Any(m => m.UserId == user.Id.ToString()) == true)
                    {
                        errorMessages.Add($"{user.Username} is already a member of this board");
                        continue;
                    }

                    var request = new AddBoardMemberRequest
                    {
                        UserId = user.Id.ToString(),
                        Role = _addMemberForm.Role
                    };

                    await BoardMemberService.AddAsync(BoardId, request);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorMessages.Add($"Failed to add {user.Username}: {ex.Message}");
                }
            }

            if (successCount > 0)
            {
                var message = successCount == 1
                    ? $"Successfully added 1 member to the board"
                    : $"Successfully added {successCount} members to the board";
                Message.Success(message);
            }

            if (errorMessages.Any())
            {
                foreach (var error in errorMessages)
                {
                    Message.Warning(error);
                }
            }

            _showAddMemberModal = false;
            ResetAddMemberForm();
            await LoadBoardDetailOrMine();
        }
        catch (Exception ex)
        {
            Message.Error($"Failed to add members: {ex.Message}");
        }
        finally
        {
            _isAddingMember = false;
            StateHasChanged();
        }
    }

    private async Task ChangeRole(BoardMemberDto member, string newRole)
    {
        if (member.Role == newRole)
            return;

        try
        {
            var request = new UpdateBoardMemberRoleRequest { Role = newRole };
            await BoardMemberService.UpdateRoleAsync(BoardId, member.UserId, request);
            Message.Success($"Successfully updated member role to {newRole}");
            await LoadBoardDetailOrMine();
        }
        catch (Exception ex)
        {
            Message.Error($"Failed to update member role: {ex.Message}");
        }
    }

    private async Task RemoveMember(BoardMemberDto member)
    {
        try
        {
            await BoardMemberService.RemoveAsync(BoardId, member.UserId);
            Message.Success("Successfully removed member from board");
            await LoadBoardDetailOrMine();
        }
        catch (Exception ex)
        {
            Message.Error($"Failed to remove member: {ex.Message}");
        }
    }

    private void ResetAddMemberForm()
    {
        _addMemberForm = new AddMemberModal.AddMemberForm();
    }

    private async Task HandleChangeRole((BoardMemberDto member, string role) args)
    {
        await ChangeRole(args.member, args.role);
    }

    private async Task HandleRemoveMember(BoardMemberDto member)
    {
        await RemoveMember(member);
    }
    private bool CanManageStates()
    {
        if (_boardDetail == null || _currentUser == null)
            return false;

        return _boardDetail.OwnerId == _currentUser.Id.ToString() ||
               _boardDetail.Members.Any(m => m.UserId == _currentUser.Id.ToString());
    }

    private void ResetAddStateForm()
    {
        _addStateForm = new CreateStateRequest();
        if (_boardDetail?.States != null && _boardDetail.States.Any())
        {
            _addStateForm.Order = _boardDetail.States.Max(s => s.Order) + 1;
        }
        else
        {
            _addStateForm.Order = 1;
        }
    }

    private bool CanManageTasks()
    {
        if (_boardDetail == null || _currentUser == null)
            return false;

        return _boardDetail.OwnerId == _currentUser.Id.ToString() ||
               _boardDetail.Members.Any(m => m.UserId == _currentUser.Id.ToString());
    }

    private void ResetAddTaskForm()
    {
        _addTaskForm = new CreateTaskRequest
        {
            BoardId = BoardId
        };

        if (_boardDetail?.States != null && _boardDetail.States.Any())
        {
            _addTaskForm.StateId = _boardDetail.States.First().Id;
        }
    }

    private async Task AddTask()
    {
        if (string.IsNullOrWhiteSpace(_addTaskForm.Title))
        {
            Message.Error(UI.Resources.I18n.PleaseEnterTaskTitle);
            return;
        }

        if (_addTaskForm.StateId == 0)
        {
            Message.Error(UI.Resources.I18n.PleaseSelectState);
            return;
        }

        try
        {
            _isAddingTask = true;
            StateHasChanged();

            var taskId = await TaskService.CreateAsync(_addTaskForm);

            await LoadBoardDetailOrMine();

            _showAddTaskModal = false;
            Message.Success(string.Format(UI.Resources.I18n.TaskCreatedSuccess, _addTaskForm.Title));
        }
        catch (Exception ex)
        {
            Message.Error(string.Format(UI.Resources.I18n.FailedToCreateTask, ex.Message));
        }
        finally
        {
            _isAddingTask = false;
            StateHasChanged();
        }
    }

    private void ShowCreateMeetingModal()
    {
        if (!CanManageTasks())
        {
            Message.Warning(UI.Resources.I18n.OnlyOwnersCanScheduleMeetings);
            return;
        }

        ResetAddMeetingForm();
        _showAddMeetingModal = true;
    }

    private void ResetAddMeetingForm()
    {
        _addMeetingForm = new CreateMeetingRequestDto
        {
            BoardId = Guid.Parse(BoardId),
            CreatedBy = _currentUser != null ? _currentUser.Id : Guid.Empty,
            Duration = 60 // Default to 1 hour
        };
    }

    private async Task AddMeeting()
    {
        if (string.IsNullOrWhiteSpace(_addMeetingForm.Title))
        {
            Message.Error(UI.Resources.I18n.PleaseEnterMeetingTitle);
            return;
        }

        if (string.IsNullOrWhiteSpace(_addMeetingForm.Domain))
        {
            Message.Error(UI.Resources.I18n.PleaseEnterMeetingDomain);
            return;
        }

        try
        {
            _isAddingMeeting = true;
            StateHasChanged();

            var meetingId = await MeetingService.CreateMeetingAsync(_addMeetingForm);

            _showAddMeetingModal = false;
            Message.Success(string.Format(UI.Resources.I18n.MeetingScheduledSuccess, _addMeetingForm.Title));

            await LoadBoardDetailOrMine();
        }
        catch (Exception ex)
        {
            Message.Error($"Failed to schedule meeting: {ex.Message}");
        }
        finally
        {
            _isAddingMeeting = false;
            StateHasChanged();
        }
    }
    
    private void ShowManageMeetingsModal()
    {
        if (!CanManageTasks())
        {
            Message.Warning(UI.Resources.I18n.OnlyOwnersCanScheduleMeetings);
            return;
        }

        _showManageMeetingsModal = true;
    }

    private async Task HandleMeetingChanged()
    {
        await Task.CompletedTask;
    }
}
