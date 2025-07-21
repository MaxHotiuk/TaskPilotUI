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

    public async Task UpdateAsync(int id, UpdateStateRequest request)
    {
        await _boardStateApi.UpdateAsync(id, request);
    }

    public async Task DeleteAsync(int id)
    {
        await _boardStateApi.DeleteAsync(id);
    }

    public async Task SwapOrderAsync(string boardId, SwapStateOrderRequest request)
    {
        await _boardStateApi.SwapOrderAsync(boardId, request);
    }
}
