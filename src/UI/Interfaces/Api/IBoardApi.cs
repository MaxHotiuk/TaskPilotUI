using Refit;
using UI.Models.Backlog;
using UI.Models.Board;

namespace UI.Interfaces.Api;

public interface IBoardApi
{
    [Get("/api/boards/{id}")]
    Task<BoardDto> GetByIdAsync(string id);

    [Post("/api/boards")]
    Task<string> CreateAsync([Body] CreateBoardRequest request);

    [Put("/api/boards/{id}")]
    Task UpdateAsync(string id, [Body] CreateBoardRequest request);

    [Delete("/api/boards/{id}")]
    Task DeleteAsync(string id);

    [Get("/api/boards/owner/search")]
    Task<IEnumerable<BoardSearchDto>> SearchBoardsRangeForOwnerAsync(
        [Query] Guid ownerId,
        [Query] string searchTerm,
        [Query] int page,
        [Query] int pageSize);

    [Get("/api/boards/user/search")]
    Task<IEnumerable<BoardSearchDto>> SearchBoardsRangeForUserAsync(
        [Query] Guid userId,
        [Query] string searchTerm,
        [Query] int page,
        [Query] int pageSize);

    [Get("/api/boards/member/search")]
    Task<IEnumerable<BoardSearchDto>> SearchBoardsRangeForMemberAsync(
        [Query] Guid userId,
        [Query] string searchTerm,
        [Query] int page,
        [Query] int pageSize);

    [Post("/api/boards/{boardId}/archive")]
    Task ArchiveBoardAsync(
        string boardId,
        CancellationToken cancellationToken = default);

    [Post("/api/boards/{boardId}/dearchive")]
    Task DearchiveBoardAsync(
        string boardId,
        CancellationToken cancellationToken = default);

    [Get("/api/users/{ownerId}/boards/archived")]
    Task<IEnumerable<BoardDto>> GetArchivedBoardsByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    [Get("/api/boards/{id}")]
    Task<BoardDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
    
    [Get("/api/boards/{boardId}/backlog/search")]
    Task<IEnumerable<BacklogDto>> SearchBacklogRangeForBoardAsync(
        [Query] Guid boardId,
        [Query] string searchTerm,
        [Query] int page,
        [Query] int pageSize,
        [Query] DateOnly startDate,
        [Query] DateOnly endDate,
        CancellationToken cancellationToken = default);
}
