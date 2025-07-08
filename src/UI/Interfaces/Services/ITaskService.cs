using UI.Models.Task;

namespace UI.Interfaces.Services;

public interface ITaskService
{
    Task<List<TaskItemDto>> GetBoardTasksAsync(string boardId);
}
