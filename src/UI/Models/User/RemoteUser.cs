namespace UI.Models.User;

public class RemoteUser
{
    public string? UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? VideoId { get; set; }
    public string ConnectionStatus { get; set; } = "connecting";
    public bool IsScreenSharing { get; set; } = false;
}