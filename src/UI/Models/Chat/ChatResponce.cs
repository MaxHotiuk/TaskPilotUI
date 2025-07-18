namespace UI.Models.Chat;

public class ChatResponse
{
    public string? Response { get; set; }
    public List<string>? Sources { get; set; }
    public string? SessionId { get; set; }
}
