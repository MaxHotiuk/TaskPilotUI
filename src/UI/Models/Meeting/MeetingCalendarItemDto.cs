namespace UI.Models.Meeting;

public class MeetingCalendarItemDto
{
    public Guid Id { get; init; }
    public Guid BoardId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime? ScheduledAt { get; init; }
    public int? Duration { get; init; }
    public string? Description { get; init; }
}