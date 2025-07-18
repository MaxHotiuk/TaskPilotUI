using Microsoft.Extensions.Logging;
using UI.Interfaces.Api;
using UI.Interfaces.Services;
using UI.Models.User;

namespace UI.Services;

public class UserService : IUserService
{
    private readonly IUserApi _userApi;
    private readonly IAuthService _authService;
    private readonly ILogger<UserService> _logger;
    private readonly Dictionary<string, UserDto> _userCache = new();
    private DateTime _lastCacheUpdate = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public UserService(
        IUserApi userApi,
        IAuthService authService,
        ILogger<UserService> logger)
    {
        _userApi = userApi;
        _authService = authService;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        try
        {
            var users = await _userApi.GetAllAsync();
            
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

    public async Task<UserDto?> GetByIdAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return null;

        if (_userCache.TryGetValue(userId, out var cachedUser) &&
            DateTime.UtcNow - _lastCacheUpdate < _cacheExpiry)
        {
            return cachedUser;
        }

        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Invalid userId format: {UserId}", userId);
                return null;
            }
            var user = await _userApi.GetByIdAsync(userGuid);
            if (user != null)
            {
                _userCache[userId] = user;
            }
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user by ID: {UserId}", userId);
            return null;
        }
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrEmpty(email))
            return null;

        try
        {
            var user = await _userApi.GetByEmailAsync(email);
            
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

    public async Task<Dictionary<string, UserDto>> GetByIdsAsync(IEnumerable<string> userIds)
    {
        var result = new Dictionary<string, UserDto>();
        var missingUserIds = new List<string>();

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
