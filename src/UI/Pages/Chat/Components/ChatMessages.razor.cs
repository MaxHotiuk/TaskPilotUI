using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using UI.Interfaces.Services;
using UI.Models.Avatar;
using UI.Models.Chat;

namespace UI.Pages.Chat.Components;

public partial class ChatMessages : ComponentBase
{
    [Parameter] public ChatDto? ActiveChat { get; set; }
    [Parameter] public IReadOnlyList<ChatMessageDto> Messages { get; set; } = Array.Empty<ChatMessageDto>();
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public bool IsSending { get; set; }
    [Parameter] public bool HasMoreMessages { get; set; }
    [Parameter] public Guid CurrentUserId { get; set; }
    [Parameter] public string NewMessage { get; set; } = string.Empty;
    [Parameter] public IReadOnlyList<string> TypingUsers { get; set; } = Array.Empty<string>();
    [Parameter] public EventCallback<string> NewMessageChanged { get; set; }
    [Parameter] public EventCallback OnSend { get; set; }
    [Parameter] public EventCallback OnLoadMore { get; set; }
    [Parameter] public EventCallback OnStopTyping { get; set; }
    [Parameter] public IReadOnlyList<string> PendingAttachmentNames { get; set; } = Array.Empty<string>();
    [Parameter] public bool HasPendingAttachments { get; set; }
    [Parameter] public EventCallback<InputFileChangeEventArgs> OnAttachmentsSelected { get; set; }
    [Parameter] public EventCallback<int> OnRemoveAttachment { get; set; }
    [Parameter] public EventCallback OnClearAttachments { get; set; }

