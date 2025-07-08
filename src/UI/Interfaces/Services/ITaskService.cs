using UI.Models.Task;

namespace UI.Interfaces.Services;

public interface ITaskService
{
    Task<List<TaskItemDto>> GetBoardTasksAsync(string boardId);
    Task<TaskItemDto> GetTaskByIdAsync(string taskId);
    Task<string> CreateTaskAsync(CreateTaskRequest request);
    Task UpdateTaskAsync(string taskId, UpdateTaskRequest request);
    Task DeleteTaskAsync(string taskId);
}
