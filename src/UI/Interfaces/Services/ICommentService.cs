using UI.Models.Comment;

namespace UI.Interfaces.Services;

public interface ICommentService
{
    Task<List<CommentDto>> GetTaskCommentsAsync(string taskId);
    Task<CommentDto> GetByIdAsync(string commentId);
    Task<string> CreateAsync(CreateCommentRequest request);
    Task UpdateAsync(string commentId, UpdateCommentRequest request);
    Task DeleteAsync(string commentId);
    Task<List<CommentDto>> SearchCommentsAsync(
        string searchTerm, Guid taskId, int page, int pageSize);
}
