namespace UI.Models.Organization;

public class SendManagerRequestDto
{
    public Guid UserId { get; set; }
    public string Message { get; set; } = string.Empty;
}
