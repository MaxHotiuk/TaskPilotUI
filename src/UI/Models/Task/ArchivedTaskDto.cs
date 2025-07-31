namespace UI.Models.Task;

public class ArchivedTaskDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Assignee { get; init; }
    public DateTime? DueDate { get; init; }
}
