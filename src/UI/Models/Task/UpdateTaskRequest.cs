namespace UI.Models.Task;

public class UpdateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StateId { get; set; }
    public string? AssigneeId { get; set; }
    public string? DueDate { get; set; }
}
