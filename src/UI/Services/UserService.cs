using Microsoft.Extensions.Logging;
using UI.Interfaces.Api;
using UI.Interfaces.Services;
using UI.Models.User;

namespace UI.Services;

public class UserService : IUserService
{
    private readonly ITaskPilotApi _taskPilotApi;
    private readonly IAuthService _authService;
    private readonly ILogger<UserService> _logger;
    private readonly Dictionary<string, UserDto> _userCache = new();
    private DateTime _lastCacheUpdate = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public UserService(
        ITaskPilotApi taskPilotApi,
        IAuthService authService,
        ILogger<UserService> logger)
    {
        _taskPilotApi = taskPilotApi;
        _authService = authService;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        try
        {
            var token = await _authService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No access token available for getting users");
                return new List<UserDto>();
            }

            var users = await _taskPilotApi.GetAllUsersAsync($"Bearer {token}");
            
            // Update cache
            foreach (var user in users)
            {
                _userCache[user.Id] = user;
            }
            _lastCacheUpdate = DateTime.UtcNow;

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all users");
            return new List<UserDto>();
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return null;

        // Check cache first
        if (_userCache.TryGetValue(userId, out var cachedUser) && 
            DateTime.UtcNow - _lastCacheUpdate < _cacheExpiry)
        {
            return cachedUser;
        }

        try
        {
            // If not in cache or cache expired, fetch all users to populate cache
            var allUsers = await GetAllUsersAsync();
            return allUsers.FirstOrDefault(u => u.Id == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user by ID: {UserId}", userId);
            return null;
        }
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        if (string.IsNullOrEmpty(email))
            return null;

        try
        {
            var token = await _authService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No access token available for getting user by email");
                return null;
            }

            var user = await _taskPilotApi.GetUserByEmailAsync(email, $"Bearer {token}");
            
            // Update cache
            if (user != null)
            {
                _userCache[user.Id] = user;
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user by email: {Email}", email);
            return null;
        }
    }

    public async Task<Dictionary<string, UserDto>> GetUsersByIdsAsync(IEnumerable<string> userIds)
    {
        var result = new Dictionary<string, UserDto>();
        var missingUserIds = new List<string>();

        // Check cache first
        foreach (var userId in userIds)
        {
            if (!string.IsNullOrEmpty(userId) && _userCache.TryGetValue(userId, out var cachedUser) &&
                DateTime.UtcNow - _lastCacheUpdate < _cacheExpiry)
            {
                result[userId] = cachedUser;
            }
            else
            {
                missingUserIds.Add(userId);
            }
        }

        // If we have missing users and cache is expired or incomplete, refresh cache
        if (missingUserIds.Any() || DateTime.UtcNow - _lastCacheUpdate >= _cacheExpiry)
        {
            var allUsers = await GetAllUsersAsync();
            foreach (var userId in missingUserIds)
            {
                var user = allUsers.FirstOrDefault(u => u.Id == userId);
                if (user != null)
                {
                    result[userId] = user;
                }
            }
        }

        return result;
    }

    public void ClearCache()
    {
        _userCache.Clear();
        _lastCacheUpdate = DateTime.MinValue;
    }
}
