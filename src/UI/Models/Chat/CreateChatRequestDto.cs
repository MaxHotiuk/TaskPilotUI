namespace UI.Models.Chat;

public class CreateChatRequestDto
{
    public Guid OrganizationId { get; set; }
    public Guid CreatedById { get; set; }
    public ChatType Type { get; set; }
    public string? Name { get; set; }
    public List<Guid> MemberIds { get; set; } = new();
}
