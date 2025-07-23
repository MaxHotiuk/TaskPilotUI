using UI.Interfaces.Api;
using UI.Interfaces.Services;
using UI.Models.Tag;

namespace UI.Services;

public class TagService : ITagService
{
    private readonly ITagApi _tagApi;

    public TagService(ITagApi tagApi)
    {
        _tagApi = tagApi;
    }

    public async Task<IEnumerable<TagDto>> GetByBoardIdAsync(Guid boardId)
    {
        return await _tagApi.GetByBoardIdAsync(boardId);
    }

    public async Task CreateAsync(Guid boardId, CreateTagRequestDto request)
    {
        await _tagApi.CreateAsync(boardId, request);
    }

    public async Task UpdateAsync(Guid boardId, int id, UpdateTagRequestDto request, CancellationToken cancellationToken = default)
    {
        await _tagApi.UpdateAsync(boardId, id, request, cancellationToken);
    }

    public async Task DeleteAsync(Guid boardId, int id, CancellationToken cancellationToken = default)
    {
        await _tagApi.DeleteAsync(boardId, id, cancellationToken);
    }
}