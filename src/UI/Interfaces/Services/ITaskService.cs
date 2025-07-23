using UI.Models.Task;

namespace UI.Interfaces.Services;

public interface ITaskService
{
    Task<List<TaskItemDto>> GetBoardTasksAsync(string boardId);
    Task<TaskItemDto> GetByIdAsync(string taskId);
    Task<string> CreateAsync(CreateTaskRequest request);
    Task UpdateAsync(string taskId, UpdateTaskRequest request);
    Task DeleteAsync(string taskId);
    Task<List<TaskCalendarItemDto>> GetForCalendarMonthAsync(Guid userId, DateTime dayInMonth);
    Task ArchiveAsync(Guid taskId);
    Task RestoreAsync(Guid taskId);
    Task<List<ArchivedTaskDto>> SearchArchivedRangeTaskItemsAsync(
        int page,
        int pageSize,
        string searchTerm,
        Guid boardId);
}