    [Inject] private IAvatarService AvatarService { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
    private readonly Dictionary<Guid, AvatarDto?> _avatarCache = new();
    private readonly HashSet<Guid> _avatarLoading = new();
    private ElementReference _threadRef;
    private Guid? _lastChatId;
    private int _lastMessageCount;
    private bool _forceScrollToBottom;
    private bool _messagesChanged;
    private bool _isPinnedToBottom = true;
    private bool _isLoadingOlder;
    private double? _previousScrollHeight;
    private double? _previousScrollTop;
    protected override void OnParametersSet()
    {
        if (_lastChatId != ActiveChat?.Id)
        {
            _lastChatId = ActiveChat?.Id;
            _forceScrollToBottom = true;
            _isPinnedToBottom = true;
            _lastMessageCount = 0;
            _avatarCache.Clear();
            _avatarLoading.Clear();
        }

        if (_lastMessageCount != Messages.Count)
        {
            _messagesChanged = true;
            _lastMessageCount = Messages.Count;
        }

        var senderIds = Messages
            .Select(message => message.SenderId)
            .Where(senderId => senderId != Guid.Empty)
            .Distinct()
            .ToList();

        foreach (var senderId in senderIds)
        {
            if (!_avatarCache.ContainsKey(senderId) && !_avatarLoading.Contains(senderId))
            {
                _ = LoadAvatarAsync(senderId);
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_threadRef.Equals(default))
            return;

        if (_isLoadingOlder && _previousScrollHeight.HasValue && _previousScrollTop.HasValue)
        {
            var info = await JsRuntime.InvokeAsync<ScrollInfo>("chatHelpers.getScrollInfo", _threadRef);
            var newScrollTop = _previousScrollTop.Value + (info.ScrollHeight - _previousScrollHeight.Value);
            await JsRuntime.InvokeVoidAsync("chatHelpers.setScrollTop", _threadRef, newScrollTop);
            _isLoadingOlder = false;
        }

        if ((firstRender || _forceScrollToBottom || (_messagesChanged && _isPinnedToBottom)) && Messages.Any())
        {
            await JsRuntime.InvokeVoidAsync("chatHelpers.scrollToBottom", _threadRef);
            _forceScrollToBottom = false;
            _messagesChanged = false;
            _isPinnedToBottom = true;
        }
        else
        {
            _messagesChanged = false;
        }
    }

    private Task OnMessageInput(ChangeEventArgs e)
    {
        var value = e.Value?.ToString() ?? string.Empty;
        NewMessage = value;
        return NewMessageChanged.InvokeAsync(value);
    }

    private Task OnMessageBlur()
    {
        return OnStopTyping.InvokeAsync();
    }

    private Task OnAttachmentsSelectedInternal(InputFileChangeEventArgs e)
    {
        return OnAttachmentsSelected.InvokeAsync(e);
    }

    private async Task HandleScrollAsync()
    {
        if (_threadRef.Equals(default))
            return;

        _isPinnedToBottom = await JsRuntime.InvokeAsync<bool>("chatHelpers.isNearBottom", _threadRef, 80);
        var isNearTop = await JsRuntime.InvokeAsync<bool>("chatHelpers.isNearTop", _threadRef, 40);

        if (isNearTop && HasMoreMessages && !IsLoading && !_isLoadingOlder)
        {
            _isLoadingOlder = true;
            var info = await JsRuntime.InvokeAsync<ScrollInfo>("chatHelpers.getScrollInfo", _threadRef);
            _previousScrollHeight = info.ScrollHeight;
            _previousScrollTop = info.ScrollTop;
            await OnLoadMore.InvokeAsync();
        }
    }

    private string GetSenderInitials(string senderName)
    {
        if (string.IsNullOrWhiteSpace(senderName))
            return "?";

        return senderName.Trim()[0].ToString().ToUpperInvariant();
    }

    private bool IsOwnMessage(ChatMessageDto message)
    {
        return message.SenderId == CurrentUserId;
    }

    private bool ShouldShowSender(ChatMessageDto message)
    {
        return ActiveChat?.Type == ChatType.Group && !IsOwnMessage(message);
    }

    private bool ShouldShowReadStatus(ChatMessageDto message)
    {
        return IsOwnMessage(message);
    }

    private string GetReadStatus(ChatMessageDto message)
    {
        if (ActiveChat == null)
            return string.Empty;

        var otherMembers = ActiveChat.Members.Where(member => member.UserId != CurrentUserId).ToList();
        if (!otherMembers.Any())
            return string.Empty;

        var isRead = otherMembers.All(member => member.LastReadAt.HasValue && member.LastReadAt.Value >= message.CreatedAt);
        return isRead ? "Read" : "Sent";
    }

    private string GetTypingText()
    {
        if (!TypingUsers.Any())
            return string.Empty;

        if (TypingUsers.Count == 1)
            return $"{TypingUsers[0]} is typing...";

        if (TypingUsers.Count == 2)
            return $"{TypingUsers[0]} and {TypingUsers[1]} are typing...";

        return $"{TypingUsers.Count} people are typing...";
    }

    private sealed class ScrollInfo
    {
        public double ScrollTop { get; set; }
        public double ScrollHeight { get; set; }
        public double ClientHeight { get; set; }
    }

    private bool TryGetAvatar(ChatMessageDto message, out string? avatarUrl)
    {
        if (_avatarCache.TryGetValue(message.SenderId, out var avatar) && avatar != null && !string.IsNullOrEmpty(avatar.CompressedUrl))
        {
            avatarUrl = avatar.CompressedUrl;
            return true;
        }

        avatarUrl = null;
        return false;
    }

    private async Task LoadAvatarAsync(Guid userId)
    {
        if (_avatarCache.ContainsKey(userId) || _avatarLoading.Contains(userId))
            return;

        _avatarLoading.Add(userId);
        try
        {
            var avatar = await AvatarService.GetAvatarOrNullAsync(userId);
            _avatarCache[userId] = avatar;
            await InvokeAsync(StateHasChanged);
        }
        catch
        {
            _avatarCache[userId] = null;
        }
        finally
        {
            _avatarLoading.Remove(userId);
        }
    }

    private string GetChatTitle()
    {
        if (ActiveChat == null)
            return "Chat";

        if (ActiveChat.Type == ChatType.Group)
            return string.IsNullOrWhiteSpace(ActiveChat.Name) ? "Group chat" : ActiveChat.Name;

        var otherMember = ActiveChat.Members.FirstOrDefault(member => member.UserId != CurrentUserId);
        if (otherMember != null && !string.IsNullOrWhiteSpace(otherMember.UserName))
            return otherMember.UserName;

        return "Chat";
    }
}
