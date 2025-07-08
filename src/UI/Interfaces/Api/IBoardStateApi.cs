using Refit;
using UI.Models.State;

namespace UI.Interfaces.Api;

public interface IBoardStateApi
{
    [Get("/api/boards/{boardId}/states")]
    Task<List<StateDto>> GetBoardStatesAsync(string boardId);

    [Post("/api/boards/{boardId}/states")]
    Task<int> CreateAsync(string boardId, [Body] CreateStateRequest request);
}
