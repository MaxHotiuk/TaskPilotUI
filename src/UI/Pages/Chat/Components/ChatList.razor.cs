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

    private readonly Dictionary<Guid, AvatarDto?> _avatarCache = new();
    private readonly HashSet<Guid> _avatarLoading = new();

    protected override void OnParametersSet()
    {
        foreach (var chat in Chats)
        {
            var otherMemberId = GetOtherMemberId(chat);
            if (otherMemberId.HasValue)
            {
                _ = LoadAvatarAsync(otherMemberId.Value);
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
            return "B";

        if (chat.Type == ChatType.Group)
            return "G";

        var member = chat.Members.FirstOrDefault(m => m.UserId != CurrentUserId);
        if (member == null || string.IsNullOrWhiteSpace(member.UserName))
            return "P";

        return member.UserName.Trim()[0].ToString().ToUpperInvariant();
    }

    private string? GetChatIcon(ChatDto chat)
    {
        return chat.Type switch
        {
            ChatType.Group => "team",
            ChatType.Board => "appstore",
            _ => null
        };
    }

    private bool TryGetChatAvatar(ChatDto chat, out string? avatarUrl)
    {
        avatarUrl = null;
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
}
