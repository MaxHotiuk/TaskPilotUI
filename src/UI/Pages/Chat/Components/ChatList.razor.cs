using Microsoft.AspNetCore.Components;
using UI.Interfaces.Services;
using UI.Models.Avatar;
using UI.Models.Chat;

namespace UI.Pages.Chat.Components;

public partial class ChatList : ComponentBase
{
    [Parameter] public IReadOnlyList<ChatDto> Chats { get; set; } = Array.Empty<ChatDto>();
    [Parameter] public Guid? SelectedChatId { get; set; }
    [Parameter] public Guid CurrentUserId { get; set; }
    [Parameter] public EventCallback<ChatDto> OnSelectChat { get; set; }

    [Inject] private IAvatarService AvatarService { get; set; } = default!;
    [Inject] private IChatSystemService ChatService { get; set; } = default!;

    private readonly Dictionary<Guid, AvatarDto?> _avatarCache = new();
    private readonly HashSet<Guid> _avatarLoading = new();
    private readonly Dictionary<Guid, ChatAvatarDto?> _chatAvatarCache = new();
    private readonly HashSet<Guid> _chatAvatarLoading = new();

    protected override void OnParametersSet()
    {
        foreach (var chat in Chats)
        {
            var otherMemberId = GetOtherMemberId(chat);
            if (otherMemberId.HasValue)
            {
                _ = LoadAvatarAsync(otherMemberId.Value);
            }

            if (chat.Type != ChatType.Private)
            {
                _ = LoadChatAvatarAsync(chat.Id);
            }
        }
    }

    private string GetChatTitle(ChatDto chat)
    {
        if (chat.Type == ChatType.Board)
            return string.IsNullOrWhiteSpace(chat.Name) ? "Board chat" : chat.Name;

        if (chat.Type == ChatType.Group)
            return string.IsNullOrWhiteSpace(chat.Name) ? "Group chat" : chat.Name;

        var otherMember = chat.Members.FirstOrDefault(member => member.UserId != CurrentUserId);
        if (otherMember != null && !string.IsNullOrWhiteSpace(otherMember.UserName))
            return otherMember.UserName;

        return "Private chat";
    }

    private string GetChatInitials(ChatDto chat)
    {
        if (chat.Type == ChatType.Board)
            return GetInitials(string.IsNullOrWhiteSpace(chat.Name) ? "Board" : chat.Name);

        if (chat.Type == ChatType.Group)
            return GetInitials(string.IsNullOrWhiteSpace(chat.Name) ? "Group" : chat.Name);

        var member = chat.Members.FirstOrDefault(m => m.UserId != CurrentUserId);
        if (member == null || string.IsNullOrWhiteSpace(member.UserName))
            return "P";

        return GetInitials(member.UserName);
    }

    private bool TryGetChatAvatar(ChatDto chat, out string? avatarUrl)
    {
        avatarUrl = null;
        if (chat.Type == ChatType.Private)
        {
            var otherMemberId = GetOtherMemberId(chat);
            if (!otherMemberId.HasValue)
                return false;

            if (_avatarCache.TryGetValue(otherMemberId.Value, out var avatar) && avatar != null && !string.IsNullOrEmpty(avatar.CompressedUrl))
            {
                avatarUrl = avatar.CompressedUrl;
                return true;
            }

            return false;
        }

        if (_chatAvatarCache.TryGetValue(chat.Id, out var chatAvatar) && chatAvatar != null && !string.IsNullOrEmpty(chatAvatar.CompressedUrl))
        {
            avatarUrl = chatAvatar.CompressedUrl;
            return true;
        }

        return false;
    }

    private Guid? GetOtherMemberId(ChatDto chat)
    {
        if (chat.Type != ChatType.Private)
            return null;

        var member = chat.Members.FirstOrDefault(m => m.UserId != CurrentUserId);
        return member?.UserId;
    }

    private bool IsChatUnread(ChatDto chat)
    {
        if (chat.LastMessage == null)
            return false;

        if (chat.LastMessage.SenderId == CurrentUserId)
            return false;

        var member = chat.Members.FirstOrDefault(m => m.UserId == CurrentUserId);
        if (member?.LastReadAt == null)
            return true;

        return chat.LastMessage.CreatedAt > member.LastReadAt.Value;
    }

    private const int MessagePreviewMaxLength = 15;

    private string GetLastMessagePreviewText(ChatDto chat, ChatMessagePreviewDto message)
    {
        var content = GetMessagePreviewContent(message);
        var truncated = TruncateContent(content);

        if (IsSystemMessage(message))
            return truncated;

        var senderName = ResolveSenderName(chat, message);
        return $"{senderName}: {truncated}";
    }

    private string ResolveSenderName(ChatDto chat, ChatMessagePreviewDto message)
    {
        if (message.SenderId == CurrentUserId)
            return "You";

        if (!string.IsNullOrWhiteSpace(message.SenderName))
            return message.SenderName;

        var memberName = chat.Members.FirstOrDefault(member => member.UserId == message.SenderId)?.UserName;
        if (!string.IsNullOrWhiteSpace(memberName))
            return memberName;

        return "Someone";
    }

    private string GetMessagePreviewContent(ChatMessagePreviewDto message)
    {
        if (string.Equals(message.MessageType, "Call", StringComparison.OrdinalIgnoreCase))
            return "started a call";

        if (IsTaskMessage(message))
            return string.IsNullOrWhiteSpace(message.Content) ? "Task update" : message.Content;

        if (IsUpdateMessage(message))
            return string.IsNullOrWhiteSpace(message.Content) ? "Chat update" : message.Content;

        return message.Content ?? string.Empty;
    }

    private bool IsTaskMessage(ChatMessagePreviewDto message)
    {
        return string.Equals(message.MessageType, "Task", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsUpdateMessage(ChatMessagePreviewDto message)
    {
        return string.Equals(message.MessageType, "Update", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsSystemMessage(ChatMessagePreviewDto message)
    {
        return IsTaskMessage(message) || IsUpdateMessage(message);
    }

    private string TruncateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        if (content.Length <= MessagePreviewMaxLength)
            return content;

        return content.Substring(0, MessagePreviewMaxLength) + "...";
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
        }
        catch
        {
            _avatarCache[userId] = null;
        }
        finally
        {
            _avatarLoading.Remove(userId);
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadChatAvatarAsync(Guid chatId)
    {
        if (CurrentUserId == Guid.Empty)
            return;

        if (_chatAvatarCache.ContainsKey(chatId) || _chatAvatarLoading.Contains(chatId))
            return;

        _chatAvatarLoading.Add(chatId);
        try
        {
            var avatar = await ChatService.GetChatAvatarOrNullAsync(chatId, CurrentUserId);
            _chatAvatarCache[chatId] = avatar;
        }
        catch
        {
            _chatAvatarCache[chatId] = null;
        }
        finally
        {
            _chatAvatarLoading.Remove(chatId);
            await InvokeAsync(StateHasChanged);
        }
    }

    private string GetInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "?";

        var parts = value.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => part[0].ToString().ToUpperInvariant())
            .ToArray();

        return parts.Length == 0 ? "?" : string.Concat(parts);
    }
}
