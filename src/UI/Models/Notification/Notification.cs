namespace UI.Models.Notification;

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public Guid? BoardId { get; set; }
    public Guid? TaskId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}