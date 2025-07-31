using UI.Models.Backlog;
using UI.Models.Board;

namespace UI.Interfaces.Services;

public interface IBoardService
{
    Task<List<BoardDto>> GetBoardsAsync(string userId);
    Task<List<BoardDto>> GetCachedBoardsAsync(string userId);
    Task<BoardDto?> GetByIdAsync(string id);
    Task<string> CreateAsync(CreateBoardRequest request);
    Task UpdateAsync(string id, CreateBoardRequest request);
    Task DeleteAsync(string id);
    Task<BoardDetailDto?> GetDetailAsync(string boardId);
    Task<BoardWithStats> GetWithStatsAsync(string boardId);
    Task<BoardWithStats> GetCachedWithStatsAsync(string boardId);
    Task ClearCacheAsync(string userId);
    Task<IEnumerable<BoardSearchDto>> SearchBoardsRangeForOwnerAsync(
        Guid ownerId, string searchTerm, int page, int pageSize);
    Task<IEnumerable<BoardSearchDto>> SearchBoardsRangeForUserAsync(
        Guid userId, string searchTerm, int page, int pageSize);

    Task<IEnumerable<BoardSearchDto>> SearchBoardsRangeForMemberAsync(
        Guid userId, string searchTerm, int page, int pageSize);

    Task ArchiveBoardAsync(
        string boardId,
        CancellationToken cancellationToken = default);

    Task DearchiveBoardAsync(
        string boardId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<BoardDto>> GetArchivedBoardsByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<BoardSearchDto>> GetArchivedBoardsRangeForUserAsync(
        Guid userId,
        string searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<BoardDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<BacklogDto>> SearchBacklogRangeForBoardAsync(
        Guid boardId,
        string searchTerm,
        int page,
        int pageSize,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
