using Refit;
using UI.Models.Task;

namespace UI.Interfaces.Api;

public interface IBoardTaskApi
{
    [Get("/api/boards/{boardId}/tasks")]
    Task<List<TaskItemDto>> GetBoardTasksAsync(string boardId);

    [Get("/api/tasks/{taskId}")]
    Task<TaskItemDto> GetByIdAsync(string taskId);

    [Post("/api/tasks")]
    Task<string> CreateAsync([Body] CreateTaskRequest request);

    [Put("/api/tasks/{taskId}")]
    Task UpdateAsync(string taskId, [Body] UpdateTaskRequest request);

    [Delete("/api/tasks/{taskId}")]
    Task DeleteAsync(string taskId);

    [Get("/api/tasks/calendar")]
    Task<List<TaskCalendarItemDto>> GetForCalendarMonthAsync(
        Guid userId,
        DateTime dayInMonth);

    [Post("/api/tasks/{taskId}/archive")]
    Task ArchiveAsync(Guid taskId);

    [Post("/api/tasks/{taskId}/restore")]
    Task RestoreAsync(Guid taskId);
    
    [Get("/api/tasks/archived")]
    Task<List<ArchivedTaskDto>> SearchArchivedRangeTaskItemsAsync(
        int page,
        int pageSize,
        string searchTerm,
        Guid boardId);
}
