using Refit;
using UI.Models.Comment;

namespace UI.Interfaces.Api;

public interface ICommentApi
{
    [Post("/api/comments")]
    Task<string> CreateCommentAsync([Body] CreateCommentRequest request);

    [Get("/api/comments/{id}")]
    Task<CommentDto> GetCommentByIdAsync(string id);

    [Get("/api/tasks/{taskId}/comments")]
    Task<List<CommentDto>> GetTaskCommentsAsync(string taskId);

    [Put("/api/comments/{id}")]
    Task UpdateCommentAsync(string id, [Body] UpdateCommentRequest request);

    [Delete("/api/comments/{id}")]
    Task DeleteCommentAsync(string id);
}
