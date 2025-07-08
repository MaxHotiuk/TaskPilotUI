using UI.Models.Board;

namespace UI.Interfaces.Services;

public interface IBoardService
{
    Task<List<BoardDto>> GetUserBoardsAsync(string userId);
    Task<List<BoardDto>> GetCachedUserBoardsAsync(string userId);
    Task<BoardDto?> GetBoardByIdAsync(string id);
    Task<string> CreateBoardAsync(CreateBoardRequest request);
    Task UpdateBoardAsync(string id, CreateBoardRequest request);
    Task DeleteBoardAsync(string id);
    Task<BoardDetailDto?> GetBoardDetailAsync(string boardId);
    Task<BoardWithStats> GetBoardWithStatsAsync(string boardId);
    Task<BoardWithStats> GetCachedBoardWithStatsAsync(string boardId);
    Task ClearBoardCacheAsync(string userId);
}
