namespace UI.Models.Backlog;

public record BacklogDto
{
    public string Description { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}