namespace UI.Models.Chat;

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid SenderId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? AssigneeId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string MessageType { get; set; } = "Text";
    public string Content { get; set; } = string.Empty;
    public bool HasAttachments { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
