namespace UI.Models.Chat;

public class StartChatCallResponseDto
{
    public string RoomUrl { get; set; } = string.Empty;
    public ChatMessageDto? Message { get; set; }
}
