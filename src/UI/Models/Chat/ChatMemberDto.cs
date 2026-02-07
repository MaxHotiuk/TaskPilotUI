namespace UI.Models.Chat;

public class ChatMemberDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ChatMemberRole Role { get; set; }
    public DateTime? LastReadAt { get; set; }
}
