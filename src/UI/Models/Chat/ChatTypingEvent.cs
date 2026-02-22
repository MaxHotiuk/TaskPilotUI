namespace UI.Models.Chat;

public class ChatTypingEvent
{
    public Guid ChatId { get; set; }
    public Guid UserId { get; set; }
}
