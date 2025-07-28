namespace UI.Models.Meeting;

public class MeetingCalendarItemDto
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public int? Duration { get; set; }
    public string? Description { get; set; }
}