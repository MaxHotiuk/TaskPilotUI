using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;
using UI.Interfaces.SignalR;

namespace UI.Services
{
    public class NotificationSignalRService : INotificationSignalRService
    {
        private readonly ILogger<NotificationSignalRService> _logger;
        private readonly string _apiBaseUrl;
        private HubConnection? _hubConnection;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public NotificationSignalRService(
            ILogger<NotificationSignalRService> logger, 
            IConfiguration configuration)
        {
            _logger = logger;
            _apiBaseUrl = configuration["Api:BaseUrl"] ?? throw new InvalidOperationException("API Base URL is not configured.");
        }

        public async Task ConnectAsync()
        {
            if (_hubConnection != null && _hubConnection.State != HubConnectionState.Disconnected)
                return;

            var baseUrl = _apiBaseUrl.TrimEnd('/');
            var hubUrl = $"{baseUrl}/hubs/notification";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            try
            {
                await _hubConnection.StartAsync();
                _logger.LogInformation("Connected to notification hub successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to notification hub");
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_hubConnection != null)
            {
                try
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                    _logger.LogInformation("Disconnected from notification hub");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while disconnecting from notification hub");
                    throw;
                }
            }
        }

        public async Task JoinUserGroupAsync(string userId)
        {
            if (_hubConnection != null && !string.IsNullOrEmpty(userId))
            {
                try
                {
                    await _hubConnection.InvokeAsync("JoinUserGroup", userId);
                    _logger.LogInformation($"Joined user group for user {userId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to join user group for user {userId}");
                    throw;
                }
            }
        }

        public async Task LeaveUserGroupAsync(string userId)
        {
            if (_hubConnection != null && !string.IsNullOrEmpty(userId))
            {
                try
                {
                    await _hubConnection.InvokeAsync("LeaveUserGroup", userId);
                    _logger.LogInformation($"Left user group for user {userId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to leave user group for user {userId}");
                    throw;
                }
            }
        }

        public void OnNotificationReceived(Action<object> handler)
        {
            _hubConnection?.On("ReceiveNotification", handler);
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
            {
                try
                {
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                    _logger.LogInformation("NotificationSignalRService disposed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while disposing NotificationSignalRService");
                }
            }
        }
    }
}