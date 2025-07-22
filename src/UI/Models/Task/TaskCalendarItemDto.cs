namespace UI.Models.Task;

public record TaskCalendarItemDto
{
    public Guid Id { get; init; }
    public Guid BoardId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime? DueDate { get; init; }
}
