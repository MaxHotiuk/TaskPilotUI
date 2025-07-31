using Refit;
using UI.Models.Tag;

namespace UI.Interfaces.Api;

public interface ITagApi
{
    [Post("/api/boards/{boardId}/tags")]
    Task CreateAsync(
        Guid boardId,
        [Body] CreateTagRequestDto dto,
        CancellationToken cancellationToken = default);

    [Delete("/api/boards/{boardId}/tags/{id}")]
    Task DeleteAsync(
        Guid boardId,
        int id,
        CancellationToken cancellationToken = default);

    [Get("/api/boards/{boardId}/tags")]
    Task<IEnumerable<TagDto>> GetByBoardIdAsync(
        Guid boardId,
        CancellationToken cancellationToken = default);
    
    [Put("/api/boards/{boardId}/tags/{id}")]
    Task UpdateAsync(
        Guid boardId,
        int id,
        [Body] UpdateTagRequestDto dto,
        CancellationToken cancellationToken = default);
}
