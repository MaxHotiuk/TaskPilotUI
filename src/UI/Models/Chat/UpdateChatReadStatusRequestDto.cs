namespace UI.Models.Chat;

public class UpdateChatReadStatusRequestDto
{
    public Guid UserId { get; set; }
    public DateTime ReadAt { get; set; }
}
