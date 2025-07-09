using System;

namespace UI.Models.Attachment;

public class AttachmentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = null!;
    public string Url { get; set; } = null!;
    public Guid EntityId { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? UploadedBy { get; set; }
}
