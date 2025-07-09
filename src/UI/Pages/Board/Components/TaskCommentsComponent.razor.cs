using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using AntDesign;
using UI.Models.Comment;
using UI.Models.User;
using UI.Interfaces.Services;
using UI.Models.Avatar;
using UI.Models.Attachment;
using System.Collections.Concurrent;

namespace UI.Pages.Board.Components;

public partial class TaskCommentsComponent : ComponentBase
{
    [Inject] private IAttachmentService AttachmentService { get; set; } = default!;
    [Inject] private IMessageService Message { get; set; } = default!;
    [Parameter] public string TaskId { get; set; } = string.Empty;
    [Parameter] public List<UserDto> AllUsers { get; set; } = new();
    [Parameter] public bool CanAddComment { get; set; } = true;
    [Parameter] public string? CurrentUserId { get; set; }

    [Inject] private ICommentService CommentService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;
    [Inject] private IAvatarService AvatarService { get; set; } = default!;
    private ConcurrentDictionary<string, AvatarDto?> _avatarCache = new();
    private ConcurrentDictionary<string, bool> _avatarLoading = new();

    private List<CommentDto> Comments { get; set; } = new();
    private bool IsLoading { get; set; } = true;
    private bool IsAdding { get; set; } = false;
    private bool IsUpdating { get; set; } = false;
    private string NewCommentContent { get; set; } = string.Empty;

    private string? EditingCommentId { get; set; }
    private string EditingContent { get; set; } = string.Empty;

    private List<AttachmentMemory> SelectedAttachments { get; set; } = new();
    private List<string> SelectedAttachmentNames { get; set; } = new();
    private List<AttachmentDto> UploadedAttachments { get; set; } = new();


    public async Task OnAttachmentsSelected(Microsoft.AspNetCore.Components.Forms.InputFileChangeEventArgs e)
    {
        if (e.FileCount == 0) return;

        var newFiles = e.GetMultipleFiles();
        foreach (var file in newFiles)
        {
            if (!SelectedAttachments.Any(f => f.Name == file.Name))
            {
                using var stream = file.OpenReadStream(long.MaxValue);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                SelectedAttachments.Add(new AttachmentMemory
                {
                    Name = file.Name,
                    Data = ms.ToArray(),
                    ContentType = file.ContentType
                });
                SelectedAttachmentNames.Add(file.Name);
            }
        }
        StateHasChanged();
    }


    private void ClearAttachments()
    {
        SelectedAttachments.Clear();
        SelectedAttachmentNames.Clear();
        UploadedAttachments.Clear();
        StateHasChanged();
    }


