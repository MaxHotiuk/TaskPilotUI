namespace UI.Models.Chat;

public class UpdateChatNameRequestDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
}
