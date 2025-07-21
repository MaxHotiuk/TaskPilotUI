using UI.Models.User;

namespace UI.Interfaces.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetByIdAsync(string userId);
    Task<UserDto?> GetByEmailAsync(string email);
    Task<Dictionary<string, UserDto>> GetByIdsAsync(IEnumerable<string> userIds);
    void ClearCache();
}
