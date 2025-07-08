using UI.Models.State;

namespace UI.Interfaces.Services;

public interface ITaskStateService
{
    Task<List<StateDto>> GetBoardStatesAsync(string boardId);
    Task<int> CreateAsync(string boardId, CreateStateRequest request);
}
