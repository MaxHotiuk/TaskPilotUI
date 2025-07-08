using Refit;
using UI.Models.Task;

namespace UI.Interfaces.Api;

public interface IBoardTaskApi
{
    [Get("/api/boards/{boardId}/tasks")]
    Task<List<TaskItemDto>> GetBoardTasksAsync(string boardId);
}
