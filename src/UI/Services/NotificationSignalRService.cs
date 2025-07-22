using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;
using UI.Interfaces.SignalR;
using Notification = UI.Models.Notification.Notification;
using NotificationType = UI.Models.Notification.NotificationType;
using AntDesign;

namespace UI.Services
{
    public class NotificationSignalRService : INotificationSignalRService
    {
        private readonly ILogger<NotificationSignalRService> _logger;
        private readonly string _apiBaseUrl;
        private readonly IMessageService _messageService;
        private HubConnection? _hubConnection;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public NotificationSignalRService(
            ILogger<NotificationSignalRService> logger, 
            IConfiguration configuration,
            IMessageService messageService)
        {
            _logger = logger;
            _apiBaseUrl = configuration["Api:BaseUrl"] ?? throw new InvalidOperationException("API Base URL is not configured.");
            _messageService = messageService;
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

            SetupNotificationHandler();

            try
            {
                await _hubConnection.StartAsync();
            }
            catch (Exception)
            {
            }
        }

        private void SetupNotificationHandler()
        {
            _hubConnection?.On<Notification>("ReceiveNotification", (notification) =>
            {
                try
                {
                    ShowNotificationMessage(notification);
                }
                catch (Exception)
                {
                }
            });
        }

        private void ShowNotificationMessage(Notification notification)
        {
            var notificationText = GetNotificationDisplayText(notification);
            var iconType = GetNotificationIcon(notification.Type);
            
            var config = new MessageConfig()
            {
                Content = notificationText,
                Duration = 5,
                Icon = iconType
            };

            switch (notification.Type)
            {
                case NotificationType.AddedToBoard:
                    _messageService.Info(config);
                    break;
                case NotificationType.AssignedToTask:
                    _messageService.Warning(config);
                    break;
                case NotificationType.CommentedOnTask:
                    _messageService.Success(config);
                    break;
                default:
                    _messageService.Info(config);
                    break;
            }
        }

        private string GetNotificationDisplayText(Notification notification)
        {
            const int maxLength = 80;
            var text = notification.Text;
            
            if (text.Length > maxLength)
            {
                text = text.Substring(0, maxLength) + "...";
            }

            return $"🔔 {text}";
        }

        private RenderFragment GetNotificationIcon(NotificationType type)
        {
            return type switch
            {
                NotificationType.AddedToBoard => builder =>
                {
                    builder.OpenComponent<Icon>(0);
                    builder.AddAttribute(1, "Type", "team");
                    builder.CloseComponent();
                },
                NotificationType.AssignedToTask => builder =>
                {
                    builder.OpenComponent<Icon>(0);
                    builder.AddAttribute(1, "Type", "user");
                    builder.CloseComponent();
                },
                NotificationType.CommentedOnTask => builder =>
                {
                    builder.OpenComponent<Icon>(0);
                    builder.AddAttribute(1, "Type", "message");
                    builder.CloseComponent();
                },
                _ => builder =>
                {
                    builder.OpenComponent<Icon>(0);
                    builder.AddAttribute(1, "Type", "notification");
                    builder.CloseComponent();
                }
            };
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
                }
                catch (Exception)
                {
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
                }
                catch (Exception)
                {
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
                }
                catch (Exception)
                {
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
                }
                catch (Exception)
                {
                }
            }
        }
    }
}