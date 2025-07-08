using Refit;
using UI.Models.Board;
using UI.Models.User;

namespace UI.Interfaces.Api;

public interface IUserApi
{
    [Get("/api/users/me")]
    Task<UserDto> GetCurrentUserAsync();

    [Get("/api/users/{userId}/boards")]
    Task<List<BoardDto>> GetUserBoardsAsync(string userId);

    [Get("/api/users/by-email")]
    Task<UserDto> GetUserByEmailAsync([Query] string email);

    [Get("/api/users")]
    Task<List<UserDto>> GetAllUsersAsync();
}
