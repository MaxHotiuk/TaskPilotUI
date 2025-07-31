namespace UI.Models.Meeting;

public class MeetingMemberDto
{
    public Guid MeetingId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
}
