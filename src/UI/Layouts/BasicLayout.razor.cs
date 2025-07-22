using AntDesign.Extensions.Localization;
using AntDesign.ProLayout;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using System.Net.Http.Json;
using UI.Interfaces.Services;

namespace UI.Layouts
{
    public partial class BasicLayout : LayoutComponentBase, IDisposable
    {
        private MenuDataItem[] _menuData = Array.Empty<MenuDataItem>();
        private bool collapsed;
        private bool showNotification = false;
        private string notificationMessage = "";
        private UI.Models.Notification.NotificationType notificationType = UI.Models.Notification.NotificationType.AddedToBoard;

        [Inject] private ReuseTabsService TabService { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;

        public LinkItem[] Links => Array.Empty<LinkItem>();

        protected override async Task OnInitializedAsync()
        {
            _menuData = new[] {
                new MenuDataItem
                {
                    Path = "/",
                    Name = "Boards",
                    Key = "boards",
                    Icon = "appstore",
                },
                new MenuDataItem
                {
                    Path = "/profile",
                    Name = "Profile",
                    Key = "profile",
                    Icon = "user",
                },
                new MenuDataItem
                {
                    Path = "/ai-assistant",
                    Name = "Ask AI",
                    Key = "aiAssistant",
                    Icon = "robot"
                },
                new MenuDataItem
                {
                    Path = "/notifications",
                    Name = "Notifications",
                    Key = "notifications",
                    Icon = "bell"
                },
                new MenuDataItem
                {
                    Path = "/calendar",
                    Name = "Calendar",
                    Key = "calendar",
                    Icon = "calendar"
                }
            };

            NotificationSignalRService.OnNotificationReceived(HandleNotification);
            
            try
            {
                await NotificationSignalRService.ConnectAsync();
                
                var currentUser = await AuthService.GetCurrentUserAsync();
                if (currentUser != null && !string.IsNullOrWhiteSpace(currentUser.Id))
                {
                    await NotificationSignalRService.JoinUserGroupAsync(currentUser.Id);
                }
            }
            catch (Exception)
            {
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (!NotificationSignalRService.IsConnected)
                {
                    try
                    {
                        await NotificationSignalRService.ConnectAsync();
                        
                        var currentUser = await AuthService.GetCurrentUserAsync();
                        if (currentUser != null && !string.IsNullOrWhiteSpace(currentUser.Id))
                        {
                            await NotificationSignalRService.JoinUserGroupAsync(currentUser.Id);
                        }
                        
                        StateHasChanged();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to connect to SignalR hub or join user group: {ex.Message}");
                    }
                }
            }
        }

        void Toggle()
        {
            collapsed = !collapsed;
        }

        void Reload()
        {
            TabService.ReloadPage();
        }

        public async void Dispose()
        {
            try
            {
                var currentUser = await AuthService.GetCurrentUserAsync();
                if (currentUser != null && !string.IsNullOrWhiteSpace(currentUser.Id))
                {
                    await NotificationSignalRService.LeaveUserGroupAsync(currentUser.Id);
                }
                
                await NotificationSignalRService.DisconnectAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting from SignalR: {ex.Message}");
            }
        }

        private void HandleNotification(object notificationObj)
        {
            if (notificationObj is UI.Models.Notification.Notification notification)
            {
                notificationMessage = notification.Text;
                notificationType = notification.Type;
                showNotification = true;
                
                InvokeAsync(StateHasChanged);
                
                _ = Task.Delay(5000).ContinueWith(async t =>
                {
                    showNotification = false;
                    await InvokeAsync(StateHasChanged);
                });
            }
        }

        private void HideNotification()
        {
            showNotification = false;
            StateHasChanged();
        }
    }
}