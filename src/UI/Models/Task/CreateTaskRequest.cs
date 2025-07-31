namespace UI.Models.Task;

public class CreateTaskRequest
{
    public string BoardId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StateId { get; set; }
    public string? AssigneeId { get; set; }
    public int? TagId { get; set; }
    public int Priority { get; set; } = 2;
    public string? DueDate { get; set; }
}
