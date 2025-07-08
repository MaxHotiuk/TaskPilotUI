using Refit;
using UI.Models.Comment;

namespace UI.Interfaces.Api;

public interface ICommentApi
{
    [Post("/api/comments")]
    Task<string> CreateAsync([Body] CreateCommentRequest request);

    [Get("/api/comments/{id}")]
    Task<CommentDto> GetByIdAsync(string id);

    [Get("/api/tasks/{taskId}/comments")]
    Task<List<CommentDto>> GetTaskCommentsAsync(string taskId);

    [Put("/api/comments/{id}")]
    Task UpdateAsync(string id, [Body] UpdateCommentRequest request);

    [Delete("/api/comments/{id}")]
    Task DeleteAsync(string id);
}
