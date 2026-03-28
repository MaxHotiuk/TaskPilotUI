namespace UI.Models.Organization;

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
