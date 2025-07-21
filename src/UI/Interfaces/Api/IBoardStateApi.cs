using Refit;
using UI.Models.State;

namespace UI.Interfaces.Api;

public interface IBoardStateApi
{
    [Get("/api/boards/{boardId}/states")]
    Task<List<StateDto>> GetBoardStatesAsync(string boardId);

    [Post("/api/boards/{boardId}/states")]
    Task<int> CreateAsync(string boardId, [Body] CreateStateRequest request);

    [Put("/api/states/{id}")]
    Task UpdateAsync(int id, [Body] UpdateStateRequest request);

    [Delete("/api/states/{id}")]
    Task DeleteAsync(int id);

    [Post("/api/boards/{boardId}/states/swap-order")]
    Task SwapOrderAsync(string boardId, [Body] SwapStateOrderRequest request);
}
