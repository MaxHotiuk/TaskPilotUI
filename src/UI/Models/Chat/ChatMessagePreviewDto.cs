namespace UI.Models.Chat;

public class ChatMessagePreviewDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string MessageType { get; set; } = "Text";
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
