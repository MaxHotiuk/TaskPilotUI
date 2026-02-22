namespace UI.Models.Chat;

public class UpdateChatMembersRequestDto
{
    public Guid UserId { get; set; }
    public IEnumerable<Guid> MemberIds { get; set; } = Array.Empty<Guid>();
}
