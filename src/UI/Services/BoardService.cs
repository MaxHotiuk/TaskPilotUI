using UI.Models.Board;
using UI.Models.Member;
using UI.Models.Task;
using UI.Models.State;
using UI.Models.User;
using Refit;
using UI.Interfaces.Services;
using UI.Interfaces.Api;

namespace UI.Services;

public class BoardService : IBoardService
{
    private readonly ITaskPilotApi _taskPilotApi;
    private readonly IAuthService _authService;
    private readonly ILocalStorageService _localStorage;
    private const string BOARDS_CACHE_KEY = "cached_boards";
    private const string BOARD_STATS_CACHE_PREFIX = "cached_board_stats_";

    public BoardService(ITaskPilotApi taskPilotApi, IAuthService authService, ILocalStorageService localStorage)
    {
        _taskPilotApi = taskPilotApi;
        _authService = authService;
        _localStorage = localStorage;
    }

    public async Task<List<BoardDto>> GetUserBoardsAsync(string userId)
    {
        try
        {
            var boards = await _taskPilotApi.GetUserBoardsAsync(userId);
            
            await _localStorage.SetItemAsync($"{BOARDS_CACHE_KEY}_{userId}", boards);
            
            return boards;
        }
        catch (ApiException)
        {
            var cachedBoards = await _localStorage.GetItemAsync<List<BoardDto>>($"{BOARDS_CACHE_KEY}_{userId}");
            return cachedBoards ?? new List<BoardDto>();
        }
        catch (Exception)
        {
            var cachedBoards = await _localStorage.GetItemAsync<List<BoardDto>>($"{BOARDS_CACHE_KEY}_{userId}");
            return cachedBoards ?? new List<BoardDto>();
        }
    }

    public async Task<BoardDto?> GetBoardByIdAsync(string id)
    {
        try
        {
            return await _taskPilotApi.GetBoardByIdAsync(id);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<string> CreateBoardAsync(CreateBoardRequest request)
    {
        try
        {
            return await _taskPilotApi.CreateBoardAsync(request);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task UpdateBoardAsync(string id, CreateBoardRequest request)
    {
        try
        {
            await _taskPilotApi.UpdateBoardAsync(id, request);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task DeleteBoardAsync(string id)
    {
        try
        {
            await _taskPilotApi.DeleteBoardAsync(id);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<BoardMemberDto>> GetBoardMembersAsync(string boardId)
    {
        try
        {
            return await _taskPilotApi.GetBoardMembersAsync(boardId);
        }
        catch (Exception)
        {
            return new List<BoardMemberDto>();
        }
    }

    public async Task<List<TaskItemDto>> GetBoardTasksAsync(string boardId)
    {
        try
        {
            return await _taskPilotApi.GetBoardTasksAsync(boardId);
        }
        catch (Exception)
        {
            return new List<TaskItemDto>();
        }
    }

    public async Task<BoardWithStats> GetBoardWithStatsAsync(string boardId)
    {
        try
        {
            var board = await GetBoardByIdAsync(boardId);
            if (board == null)
            {
                return new BoardWithStats();
            }

            var tasks = await GetBoardTasksAsync(boardId);
            var members = await GetBoardMembersAsync(boardId);
            var currentUser = _authService.GetCachedUser();
            
            var boardWithStats = new BoardWithStats
            {
                Board = board,
                TaskCount = tasks.Count,
                MemberCount = members.Count,
                Members = members,
                IsOwner = currentUser != null && board.OwnerId == currentUser.Id
            };

            await _localStorage.SetItemAsync($"{BOARD_STATS_CACHE_PREFIX}{boardId}", boardWithStats);
            
            return boardWithStats;
        }
        catch (Exception)
        {
            var cachedStats = await _localStorage.GetItemAsync<BoardWithStats>($"{BOARD_STATS_CACHE_PREFIX}{boardId}");
            return cachedStats ?? new BoardWithStats();
        }
    }

    public async Task<List<BoardDto>> GetCachedUserBoardsAsync(string userId)
    {
        var cachedBoards = await _localStorage.GetItemAsync<List<BoardDto>>($"{BOARDS_CACHE_KEY}_{userId}");
        return cachedBoards ?? new List<BoardDto>();
    }

    public async Task<BoardWithStats> GetCachedBoardWithStatsAsync(string boardId)
    {
        var cachedStats = await _localStorage.GetItemAsync<BoardWithStats>($"{BOARD_STATS_CACHE_PREFIX}{boardId}");
        return cachedStats ?? new BoardWithStats();
    }

    public async Task ClearBoardCacheAsync(string userId)
    {
        await _localStorage.RemoveItemAsync($"{BOARDS_CACHE_KEY}_{userId}");
    }

    public async Task<List<StateDto>> GetBoardStatesAsync(string boardId)
    {
        try
        {
            return await _taskPilotApi.GetBoardStatesAsync(boardId);
        }
        catch (Exception)
        {
            return new List<StateDto>();
        }
    }

    public async Task<int> CreateStateAsync(string boardId, CreateStateRequest request)
    {
        try
        {
            return await _taskPilotApi.CreateStateAsync(boardId, request);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BoardDetailDto?> GetBoardDetailAsync(string boardId)
    {
        try
        {
            var board = await GetBoardByIdAsync(boardId);
            if (board == null)
            {
                return null;
            }

            var members = await GetBoardMembersAsync(boardId);
            var tasks = await GetBoardTasksAsync(boardId);
            var states = await GetBoardStatesAsync(boardId);

            return new BoardDetailDto
            {
                Id = board.Id,
                Name = board.Name,
                Description = board.Description,
                OwnerId = board.OwnerId,
                CreatedAt = board.CreatedAt,
                UpdatedAt = board.UpdatedAt,
                Members = members,
                Tasks = tasks,
                States = states
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task AddBoardMemberAsync(string boardId, AddBoardMemberRequest request)
    {
        try
        {
            await _taskPilotApi.AddBoardMemberAsync(boardId, request);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task UpdateBoardMemberRoleAsync(string boardId, string userId, UpdateBoardMemberRoleRequest request)
    {
        try
        {
            await _taskPilotApi.UpdateBoardMemberRoleAsync(boardId, userId, request);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task RemoveBoardMemberAsync(string boardId, string userId)
    {
        try
        {
            await _taskPilotApi.RemoveBoardMemberAsync(boardId, userId);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        try
        {
            return await _taskPilotApi.GetUserByEmailAsync(email);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        try
        {
            return await _taskPilotApi.GetAllUsersAsync();
        }
        catch (Exception)
        {
            return new List<UserDto>();
        }
    }
}
