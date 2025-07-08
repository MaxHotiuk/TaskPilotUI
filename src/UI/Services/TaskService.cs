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

    public async Task<TaskItemDto> GetTaskByIdAsync(string taskId)
    {
        try
        {
            return await _boardTaskApi.GetTaskByIdAsync(taskId);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get task: {ex.Message}", ex);
        }
    }

    public async Task<string> CreateTaskAsync(CreateTaskRequest request)
    {
        try
        {
            return await _boardTaskApi.CreateTaskAsync(request);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create task: {ex.Message}", ex);
        }
    }

    public async Task UpdateTaskAsync(string taskId, UpdateTaskRequest request)
    {
        try
        {
            await _boardTaskApi.UpdateTaskAsync(taskId, request);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to update task: {ex.Message}", ex);
        }
    }

    public async Task DeleteTaskAsync(string taskId)
    {
        try
        {
            await _boardTaskApi.DeleteTaskAsync(taskId);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to delete task: {ex.Message}", ex);
        }
    }
}
