using UI.Models.User;

namespace UI.Interfaces.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<Dictionary<string, UserDto>> GetUsersByIdsAsync(IEnumerable<string> userIds);
    void ClearCache();
}
