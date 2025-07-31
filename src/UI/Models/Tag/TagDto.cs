namespace UI.Models.Tag;

public class TagDto
{
    public int Id { get; set; }
    public Guid BoardId { get; init; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
}
