namespace UI.Models.Meeting;

public class MeetingDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public Guid BoardId { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public int? Duration { get; set; }
    public string Status { get; set; } = string.Empty;
}
