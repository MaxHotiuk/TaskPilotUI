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

    public async Task<CommentDto> GetByIdAsync(string commentId)
    {
        try
        {
            return await _commentApi.GetByIdAsync(commentId);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get comment: {ex.Message}", ex);
        }
    }

    public async Task<string> CreateAsync(CreateCommentRequest request)
    {
        try
        {
            return await _commentApi.CreateAsync(request);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create comment: {ex.Message}", ex);
        }
    }

    public async Task UpdateAsync(string commentId, UpdateCommentRequest request)
    {
        try
        {
            await _commentApi.UpdateAsync(commentId, request);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to update comment: {ex.Message}", ex);
        }
    }

    public async Task DeleteAsync(string commentId)
    {
        try
        {
            await _commentApi.DeleteAsync(commentId);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to delete comment: {ex.Message}", ex);
        }
    }

    public async Task<List<CommentDto>> SearchCommentsAsync(
        string searchTerm, Guid taskId, int page, int pageSize)
    {
        try
        {
            return await _commentApi.SearchCommentsAsync(searchTerm, taskId, page, pageSize);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to search comments: {ex.Message}", ex);
        }
    }
}
