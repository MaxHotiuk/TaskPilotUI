using UI.Models.Board;
using UI.Models.Member;
using UI.Models.Task;
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

    private async Task<string> GetAuthTokenAsync()
    {
        var token = await _authService.GetAccessTokenAsync();
        return string.IsNullOrEmpty(token) ? "" : $"Bearer {token}";
    }

    public async Task<List<BoardDto>> GetUserBoardsAsync(string userId)
    {
        try
        {
            var token = await GetAuthTokenAsync();
            var boards = await _taskPilotApi.GetUserBoardsAsync(userId, token);
            
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
            var token = await GetAuthTokenAsync();
            return await _taskPilotApi.GetBoardByIdAsync(id, token);
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
            var token = await GetAuthTokenAsync();
            return await _taskPilotApi.CreateBoardAsync(request, token);
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
            var token = await GetAuthTokenAsync();
            await _taskPilotApi.UpdateBoardAsync(id, request, token);
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
            var token = await GetAuthTokenAsync();
            await _taskPilotApi.DeleteBoardAsync(id, token);
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
            var token = await GetAuthTokenAsync();
            return await _taskPilotApi.GetBoardMembersAsync(boardId, token);
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
            var token = await GetAuthTokenAsync();
            return await _taskPilotApi.GetBoardTasksAsync(boardId, token);
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
}
