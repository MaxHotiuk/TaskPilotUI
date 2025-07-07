using Refit;
using UI.Models.Board;
using UI.Models.Member;
using UI.Models.Task;
using UI.Models.User;
using UI.Models.State;

namespace UI.Interfaces.Api;

public interface ITaskPilotApi
{
    // User endpoints
    [Get("/api/users/me")]
    Task<UserDto> GetCurrentUserAsync();

    [Get("/api/users/{userId}/boards")]
    Task<List<BoardDto>> GetUserBoardsAsync(string userId);

    // Board endpoints
    [Get("/api/boards/{id}")]
    Task<BoardDto> GetBoardByIdAsync(string id);

    [Post("/api/boards")]
    Task<string> CreateBoardAsync([Body] CreateBoardRequest request);

    [Put("/api/boards/{id}")]
    Task UpdateBoardAsync(string id, [Body] CreateBoardRequest request);

    [Delete("/api/boards/{id}")]
    Task DeleteBoardAsync(string id);

    [Get("/api/boards/{boardId}/members")]
    Task<List<BoardMemberDto>> GetBoardMembersAsync(string boardId);

    [Get("/api/boards/{boardId}/tasks")]
    Task<List<TaskItemDto>> GetBoardTasksAsync(string boardId);

    // Board member endpoints
    [Post("/api/boards/{boardId}/members")]
    Task AddBoardMemberAsync(string boardId, [Body] AddBoardMemberRequest request);

    [Put("/api/boards/{boardId}/members/{userId}/role")]
    Task UpdateBoardMemberRoleAsync(string boardId, string userId, [Body] UpdateBoardMemberRoleRequest request);

    [Delete("/api/boards/{boardId}/members/{userId}")]
    Task RemoveBoardMemberAsync(string boardId, string userId);

    // State endpoints
    [Get("/api/boards/{boardId}/states")]
    Task<List<StateDto>> GetBoardStatesAsync(string boardId);

    [Post("/api/boards/{boardId}/states")]
    Task<int> CreateStateAsync(string boardId, [Body] CreateStateRequest request);

    // User search endpoints
    [Get("/api/users/by-email")]
    Task<UserDto> GetUserByEmailAsync([Query] string email);

    [Get("/api/users")]
    Task<List<UserDto>> GetAllUsersAsync();
}
