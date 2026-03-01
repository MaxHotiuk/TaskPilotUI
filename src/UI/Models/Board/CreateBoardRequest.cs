namespace UI.Models.Board;

public class CreateBoardRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
}
