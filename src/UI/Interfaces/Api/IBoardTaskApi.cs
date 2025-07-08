using Refit;
using UI.Models.Task;

namespace UI.Interfaces.Api;

public interface IBoardTaskApi
{
    [Get("/api/boards/{boardId}/tasks")]
    Task<List<TaskItemDto>> GetBoardTasksAsync(string boardId);
    
    [Get("/api/tasks/{taskId}")]
    Task<TaskItemDto> GetTaskByIdAsync(string taskId);
    
    [Post("/api/tasks")]
    Task<string> CreateTaskAsync([Body] CreateTaskRequest request);
    
    [Put("/api/tasks/{taskId}")]
    Task UpdateTaskAsync(string taskId, [Body] UpdateTaskRequest request);
    
    [Delete("/api/tasks/{taskId}")]
    Task DeleteTaskAsync(string taskId);
}
