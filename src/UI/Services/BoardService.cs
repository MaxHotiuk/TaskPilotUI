using System.Net.Http.Json;
using UI.Models;

namespace UI.Services;

public interface IBoardService
{
    Task<List<BoardDto>> GetUserBoardsAsync(string userId);
    Task<List<BoardDto>> GetCachedUserBoardsAsync(string userId);
    Task<BoardDto?> GetBoardByIdAsync(string id);
    Task<string> CreateBoardAsync(CreateBoardRequest request);
    Task UpdateBoardAsync(string id, CreateBoardRequest request);
    Task DeleteBoardAsync(string id);
    Task<List<BoardMemberDto>> GetBoardMembersAsync(string boardId);
    Task<List<TaskItemDto>> GetBoardTasksAsync(string boardId);
    Task<BoardWithStats> GetBoardWithStatsAsync(string boardId);
    Task<BoardWithStats> GetCachedBoardWithStatsAsync(string boardId);
    Task ClearBoardCacheAsync(string userId);
}

public class BoardService : IBoardService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private readonly ILocalStorageService _localStorage;
    private const string BOARDS_CACHE_KEY = "cached_boards";
    private const string BOARD_STATS_CACHE_PREFIX = "cached_board_stats_";

    public BoardService(HttpClient httpClient, IAuthService authService, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _authService = authService;
        _localStorage = localStorage;
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        var token = await _authService.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        return _httpClient;
    }

    public async Task<List<BoardDto>> GetUserBoardsAsync(string userId)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var response = await client.GetAsync($"/api/users/{userId}/boards");
            
            if (response.IsSuccessStatusCode)
            {
                var boards = await response.Content.ReadFromJsonAsync<List<BoardDto>>() ?? new List<BoardDto>();
                
                // Cache the boards
                await _localStorage.SetItemAsync($"{BOARDS_CACHE_KEY}_{userId}", boards);
                
                return boards;
            }
            
            // If API call fails, try to return cached data
            var cachedBoards = await _localStorage.GetItemAsync<List<BoardDto>>($"{BOARDS_CACHE_KEY}_{userId}");
            return cachedBoards ?? new List<BoardDto>();
        }
        catch (Exception)
        {
            // If everything fails, try to return cached data
            var cachedBoards = await _localStorage.GetItemAsync<List<BoardDto>>($"{BOARDS_CACHE_KEY}_{userId}");
            return cachedBoards ?? new List<BoardDto>();
        }
    }

    public async Task<BoardDto?> GetBoardByIdAsync(string id)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var response = await client.GetAsync($"/api/boards/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<BoardDto>();
            }
            
            return null;
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
            var client = await GetAuthenticatedClientAsync();
            var response = await client.PostAsJsonAsync("/api/boards", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync() ?? string.Empty;
            }
            
            throw new Exception($"Failed to create board: {response.StatusCode}");
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
            var client = await GetAuthenticatedClientAsync();
            var response = await client.PutAsJsonAsync($"/api/boards/{id}", request);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to update board: {response.StatusCode}");
            }
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
            var client = await GetAuthenticatedClientAsync();
            var response = await client.DeleteAsync($"/api/boards/{id}");
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to delete board: {response.StatusCode}");
            }
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
            var client = await GetAuthenticatedClientAsync();
            var response = await client.GetAsync($"/api/boards/{boardId}/members");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<BoardMemberDto>>() ?? new List<BoardMemberDto>();
            }
            
            return new List<BoardMemberDto>();
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
            var client = await GetAuthenticatedClientAsync();
            var response = await client.GetAsync($"/api/boards/{boardId}/tasks");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<TaskItemDto>>() ?? new List<TaskItemDto>();
            }
            
            return new List<TaskItemDto>();
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

            // Cache the board stats
            await _localStorage.SetItemAsync($"{BOARD_STATS_CACHE_PREFIX}{boardId}", boardWithStats);
            
            return boardWithStats;
        }
        catch (Exception)
        {
            // If API call fails, try to return cached data
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
        
        // Also clear all board stats cache - this is a simple approach
        // In a real app, you might want to track which boards belong to which user
        // For now, we'll clear the user's boards cache
    }
}
