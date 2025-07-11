using Refit;
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
}
