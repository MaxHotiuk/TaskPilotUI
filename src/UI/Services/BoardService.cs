using UI.Models.Board;
using UI.Interfaces.Services;
using UI.Interfaces.Api;
using Refit;
using UI.Models.Backlog;

namespace UI.Services;

public class BoardService : IBoardService
{
    private readonly IUserApi _userApi;
    private readonly IBoardApi _boardApi;
    private readonly IBoardMemberService _boardMemberService;
    private readonly ITaskService _taskService;
    private readonly ITagService _tagService;
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
        ITagService tagService,
        ITaskStateService taskStateService,
        IAuthService authService,
        ILocalStorageService localStorage)
    {
        _userApi = userApi;
        _boardApi = boardApi;
        _boardMemberService = boardMemberService;
        _taskService = taskService;
        _tagService = tagService;
        _taskStateService = taskStateService;
        _authService = authService;
        _localStorage = localStorage;
    }

    public async Task<List<BoardDto>> GetBoardsAsync(string userId)
    {
        try
        {
            var boards = await _userApi.GetBoardsAsync(userId);

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

    public async Task<BoardDto?> GetByIdAsync(string id)
    {
        try
        {
            return await _boardApi.GetByIdAsync(id);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<string> CreateAsync(CreateBoardRequest request)
    {
        try
        {
            return await _boardApi.CreateAsync(request);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task UpdateAsync(string id, CreateBoardRequest request)
    {
        try
        {
            await _boardApi.UpdateAsync(id, request);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            await _boardApi.DeleteAsync(id);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BoardWithStats> GetWithStatsAsync(string boardId)
    {
        try
        {
            var board = await GetByIdAsync(boardId);
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
                IsOwner = currentUser != null && board.OwnerId == currentUser.Id.ToString()
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

    public async Task<List<BoardDto>> GetCachedBoardsAsync(string userId)
    {
        var cachedBoards = await _localStorage.GetItemAsync<List<BoardDto>>($"{BOARDS_CACHE_KEY}_{userId}");
        return cachedBoards ?? new List<BoardDto>();
    }

    public async Task<BoardWithStats> GetCachedWithStatsAsync(string boardId)
    {
        var cachedStats = await _localStorage.GetItemAsync<BoardWithStats>($"{BOARD_STATS_CACHE_PREFIX}{boardId}");
        return cachedStats ?? new BoardWithStats();
    }

    public async Task ClearCacheAsync(string userId)
    {
        await _localStorage.RemoveItemAsync($"{BOARDS_CACHE_KEY}_{userId}");
    }

    public async Task<BoardDetailDto?> GetDetailAsync(string boardId)
    {
        try
        {
            var board = await GetByIdAsync(boardId);
            if (board == null)
            {
                return null;
            }

            var members = await _boardMemberService.GetBoardMembersAsync(boardId);
            var tasks = await _taskService.GetBoardTasksAsync(boardId);
            var states = await _taskStateService.GetBoardStatesAsync(boardId);
            var tags = await _tagService.GetByBoardIdAsync(Guid.Parse(boardId));

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
                Tags = tags,
                States = states
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<IEnumerable<BoardSearchDto>> SearchBoardsRangeForOwnerAsync(
        Guid ownerId, string searchTerm, int page, int pageSize)
    {
        try
        {
            return await _boardApi.SearchBoardsRangeForOwnerAsync(ownerId, searchTerm, page, pageSize);
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<IEnumerable<BoardSearchDto>> SearchBoardsRangeForUserAsync(
        Guid userId, string searchTerm, int page, int pageSize)
    {
        try
        {
            return await _boardApi.SearchBoardsRangeForUserAsync(userId, searchTerm, page, pageSize);
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<IEnumerable<BoardSearchDto>> SearchBoardsRangeForMemberAsync(
        Guid userId, string searchTerm, int page, int pageSize)
    {
        try
        {
            return await _boardApi.SearchBoardsRangeForMemberAsync(userId, searchTerm, page, pageSize);
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task ArchiveBoardAsync(
        string boardId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _boardApi.ArchiveBoardAsync(boardId, cancellationToken);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task DearchiveBoardAsync(
        string boardId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _boardApi.DearchiveBoardAsync(boardId, cancellationToken);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IEnumerable<BoardDto>> GetArchivedBoardsByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _boardApi.GetArchivedBoardsByOwnerAsync(ownerId, cancellationToken);
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<IEnumerable<BoardSearchDto>> GetArchivedBoardsRangeForUserAsync(
        Guid userId,
        string searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var archivedBoards = await GetArchivedBoardsByOwnerAsync(userId, cancellationToken);

        var filteredBoards = string.IsNullOrWhiteSpace(searchTerm)
            ? archivedBoards
            : archivedBoards.Where(b =>
            (!string.IsNullOrEmpty(b.Name) && b.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(b.Description) && b.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));

        var pagedBoards = filteredBoards
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return pagedBoards.Select(b => new BoardSearchDto
        {
            Id = b.Id,
            Name = b.Name,
            Description = b.Description,
            OwnerId = b.OwnerId,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt,
            NumberOfMembers = 0,
            NumberOfTasks = 0
        });
    }

    public async Task<BoardDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _boardApi.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<BacklogDto>> SearchBacklogRangeForBoardAsync(
        Guid boardId,
        string searchTerm,
        int page,
        int pageSize,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _boardApi.SearchBacklogRangeForBoardAsync(
                boardId,
                searchTerm,
                page,
                pageSize,
                startDate,
                endDate,
                cancellationToken);
        }
        catch (Exception)
        {
            return [];
        }
    }
}
