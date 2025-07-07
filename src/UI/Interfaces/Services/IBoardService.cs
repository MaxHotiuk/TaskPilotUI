using UI.Models.Board;
using UI.Models.Member;
using UI.Models.Task;

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
    Task<BoardWithStats> GetBoardWithStatsAsync(string boardId);
    Task<BoardWithStats> GetCachedBoardWithStatsAsync(string boardId);
    Task ClearBoardCacheAsync(string userId);
}
