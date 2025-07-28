namespace UI.Models.Meeting;

public class CreateMeetingRequestDto
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
    public Guid BoardId { get; init; }
    public Guid CreatedBy { get; init; }
    public DateTime? ScheduledAt { get; init; }
    public int? Duration { get; init; }
    public List<Guid> MemberIds { get; init; } = new List<Guid>();
}