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
}
