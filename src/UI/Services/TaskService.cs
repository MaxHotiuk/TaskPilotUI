using UI.Interfaces.Api;
using UI.Interfaces.Services;
using UI.Models.Task;

namespace UI.Services;

public class TaskService : ITaskService
{
    private readonly IBoardTaskApi _boardTaskApi;

    public TaskService(IBoardTaskApi boardTaskApi)
    {
        _boardTaskApi = boardTaskApi;
    }

    public async Task<List<TaskItemDto>> GetBoardTasksAsync(string boardId)
    {
        try
        {
            return await _boardTaskApi.GetBoardTasksAsync(boardId);
        }
        catch (Exception)
        {
            return new List<TaskItemDto>();
        }
    }
}
