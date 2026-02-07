using Microsoft.AspNetCore.Components;
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
    [Parameter] public EventCallback<string> NewMessageChanged { get; set; }
    [Parameter] public EventCallback OnSend { get; set; }
    [Parameter] public EventCallback OnLoadMore { get; set; }

    [Inject] private IAvatarService AvatarService { get; set; } = default!;

    private readonly Dictionary<Guid, AvatarDto?> _avatarCache = new();
    private readonly HashSet<Guid> _avatarLoading = new();

    protected override void OnParametersSet()
    {
        foreach (var message in Messages)
        {
            _ = LoadAvatarAsync(message.SenderId);
        }
    }

    private Task OnMessageInput(ChangeEventArgs e)
    {
        var value = e.Value?.ToString() ?? string.Empty;
        NewMessage = value;
        return NewMessageChanged.InvokeAsync(value);
    }

    private string GetSenderInitials(string senderName)
    {
        if (string.IsNullOrWhiteSpace(senderName))
            return "?";

        return senderName.Trim()[0].ToString().ToUpperInvariant();
    }

    private bool TryGetAvatar(Guid senderId, out string? avatarUrl)
    {
        avatarUrl = null;
        if (_avatarCache.TryGetValue(senderId, out var avatar) && avatar != null && !string.IsNullOrEmpty(avatar.CompressedUrl))
        {
            avatarUrl = avatar.CompressedUrl;
            return true;
        }

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
