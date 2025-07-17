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

namespace UI.Pages.Board;

public partial class BoardDetail : ComponentBase, IDisposable
{
    [Inject] private ISignalRService SignalRService { get; set; } = default!;
    [Parameter] public string BoardId { get; set; } = string.Empty;
    [CascadingParameter] public IGlobalLoadingService LoadingService { get; set; } = default!;
    
    [Inject] private IBoardService BoardService { get; set; } = default!;
    [Inject] private IBoardMemberService BoardMemberService { get; set; } = default!;
    [Inject] private ITaskStateService TaskStateService { get; set; } = default!;
    [Inject] private ITaskService TaskService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IMessageService Message { get; set; } = default!;

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
    private TaskItemDto? _selectedTask = null;
    private AddMemberModal.AddMemberForm _addMemberForm = new();
    private CreateStateRequest _addStateForm = new();
    private CreateTaskRequest _addTaskForm = new();

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

    protected override async Task OnInitializedAsync()
    {
        await LoadCurrentUser();
        await LoadBoardDetail();

        await SignalRService.ConnectAsync();
        await SignalRService.JoinBoardGroupAsync(BoardId);

        SignalRService.OnBoardUpdated(async payload =>
        {
            await InvokeAsync(async () => await LoadBoardDetail());
        });
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

    private bool HasBoardAccess()
    {
        if (_boardDetail == null || _currentUser == null)
            return false;

        if (_boardDetail.OwnerId == _currentUser.Id)
            return true;

        return _boardDetail.Members.Any(m => m.UserId == _currentUser.Id);
    }

    private bool CanManageMembers()
    {
        if (_boardDetail == null || _currentUser == null)
            return false;

        return _boardDetail.OwnerId == _currentUser.Id;
    }

    private async Task RefreshBoard()
    {
        await LoadBoardDetail();
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
            Message.Warning("Only board owners can add members");
            return;
        }
        
        _showAddMemberModal = true;
    }

    private void ShowCreateTaskModal()
    {
        if (!CanManageTasks())
        {
            Message.Warning("Only board owners and members can add tasks");
            return;
        }
        
        ResetAddTaskForm();
        _showAddTaskModal = true;
    }

    private void ShowManageStatesModal()
    {
        if (!CanManageStates())
        {
            Message.Warning("Only board owners and members can manage states");
            return;
        }
        _showManageStatesModal = true;
    }

    private void ShowArchiveBoardModal()
    {
        if (_boardDetail == null || _currentUser == null || _boardDetail.OwnerId != _currentUser.Id)
        {
            Message.Warning("Only the board owner can archive the board");
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
            Message.Success("Board archived successfully");
            Navigation.NavigateTo("/boards");
        }
        catch (Exception ex)
        {
            Message.Error($"Failed to archive board: {ex.Message}");
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
                    if (_boardDetail?.Members.Any(m => m.UserId == user.Id) == true)
                    {
                        errorMessages.Add($"{user.Username} is already a member of this board");
                        continue;
                    }

                    var request = new AddBoardMemberRequest
                    {
                        UserId = user.Id,
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
            await LoadBoardDetail();
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
            await LoadBoardDetail();
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
            await LoadBoardDetail();
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

        return _boardDetail.OwnerId == _currentUser.Id || 
               _boardDetail.Members.Any(m => m.UserId == _currentUser.Id);
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

        return _boardDetail.OwnerId == _currentUser.Id || 
               _boardDetail.Members.Any(m => m.UserId == _currentUser.Id);
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
            Message.Error("Please enter a task title");
            return;
        }

        if (_addTaskForm.StateId == 0)
        {
            Message.Error("Please select a state for the task");
            return;
        }

        try
        {
            _isAddingTask = true;
            StateHasChanged();

            var taskId = await TaskService.CreateAsync(_addTaskForm);
            
            await LoadBoardDetail();
            
            _showAddTaskModal = false;
            Message.Success($"Task '{_addTaskForm.Title}' created successfully");
        }
        catch (Exception ex)
        {
            Message.Error($"Failed to create task: {ex.Message}");
        }
        finally
        {
            _isAddingTask = false;
            StateHasChanged();
        }
    }
}
