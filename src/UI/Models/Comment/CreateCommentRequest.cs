namespace UI.Models.Comment;

public class CreateCommentRequest
{
    public string TaskId { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
