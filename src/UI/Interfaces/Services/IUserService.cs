using UI.Models.User;

namespace UI.Interfaces.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync(Guid organizationId);
    Task<UserDto?> GetByIdAsync(string userId);
    Task<UserDto?> GetByIdAsync(Guid userId);
    Task<UserDto?> GetByEmailAsync(string email);
    Task<Dictionary<string, UserDto>> GetByIdsAsync(IEnumerable<string> userIds);
    Task<List<UserDto>> SearchUsersAsync(string query);
    void ClearCache();
}