    protected override async Task OnInitializedAsync()
    {
        await LoadComments();
        await PreloadAvatarsForComments();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrEmpty(TaskId))
        {
            await LoadComments();
            await PreloadAvatarsForComments();
        }
    }
    private async Task PreloadAvatarsForComments()
    {
        if (Comments == null) return;
        var userIds = Comments.Select(c => c.AuthorId).Distinct().ToList();
        if (!string.IsNullOrEmpty(CurrentUserId))
            userIds.Add(CurrentUserId);
        foreach (var userId in userIds)
        {
            await LoadAvatarAsync(userId);
        }
    }

    private async Task LoadAvatarAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId) || _avatarCache.ContainsKey(userId) || _avatarLoading.ContainsKey(userId))
            return;
        _avatarLoading[userId] = true;
        try
        {
            if (Guid.TryParse(userId, out var guid))
            {
                var avatar = await AvatarService.GetAvatarOrNullAsync(guid);
                _avatarCache[userId] = avatar;
            }
            else
            {
                _avatarCache[userId] = null;
            }
        }
        catch
        {
            _avatarCache[userId] = null;
        }
        finally
        {
            _avatarLoading.TryRemove(userId, out _);
            StateHasChanged();
        }
    }

    private string? GetAvatarUrl(string userId)
    {
        if (_avatarCache.TryGetValue(userId, out var avatar) && avatar != null && !string.IsNullOrEmpty(avatar.CompressedUrl))
            return avatar.CompressedUrl;
        return null;
    }

    private bool IsAvatarLoading(string userId) => _avatarLoading.ContainsKey(userId);

    private async Task LoadComments()
    {
        if (string.IsNullOrEmpty(TaskId)) return;

        IsLoading = true;
        StateHasChanged();

        try
        {
            Comments = await CommentService.GetTaskCommentsAsync(TaskId);
        }
        catch (Exception)
        {
            await NotificationService.Error(new NotificationConfig
            {
                Message = "Error",
                Description = "Failed to load comments"
            });
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private async Task AddComment()
    {
        if (string.IsNullOrWhiteSpace(NewCommentContent) || string.IsNullOrEmpty(CurrentUserId))
            return;

        IsAdding = true;
        StateHasChanged();

        try
        {
            var request = new CreateCommentRequest
            {
                TaskId = TaskId,
                AuthorId = CurrentUserId,
                Content = NewCommentContent.Trim()
            };

            var createdCommentId = await CommentService.CreateAsync(request);
            createdCommentId = createdCommentId.Substring(1, createdCommentId.Length - 2);

            if (SelectedAttachments != null && SelectedAttachments.Any())
            {
                foreach (var attachment in SelectedAttachments)
                {
                    using var ms = new MemoryStream(attachment.Data);
                    await AttachmentService.UploadAsync(Guid.Parse(createdCommentId), ms, attachment.Name);
                }
            }

            NewCommentContent = string.Empty;
            ClearAttachments();
            await LoadComments();
        }
        catch (Exception ex)
        {
            await NotificationService.Error(new NotificationConfig
            {
                Message = "Error",
                Description = $"Failed to add comment{(ex.Message != null ? ": " + ex.Message : string.Empty)}"
            });
        }
        finally
        {
            IsAdding = false;
            StateHasChanged();
        }
    }

    private void StartEditComment(CommentDto comment)
    {
        EditingCommentId = comment.Id;
        EditingContent = comment.Content;
        StateHasChanged();
    }

    private async Task SaveEditComment(string commentId)
    {
        if (string.IsNullOrWhiteSpace(EditingContent))
            return;

        IsUpdating = true;
        StateHasChanged();

        try
        {
            var request = new UpdateCommentRequest
            {
                Content = EditingContent.Trim()
            };

            await CommentService.UpdateAsync(commentId, request);
            EditingCommentId = null;
            EditingContent = string.Empty;
            await LoadComments();

            await NotificationService.Success(new NotificationConfig
            {
                Message = "Success",
                Description = "Comment updated successfully"
            });
        }
        catch (Exception)
        {
            await NotificationService.Error(new NotificationConfig
            {
                Message = "Error",
                Description = "Failed to update comment"
            });
        }
        finally
        {
            IsUpdating = false;
            StateHasChanged();
        }
    }

    private void CancelEditComment()
    {
        EditingCommentId = null;
        EditingContent = string.Empty;
        StateHasChanged();
    }

    private async Task DeleteComment(string commentId)
    {
        try
        {
            await CommentService.DeleteAsync(commentId);
            await LoadComments();

            await NotificationService.Success(new NotificationConfig
            {
                Message = "Success",
                Description = "Comment deleted successfully"
            });
        }
        catch (Exception)
        {
            await NotificationService.Error(new NotificationConfig
            {
                Message = "Error",
                Description = "Failed to delete comment"
            });
        }
    }

    private void ClearNewComment()
    {
        NewCommentContent = string.Empty;
        StateHasChanged();
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.CtrlKey && e.Key == "Enter" && !string.IsNullOrWhiteSpace(NewCommentContent))
        {
            await AddComment();
        }
    }

    private bool CanEditComment(CommentDto comment)
    {
        return comment.AuthorId == CurrentUserId;
    }

    private string GetAuthorName(string authorId)
    {
        var user = AllUsers.FirstOrDefault(u => u.Id == authorId);
        return user?.Username ?? "Unknown User";
    }

    private string GetAuthorInitials(string authorId)
    {
        var user = AllUsers.FirstOrDefault(u => u.Id == authorId);
        if (user == null) return "U";

        var parts = user.Username.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"{parts[0][0]}{parts[1][0]}".ToUpper();
        }
        return user.Username.Length > 0 ? user.Username[0].ToString().ToUpper() : "U";
    }

    private string GetCurrentUserName()
    {
        if (string.IsNullOrEmpty(CurrentUserId)) return "You";

        var user = AllUsers.FirstOrDefault(u => u.Id == CurrentUserId);
        return user?.Username ?? "You";
    }

    private string GetCurrentUserInitials()
    {
        if (string.IsNullOrEmpty(CurrentUserId)) return "Y";

        var user = AllUsers.FirstOrDefault(u => u.Id == CurrentUserId);
        if (user == null) return "Y";

        var parts = user.Username.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"{parts[0][0]}{parts[1][0]}".ToUpper();
        }
        return user.Username.Length > 0 ? user.Username[0].ToString().ToUpper() : "Y";
    }

    private string FormatDate(DateTime dateTime)
    {
        var now = DateTime.Now;
        var timeSpan = now - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "Just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";

        return dateTime.ToString("MMM dd, yyyy");
    }

    private string FormatCommentContent(string content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        return content.Replace("\n", "<br>").Replace("\r", "");
    }
    
    private class AttachmentMemory
    {
        public string Name { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
    }
}
