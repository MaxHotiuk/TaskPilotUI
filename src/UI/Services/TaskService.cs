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

    public async Task<TaskItemDto> GetByIdAsync(string taskId)
    {
        try
        {
            return await _boardTaskApi.GetByIdAsync(taskId);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get task: {ex.Message}", ex);
        }
    }

    public async Task<string> CreateAsync(CreateTaskRequest request)
    {
        try
        {
            return await _boardTaskApi.CreateAsync(request);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create task: {ex.Message}", ex);
        }
    }

    public async Task UpdateAsync(string taskId, UpdateTaskRequest request)
    {
        try
        {
            await _boardTaskApi.UpdateAsync(taskId, request);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to update task: {ex.Message}", ex);
        }
    }

    public async Task DeleteAsync(string taskId)
    {
        try
        {
            await _boardTaskApi.DeleteAsync(taskId);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to delete task: {ex.Message}", ex);
        }
    }

    public async Task<List<TaskCalendarItemDto>> GetForCalendarMonthAsync(Guid userId, DateTime dayInMonth)
    {
        try
        {
            return await _boardTaskApi.GetForCalendarMonthAsync(userId, dayInMonth);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get tasks for calendar month: {ex.Message}", ex);
        }
    }
}
