namespace UI.Models.Tag;

public class CreateTagRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}
