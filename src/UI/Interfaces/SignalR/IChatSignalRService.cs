using UI.Models.Chat;

namespace UI.Interfaces.SignalR;

public interface IChatSignalRService : IAsyncDisposable
{
    Task ConnectAsync();
    Task DisconnectAsync();
    Task JoinUserGroupAsync(string userId);
    Task LeaveUserGroupAsync(string userId);
    Task JoinChatGroupAsync(string chatId);
    Task LeaveChatGroupAsync(string chatId);
    Task StartTypingAsync(string chatId, string userId);
    Task StopTypingAsync(string chatId, string userId);
    void OnChatCreated(Action<ChatDto> handler);
    void OnChatUpdated(Action<ChatDto> handler);
    void OnChatMessageReceived(Action<ChatMessageDto> handler);
    void OnUserTyping(Action<ChatTypingEvent> handler);
    void OnUserStoppedTyping(Action<ChatTypingEvent> handler);
    bool IsConnected { get; }
}
