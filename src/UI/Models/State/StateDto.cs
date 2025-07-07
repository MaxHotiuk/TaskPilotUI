namespace UI.Models.State;

public class StateDto
{
    public int Id { get; set; }
    public string BoardId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}
