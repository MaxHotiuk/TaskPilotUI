namespace UI.Models.Meeting;

public class MeetingDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Link { get; init; } = string.Empty;
    public Guid BoardId { get; init; }
    public Guid CreatedBy { get; init; }
    public DateTime? ScheduledAt { get; init; }
    public int? Duration { get; init; }
    public string Status { get; init; } = string.Empty;
}
