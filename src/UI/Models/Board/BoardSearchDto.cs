namespace UI.Models.Board;

public class BoardSearchDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int NumberOfMembers { get; set; }
    public int NumberOfTasks { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}
