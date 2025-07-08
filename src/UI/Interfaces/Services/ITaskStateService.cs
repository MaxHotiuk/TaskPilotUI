using UI.Models.State;

namespace UI.Interfaces.Services;

public interface ITaskStateService
{
    Task<List<StateDto>> GetBoardStatesAsync(string boardId);
    Task<int> CreateStateAsync(string boardId, CreateStateRequest request);
}
