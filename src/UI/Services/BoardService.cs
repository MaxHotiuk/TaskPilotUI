using UI.Models.Board;
using UI.Interfaces.Services;
using UI.Interfaces.Api;
using Refit;

namespace UI.Services;

public class BoardService : IBoardService
{
    private readonly IUserApi _userApi;
    private readonly IBoardApi _boardApi;
    private readonly IBoardMemberService _boardMemberService;
    private readonly ITaskService _taskService;
    private readonly ITaskStateService _taskStateService;
    private readonly IAuthService _authService;
    private readonly ILocalStorageService _localStorage;
    private const string BOARDS_CACHE_KEY = "cached_boards";
    private const string BOARD_STATS_CACHE_PREFIX = "cached_board_stats_";

    public BoardService(
        IUserApi userApi,
        IBoardApi boardApi,
        IBoardMemberService boardMemberService,
        ITaskService taskService,
        ITaskStateService taskStateService,
        IAuthService authService, 
        ILocalStorageService localStorage)
    {
        _userApi = userApi;
        _boardApi = boardApi;
        _boardMemberService = boardMemberService;
        _taskService = taskService;
        _taskStateService = taskStateService;
        _authService = authService;
        _localStorage = localStorage;
    }

    public async Task<List<BoardDto>> GetUserBoardsAsync(string userId)
    {
        try
        {
            var boards = await _userApi.GetUserBoardsAsync(userId);
            
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
            return await _boardApi.GetBoardByIdAsync(id);
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
            return await _boardApi.CreateBoardAsync(request);
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
            await _boardApi.UpdateBoardAsync(id, request);
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
            await _boardApi.DeleteBoardAsync(id);
        }
        catch (Exception)
        {
            throw;
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

            var tasks = await _taskService.GetBoardTasksAsync(boardId);
            var members = await _boardMemberService.GetBoardMembersAsync(boardId);
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

    public async Task<BoardDetailDto?> GetBoardDetailAsync(string boardId)
    {
        try
        {
            var board = await GetBoardByIdAsync(boardId);
            if (board == null)
            {
                return null;
            }

            var members = await _boardMemberService.GetBoardMembersAsync(boardId);
            var tasks = await _taskService.GetBoardTasksAsync(boardId);
            var states = await _taskStateService.GetBoardStatesAsync(boardId);

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
}
