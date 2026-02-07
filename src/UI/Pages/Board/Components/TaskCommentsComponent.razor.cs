using UI.Interfaces.SignalR;
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
    [Inject] private ISignalRService SignalRService { get; set; } = default!;
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
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private bool HasMoreComments { get; set; } = false;
    private int TotalComments { get; set; } = 0;
    private string SearchTerm { get; set; } = string.Empty;
    private bool IsSearching { get; set; } = false;
    private Timer? _searchTimer;
    private bool IsLoading { get; set; } = true;
    private bool IsLoadingMore { get; set; } = false;
    private bool IsAdding { get; set; } = false;
    private bool IsUpdating { get; set; } = false;
    private string NewCommentContent { get; set; } = string.Empty;
    private string? EditingCommentId { get; set; }
    private string EditingContent { get; set; } = string.Empty;
    private List<AttachmentMemory> SelectedAttachments { get; set; } = new();
    private List<string> SelectedAttachmentNames { get; set; } = new();
    private List<AttachmentDto> UploadedAttachments { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadComments(isInitial: true);
        await PreloadAvatarsForComments();

        if (!string.IsNullOrEmpty(TaskId))
        {
            await SignalRService.ConnectAsync();
            await SignalRService.JoinTaskGroupAsync(TaskId);

            SignalRService.OnTaskUpdated(async payloadObj =>
            {
                try
                {
                    var payload = payloadObj as System.Text.Json.JsonElement?;
                    string? action = null;
                    string? commentId = null;
                    if (payload != null)
                    {
                        if (payload.Value.TryGetProperty("action", out var actionProp))
                        {
                            action = actionProp.GetString();
                        }
                        if (payload.Value.TryGetProperty("commentId", out var commentIdProp))
                        {
                            commentId = commentIdProp.GetString();
                        }
                    }
                    if (!string.IsNullOrEmpty(commentId) && (action == "commentCreated" || action == "commentUpdated"))
                    {
                        var comment = await CommentService.GetByIdAsync(commentId);
                        if (comment != null && comment.TaskId == TaskId)
                        {
                            await InvokeAsync(async () =>
                            {
                                if (action == "commentCreated")
                                {
                                    if (!Comments.Any(c => c.Id == comment.Id))
                                    {
                                        Comments.Insert(0, comment);
                                    }
                                }
                                else if (action == "commentUpdated")
                                {
                                    var idx = Comments.FindIndex(c => c.Id == comment.Id);
                                    if (idx >= 0)
                                    {
                                        Comments[idx] = comment;
                                    }
                                }
                                TotalComments = Comments.Count;
                                await PreloadAvatarsForComments();
                                StateHasChanged();
                            });
                        }
                    }
                    else if (!string.IsNullOrEmpty(commentId) && action == "commentDeleted")
                    {
                        await InvokeAsync(() =>
                        {
                            var idx = Comments.FindIndex(c => c.Id == commentId);
                            if (idx >= 0)
                            {
                                Comments.RemoveAt(idx);
                            }
                            TotalComments = Comments.Count;
                            StateHasChanged();
                            return Task.CompletedTask;
                        });
                    }
                }
                catch { }
            });
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrEmpty(TaskId))
        {
            await PreloadAvatarsForComments();
        }
    }

    private async Task LoadComments(bool isInitial = false)
    {
        if (string.IsNullOrEmpty(TaskId)) return;

        if (isInitial)
        {
            IsLoading = true;
            CurrentPage = 1;
            Comments.Clear();
        }
        else
        {
            IsLoadingMore = true;
        }

        StateHasChanged();

        try
        {
            List<CommentDto> newComments;
            
            if (IsSearching && !string.IsNullOrWhiteSpace(SearchTerm))
            {
                newComments = await CommentService.SearchCommentsAsync(
                    SearchTerm, Guid.Parse(TaskId), CurrentPage, PageSize);
            }
            else
            {
                if (CurrentPage == 1)
                {
                    var allComments = await CommentService.GetTaskCommentsAsync(TaskId);
                    newComments = allComments.Take(PageSize).ToList();
                    TotalComments = allComments.Count;
                }
                else
                {
                    var allComments = await CommentService.GetTaskCommentsAsync(TaskId);
                    newComments = allComments.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
                    TotalComments = allComments.Count;
                }
            }

            if (isInitial)
            {
                Comments = newComments;
            }
            else
            {
                Comments.AddRange(newComments);
            }

            if (IsSearching && !string.IsNullOrWhiteSpace(SearchTerm))
            {
                HasMoreComments = newComments.Count == PageSize;
            }
            else
            {
                HasMoreComments = Comments.Count < TotalComments;
            }
        }
        catch (Exception)
        {
            await NotificationService.Error(new NotificationConfig
            {
                Message = UI.Resources.I18n.Error,
                Description = UI.Resources.I18n.FailedToLoadComments
            });
        }
        finally
        {
            IsLoading = false;
            IsLoadingMore = false;
            StateHasChanged();
        }
    }

    private async Task LoadMoreComments()
    {
        if (IsLoadingMore || !HasMoreComments) return;
        
        CurrentPage++;
        await LoadComments(isInitial: false);
        await PreloadAvatarsForComments();
    }

    private void OnSearchComments()
    {
        _searchTimer?.Dispose();
        _searchTimer = new Timer(async _ => await PerformSearch(), null, 300, Timeout.Infinite);
    }

    private async Task PerformSearch()
    {
        await InvokeAsync(async () =>
        {
            IsSearching = !string.IsNullOrWhiteSpace(SearchTerm);
            CurrentPage = 1;
            await LoadComments(isInitial: true);
            await PreloadAvatarsForComments();
        });
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
            
            await LoadComments(isInitial: true);
            await PreloadAvatarsForComments();
        }
        catch (Exception ex)
        {
            await NotificationService.Error(new NotificationConfig
            {
                Message = UI.Resources.I18n.Error,
                Description = UI.Resources.I18n.FailedToAddComment + (ex.Message != null ? ": " + ex.Message : string.Empty)
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
            
            await LoadComments(isInitial: true);
            await PreloadAvatarsForComments();

            await NotificationService.Success(new NotificationConfig
            {
                Message = UI.Resources.I18n.Success,
                Description = UI.Resources.I18n.CommentUpdatedSuccess
            });
        }
        catch (Exception)
        {
            await NotificationService.Error(new NotificationConfig
            {
                Message = UI.Resources.I18n.Error,
                Description = UI.Resources.I18n.FailedToUpdateComment
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
            
            await LoadComments(isInitial: true);
            await PreloadAvatarsForComments();

            await NotificationService.Success(new NotificationConfig
            {
                Message = UI.Resources.I18n.Success,
                Description = UI.Resources.I18n.CommentDeletedSuccess
            });
        }
        catch (Exception)
        {
            await NotificationService.Error(new NotificationConfig
            {
                Message = UI.Resources.I18n.Error,
                Description = UI.Resources.I18n.FailedToDeleteComment
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
        var user = AllUsers.FirstOrDefault(u => u.Id.ToString() == authorId);
        return user?.Username ?? UI.Resources.I18n.UnknownUser;
    }

    private string GetAuthorInitials(string authorId)
    {
        var user = AllUsers.FirstOrDefault(u => u.Id.ToString() == authorId);
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
        if (string.IsNullOrEmpty(CurrentUserId)) return UI.Resources.I18n.You;

        var user = AllUsers.FirstOrDefault(u => u.Id.ToString() == CurrentUserId);
        return user?.Username ?? UI.Resources.I18n.You;
    }

    private string GetCurrentUserInitials()
    {
        if (string.IsNullOrEmpty(CurrentUserId)) return "Y";

        var user = AllUsers.FirstOrDefault(u => u.Id.ToString() == CurrentUserId);
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
            return UI.Resources.I18n.JustNow;
        if (timeSpan.TotalMinutes < 60)
            return string.Format(UI.Resources.I18n.MinutesAgo, (int)timeSpan.TotalMinutes);
        if (timeSpan.TotalHours < 24)
            return string.Format(UI.Resources.I18n.HoursAgo, (int)timeSpan.TotalHours);
        if (timeSpan.TotalDays < 7)
            return string.Format(UI.Resources.I18n.DaysAgo, (int)timeSpan.TotalDays);

        return dateTime.ToString("MMM dd, yyyy");
    }

    private string FormatCommentContent(string content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        return content.Replace("\n", "<br>").Replace("\r", "");
    }

    private void RemoveAttachmentAt(int index)
    {
        if (index >= 0 && index < SelectedAttachmentNames.Count)
        {
            var name = SelectedAttachmentNames[index];
            SelectedAttachmentNames.RemoveAt(index);
            var attachmentToRemove = SelectedAttachments.FirstOrDefault(a => a.Name == name);
            if (attachmentToRemove != null)
            {
                SelectedAttachments.Remove(attachmentToRemove);
            }
            StateHasChanged();
        }
    }

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

    public void Dispose()
    {
        // Leave SignalR group for this task
        if (!string.IsNullOrEmpty(TaskId))
        {
            _ = SignalRService.LeaveTaskGroupAsync(TaskId);
        }
        _searchTimer?.Dispose();
    }
    
    private class AttachmentMemory
    {
        public string Name { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
    }
}