using Microsoft.AspNetCore.Components;
using UI.Models.Attachment;
using UI.Interfaces.Services;

namespace UI.Pages.Board.Components
{
    public partial class CommentAttachments : ComponentBase
    {
        [Parameter] public string CommentId { get; set; } = string.Empty;
        [Inject] public IAttachmentService AttachmentService { get; set; } = default!;
        private List<AttachmentDto> Attachments = new();
        private bool IsLoading = true;
        protected bool IsPreviewVisible = false;
        protected string? PreviewImageUrl;
        private static readonly HashSet<string> AllowedImageExtensions = new HashSet<string>
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"
        };


        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrEmpty(CommentId))
            {
                try
                {
                    Console.WriteLine($"Loading attachments for CommentId: {CommentId}");
                    Attachments = await AttachmentService.GetAsync(Guid.Parse(CommentId));
                }
                catch { }
            }
            IsLoading = false;
        }

        protected void ShowImagePreview(string url)
        {
            PreviewImageUrl = url;
            IsPreviewVisible = true;
        }

        protected void ClosePreview()
        {
            IsPreviewVisible = false;
            PreviewImageUrl = null;
        }
        protected static bool IsImage(string fileName)
        {
            var ext = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            return AllowedImageExtensions.Contains(ext);
        }
    }
}
