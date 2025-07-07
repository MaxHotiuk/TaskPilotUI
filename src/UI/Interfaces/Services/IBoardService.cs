using UI.Models.Board;
using UI.Models.Member;
using UI.Models.Task;
using UI.Models.State;
using UI.Models.User;

namespace UI.Interfaces.Services;

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
    Task<List<StateDto>> GetBoardStatesAsync(string boardId);
    Task<int> CreateStateAsync(string boardId, CreateStateRequest request);
    Task<BoardDetailDto?> GetBoardDetailAsync(string boardId);
    Task<BoardWithStats> GetBoardWithStatsAsync(string boardId);
    Task<BoardWithStats> GetCachedBoardWithStatsAsync(string boardId);
    Task ClearBoardCacheAsync(string userId);
    
    // Member management
    Task AddBoardMemberAsync(string boardId, AddBoardMemberRequest request);
    Task UpdateBoardMemberRoleAsync(string boardId, string userId, UpdateBoardMemberRoleRequest request);
    Task RemoveBoardMemberAsync(string boardId, string userId);
    
    // User search
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<List<UserDto>> GetAllUsersAsync();
}
