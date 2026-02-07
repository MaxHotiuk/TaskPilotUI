using Microsoft.AspNetCore.SignalR.Client;
using UI.Interfaces.SignalR;
using UI.Models.Chat;

namespace UI.Services;

public class ChatSignalRService : IChatSignalRService
{
    private readonly ILogger<ChatSignalRService> _logger;
    private readonly string _apiBaseUrl;
    private HubConnection? _hubConnection;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public ChatSignalRService(ILogger<ChatSignalRService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _apiBaseUrl = configuration["Api:BaseUrl"] ?? throw new InvalidOperationException("API Base URL is not configured.");
    }

    public async Task ConnectAsync()
    {
        if (_hubConnection != null && _hubConnection.State != HubConnectionState.Disconnected)
            return;

        var baseUrl = _apiBaseUrl.TrimEnd('/');
        var hubUrl = $"{baseUrl}/hubs/chat";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        await _hubConnection.StartAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async Task JoinUserGroupAsync(string userId)
    {
        if (_hubConnection != null && !string.IsNullOrWhiteSpace(userId))
            await _hubConnection.InvokeAsync("JoinUserGroup", userId);
    }

    public async Task LeaveUserGroupAsync(string userId)
    {
        if (_hubConnection != null && !string.IsNullOrWhiteSpace(userId))
            await _hubConnection.InvokeAsync("LeaveUserGroup", userId);
    }

    public async Task JoinChatGroupAsync(string chatId)
    {
        if (_hubConnection != null && !string.IsNullOrWhiteSpace(chatId))
            await _hubConnection.InvokeAsync("JoinChatGroup", chatId);
    }

    public async Task LeaveChatGroupAsync(string chatId)
    {
        if (_hubConnection != null && !string.IsNullOrWhiteSpace(chatId))
            await _hubConnection.InvokeAsync("LeaveChatGroup", chatId);
    }

    public async Task StartTypingAsync(string chatId, string userId)
    {
        if (_hubConnection != null && !string.IsNullOrWhiteSpace(chatId) && !string.IsNullOrWhiteSpace(userId))
            await _hubConnection.InvokeAsync("StartTyping", chatId, userId);
    }

    public async Task StopTypingAsync(string chatId, string userId)
    {
        if (_hubConnection != null && !string.IsNullOrWhiteSpace(chatId) && !string.IsNullOrWhiteSpace(userId))
            await _hubConnection.InvokeAsync("StopTyping", chatId, userId);
    }

    public void OnChatCreated(Action<ChatDto> handler)
    {
        _hubConnection?.On("ChatCreated", handler);
    }

    public void OnChatUpdated(Action<ChatDto> handler)
    {
        _hubConnection?.On("ChatUpdated", handler);
    }

    public void OnChatMessageReceived(Action<ChatMessageDto> handler)
    {
        _hubConnection?.On("ReceiveChatMessage", handler);
    }

    public void OnUserTyping(Action<ChatTypingEvent> handler)
    {
        _hubConnection?.On("UserTyping", handler);
    }

    public void OnUserStoppedTyping(Action<ChatTypingEvent> handler)
    {
        _hubConnection?.On("UserStoppedTyping", handler);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }
}
