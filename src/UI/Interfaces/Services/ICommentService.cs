using UI.Models.Comment;

namespace UI.Interfaces.Services;

public interface ICommentService
{
    Task<List<CommentDto>> GetTaskCommentsAsync(string taskId);
    Task<CommentDto> GetCommentByIdAsync(string commentId);
    Task<string> CreateCommentAsync(CreateCommentRequest request);
    Task UpdateCommentAsync(string commentId, UpdateCommentRequest request);
    Task DeleteCommentAsync(string commentId);
}
