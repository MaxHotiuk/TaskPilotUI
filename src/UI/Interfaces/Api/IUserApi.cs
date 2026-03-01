using Refit;
using UI.Models.Board;
using UI.Models.User;

namespace UI.Interfaces.Api;

public interface IUserApi
{
    [Get("/api/users/me")]
    Task<UserDto> GetCurrentAsync();

    [Get("/api/users/{userId}/boards")]
    Task<List<BoardDto>> GetBoardsAsync(string userId);

    [Get("/api/users/by-email")]
    Task<UserDto> GetByEmailAsync([Query] string email);

    [Get("/api/users")]
    Task<List<UserDto>> GetAllAsync([Query] Guid organizationId);

    [Get("/api/users/{id}")]
    Task<UserDto> GetByIdAsync(Guid id);

    [Get("/api/users/search")]
    Task<List<UserDto>> SearchAsync([Query] string query);
}
