using UI.Models.State;

namespace UI.Interfaces.Services;

public interface ITaskStateService
{
    Task<List<StateDto>> GetBoardStatesAsync(string boardId);
    Task<int> CreateAsync(string boardId, CreateStateRequest request);
    Task UpdateAsync(int id, UpdateStateRequest request);
    Task DeleteAsync(int id);
    Task SwapOrderAsync(string boardId, SwapStateOrderRequest request);
}
