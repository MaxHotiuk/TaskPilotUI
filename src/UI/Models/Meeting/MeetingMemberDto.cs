namespace UI.Models.Meeting;

public class MeetingMemberDto
{
    public Guid MeetingId { get; init; }
    public Guid UserId { get; init; }
    public string Status { get; init; } = string.Empty;
}
