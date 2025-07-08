using UI.Interfaces.Api;
using UI.Interfaces.Services;
using UI.Models.State;

namespace UI.Services;

public class TaskStateService : ITaskStateService
{
    private readonly IBoardStateApi _boardStateApi;

    public TaskStateService(IBoardStateApi boardStateApi)
    {
        _boardStateApi = boardStateApi;
    }

    public async Task<List<StateDto>> GetBoardStatesAsync(string boardId)
    {
        try
        {
            return await _boardStateApi.GetBoardStatesAsync(boardId);
        }
        catch (Exception)
        {
            return new List<StateDto>();
        }
    }

    public async Task<int> CreateAsync(string boardId, CreateStateRequest request)
    {
        try
        {
            return await _boardStateApi.CreateAsync(boardId, request);
        }
        catch (Exception)
        {
            throw;
        }
    }
}
