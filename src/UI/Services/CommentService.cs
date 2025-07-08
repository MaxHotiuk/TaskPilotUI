using UI.Interfaces.Api;
using UI.Interfaces.Services;
using UI.Models.Comment;

namespace UI.Services;

public class CommentService : ICommentService
{
    private readonly ICommentApi _commentApi;

    public CommentService(ICommentApi commentApi)
    {
        _commentApi = commentApi;
    }

    public async Task<List<CommentDto>> GetTaskCommentsAsync(string taskId)
    {
        try
        {
            return await _commentApi.GetTaskCommentsAsync(taskId);
        }
        catch (Exception)
        {
            return new List<CommentDto>();
        }
    }

    public async Task<CommentDto> GetCommentByIdAsync(string commentId)
    {
        try
        {
            return await _commentApi.GetCommentByIdAsync(commentId);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get comment: {ex.Message}", ex);
        }
    }

    public async Task<string> CreateCommentAsync(CreateCommentRequest request)
    {
        try
        {
            return await _commentApi.CreateCommentAsync(request);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create comment: {ex.Message}", ex);
        }
    }

    public async Task UpdateCommentAsync(string commentId, UpdateCommentRequest request)
    {
        try
        {
            await _commentApi.UpdateCommentAsync(commentId, request);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to update comment: {ex.Message}", ex);
        }
    }

    public async Task DeleteCommentAsync(string commentId)
    {
        try
        {
            await _commentApi.DeleteCommentAsync(commentId);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to delete comment: {ex.Message}", ex);
        }
    }
}
