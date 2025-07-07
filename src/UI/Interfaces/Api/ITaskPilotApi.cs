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
    Task<UserDto> GetCurrentUserAsync([Header("Authorization")] string authorization);

    [Get("/api/users/{userId}/boards")]
    Task<List<BoardDto>> GetUserBoardsAsync(string userId, [Header("Authorization")] string authorization);

    // Board endpoints
    [Get("/api/boards/{id}")]
    Task<BoardDto> GetBoardByIdAsync(string id, [Header("Authorization")] string authorization);

    [Post("/api/boards")]
    Task<string> CreateBoardAsync([Body] CreateBoardRequest request, [Header("Authorization")] string authorization);

    [Put("/api/boards/{id}")]
    Task UpdateBoardAsync(string id, [Body] CreateBoardRequest request, [Header("Authorization")] string authorization);

    [Delete("/api/boards/{id}")]
    Task DeleteBoardAsync(string id, [Header("Authorization")] string authorization);

    [Get("/api/boards/{boardId}/members")]
    Task<List<BoardMemberDto>> GetBoardMembersAsync(string boardId, [Header("Authorization")] string authorization);

    [Get("/api/boards/{boardId}/tasks")]
    Task<List<TaskItemDto>> GetBoardTasksAsync(string boardId, [Header("Authorization")] string authorization);

    // Board member endpoints
    [Post("/api/boards/{boardId}/members")]
    Task AddBoardMemberAsync(string boardId, [Body] AddBoardMemberRequest request, [Header("Authorization")] string authorization);

    [Put("/api/boards/{boardId}/members/{userId}/role")]
    Task UpdateBoardMemberRoleAsync(string boardId, string userId, [Body] UpdateBoardMemberRoleRequest request, [Header("Authorization")] string authorization);

    [Delete("/api/boards/{boardId}/members/{userId}")]
    Task RemoveBoardMemberAsync(string boardId, string userId, [Header("Authorization")] string authorization);

    // State endpoints
    [Get("/api/boards/{boardId}/states")]
    Task<List<StateDto>> GetBoardStatesAsync(string boardId, [Header("Authorization")] string authorization);

    [Post("/api/boards/{boardId}/states")]
    Task<int> CreateStateAsync(string boardId, [Body] CreateStateRequest request, [Header("Authorization")] string authorization);

    // User search endpoints
    [Get("/api/users/by-email")]
    Task<UserDto> GetUserByEmailAsync([Query] string email, [Header("Authorization")] string authorization);

    [Get("/api/users")]
    Task<List<UserDto>> GetAllUsersAsync([Header("Authorization")] string authorization);
}
