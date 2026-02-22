namespace UI.Models.Chat;

public record ChatAvatarDto
{
    public string ChatId { get; init; } = string.Empty;
    public string OriginalUrl { get; init; } = string.Empty;
    public string CompressedUrl { get; init; } = string.Empty;
    public int Size { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
}
