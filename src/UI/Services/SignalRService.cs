using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;
using UI.Interfaces.SignalR;

namespace UI.Services
{
    public class SignalRService : ISignalRService
    {
        private readonly NavigationManager _navigationManager;
        private readonly ILogger<SignalRService> _logger;
        private readonly string _apiBaseUrl;
        private HubConnection? _hubConnection;
        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public SignalRService(NavigationManager navigationManager, ILogger<SignalRService> logger, IConfiguration configuration)
        {
            _navigationManager = navigationManager;
            _logger = logger;
            _apiBaseUrl = configuration["Api:BaseUrl"] ?? throw new InvalidOperationException("API Base URL is not configured.");
        }

        public async Task ConnectAsync()
        {
            if (_hubConnection != null && _hubConnection.State != HubConnectionState.Disconnected)
                return;

            var baseUrl = _apiBaseUrl.TrimEnd('/');
            var hubUrl = $"{baseUrl}/hubs/board";

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

        public async Task JoinBoardGroupAsync(string boardId)
        {
            if (_hubConnection != null)
                await _hubConnection.InvokeAsync("JoinBoardGroup", boardId);
        }

        public async Task LeaveBoardGroupAsync(string boardId)
        {
            if (_hubConnection != null)
                await _hubConnection.InvokeAsync("LeaveBoardGroup", boardId);
        }

        public async Task JoinTaskGroupAsync(string taskId)
        {
            if (_hubConnection != null)
                await _hubConnection.InvokeAsync("JoinTaskGroup", taskId);
        }

        public async Task LeaveTaskGroupAsync(string taskId)
        {
            if (_hubConnection != null)
                await _hubConnection.InvokeAsync("LeaveTaskGroup", taskId);
        }

        public void OnBoardUpdated(Action<object> handler)
        {
            _hubConnection?.On("BoardUpdated", handler);
        }

        public void OnTaskUpdated(Action<object> handler)
        {
            _hubConnection?.On("TaskUpdated", handler);
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
}
