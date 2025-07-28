namespace UI.Models.Meeting;

public class CreateMeetingRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public string Domain { get; set; } = "https://localhost:5001";
    public Guid BoardId { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public int? Duration { get; set; }
    public List<Guid> MemberIds { get; set; } = new List<Guid>();
}