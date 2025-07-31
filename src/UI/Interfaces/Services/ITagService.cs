using UI.Models.Tag;

namespace UI.Interfaces.Services;

public interface ITagService
{
    Task<IEnumerable<TagDto>> GetByBoardIdAsync(Guid boardId);
    Task CreateAsync(Guid boardId, CreateTagRequestDto request);
    Task UpdateAsync(Guid boardId, int id, UpdateTagRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid boardId, int id, CancellationToken cancellationToken = default);
}