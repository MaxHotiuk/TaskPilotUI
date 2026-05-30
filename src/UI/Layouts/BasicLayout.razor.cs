using AntDesign.Extensions.Localization;
using AntDesign.ProLayout;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;
using System.Net.Http.Json;
using UI.Interfaces.Services;
using UI.Interfaces.SignalR;
using UI.Models.Chat;

namespace UI.Layouts
{
    public partial class BasicLayout : LayoutComponentBase, IDisposable
    {
        private MenuDataItem[] _menuData = Array.Empty<MenuDataItem>();
        private bool collapsed;
        private bool showNotification = false;
        private string notificationMessage = "";
        private UI.Models.Notification.NotificationType notificationType = UI.Models.Notification.NotificationType.AddedToBoard;
        private bool showChatNotification = false;
        private string chatNotificationMessage = "";
        private UI.Models.Notification.NotificationType chatNotificationType = UI.Models.Notification.NotificationType.CommentedOnTask;
        private Guid _currentUserId;
        private bool _chatHandlersRegistered;
        private bool _notificationHandlersRegistered;
        private bool _isConnectingSignalR;
        private readonly HashSet<Guid> _joinedChatIds = new();
        private int _invitationsCount = 0;
        private int _unreadNotificationsCount = 0;
        private int _unreadChatsCount = 0;

        [Inject] private ReuseTabsService TabService { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private IChatSignalRService ChatSignalRService { get; set; } = default!;
        [Inject] private IChatSystemService ChatSystemService { get; set; } = default!;
        [Inject] private IInvitationService InvitationService { get; set; } = default!;
        [Inject] private UI.Interfaces.Services.INotificationService NotificationService { get; set; } = default!;
        [Inject] private UI.Services.NotificationCountState NotificationCountState { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        public LinkItem[] Links => Array.Empty<LinkItem>();

        protected override async Task OnInitializedAsync()
        {
            await BuildMenuDataAsync();

            AuthService.OnAuthStateChanged += HandleAuthStateChanged;
            NotificationCountState.OnChange += HandleNotificationCountChanged;

            await EnsureSignalRConnectionsAsync();
        }

        private async Task BuildMenuDataAsync()
        {
            var menuItems = new List<MenuDataItem>
            {
                new MenuDataItem
                {
                    Path = "/",
                    Name = UI.Resources.I18n.BoardsMenu,
                    Key = "boards",
                    Icon = "appstore",
                },
                new MenuDataItem
                {
                    Path = "/profile",
                    Name = UI.Resources.I18n.ProfileMenu,
                    Key = "profile",
                    Icon = "user",
                },
                new MenuDataItem
                {
                    Path = "/ai-assistant",
                    Name = UI.Resources.I18n.AskAIMenu,
                    Key = "aiAssistant",
                    Icon = "robot"
                },
                new MenuDataItem
                {
                    Path = "/chats",
                    Name = _unreadChatsCount > 0 ? $"{UI.Resources.I18n.ChatsMenu} ({_unreadChatsCount})" : UI.Resources.I18n.ChatsMenu,
                    Key = "chats",
                    Icon = "message"
                },
                new MenuDataItem
                {
                    Path = "/notifications",
                    Name = _unreadNotificationsCount > 0 ? $"{UI.Resources.I18n.NotificationsMenu} ({_unreadNotificationsCount})" : UI.Resources.I18n.NotificationsMenu,
                    Key = "notifications",
                    Icon = "bell"
                },
                new MenuDataItem
                {
                    Path = "/invitations",
                    Name = _invitationsCount > 0 ? string.Format(UI.Resources.I18n.InvitationsMenuWithCount, _invitationsCount) : UI.Resources.I18n.InvitationsMenu,
                    Key = "invitations",
                    Icon = "mail"
                },
                new MenuDataItem
                {
                    Path = "/calendar",
                    Name = UI.Resources.I18n.CalendarMenu,
                    Key = "calendar",
                    Icon = "calendar"
                }
            };

            // Add organization management
            var currentUser = await AuthService.GetCurrentUserAsync();
            if (currentUser != null && currentUser.Organizations?.Any() == true)
            {
                var organizationMenuItems = new List<MenuDataItem>();

                foreach (var org in currentUser.Organizations)
                {
                    organizationMenuItems.Add(new MenuDataItem
                    {
                        Path = $"/organization/{org.Id}",
                        Name = org.Name,
                        Key = $"org-{org.Id}",
                        Icon = "team"
                    });
                }

                menuItems.Add(new MenuDataItem
                {
                    Name = UI.Resources.I18n.Organizations,
                    Key = "organizations",
                    Icon = "apartment",
                    Children = organizationMenuItems.ToArray()
                });
            }

            // Add admin menu for admins
            if (currentUser?.Role == "Admin")
            {
                menuItems.Add(new MenuDataItem
                {
                    Name = UI.Resources.I18n.Admin,
                    Key = "admin",
                    Icon = "setting",
                    Children = new[]
                    {
                        new MenuDataItem
                        {
                            Path = "/admin/manager-requests",
                            Name = UI.Resources.I18n.ManagerRequestsMenu,
                            Key = "admin-manager-requests",
                            Icon = "crown"
                        }
                    }
                });
            }

            _menuData = menuItems.ToArray();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (!NotificationSignalRService.IsConnected)
                {
                    try
                    {
                        await EnsureSignalRConnectionsAsync();

                        StateHasChanged();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to connect to SignalR hub or join user group: {ex.Message}");
                    }
                }
            }

            await UpdateNotificationBadgeAsync();
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
                AuthService.OnAuthStateChanged -= HandleAuthStateChanged;
                NotificationCountState.OnChange -= HandleNotificationCountChanged;

                if (_currentUserId != Guid.Empty)
                {
                    await NotificationSignalRService.LeaveUserGroupAsync(_currentUserId.ToString());
                    await ChatSignalRService.LeaveUserGroupAsync(_currentUserId.ToString());
                }

                await NotificationSignalRService.DisconnectAsync();
                await ChatSignalRService.DisconnectAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting from SignalR: {ex.Message}");
            }
        }

        private void HandleAuthStateChanged()
        {
            _ = InvokeAsync(async () =>
            {
                await BuildMenuDataAsync();
                await EnsureSignalRConnectionsAsync();
                StateHasChanged();
            });
        }

        private void HandleNotificationCountChanged()
        {
            _unreadNotificationsCount = NotificationCountState.UnreadCount;
            _ = InvokeAsync(async () =>
            {
                await BuildMenuDataAsync();
                StateHasChanged();
            });
        }

        private async Task EnsureSignalRConnectionsAsync()
        {
            if (_isConnectingSignalR)
                return;

            _isConnectingSignalR = true;
            try
            {
                var currentUser = await AuthService.GetCurrentUserAsync();
                if (currentUser == null || currentUser.Id == Guid.Empty)
                {
                    _currentUserId = Guid.Empty;
                    _chatHandlersRegistered = false;
                    _notificationHandlersRegistered = false;
                    await NotificationSignalRService.DisconnectAsync();
                    await ChatSignalRService.DisconnectAsync();
                    return;
                }

                _currentUserId = currentUser.Id;

                if (!_notificationHandlersRegistered)
                {
                    NotificationSignalRService.OnNotificationReceived(HandleNotification);
                    _notificationHandlersRegistered = true;
                }

                try
                {
                    _unreadNotificationsCount = await NotificationService.GetUnreadCountAsync(_currentUserId);
                    NotificationCountState.SetCount(_unreadNotificationsCount);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load unread notifications count: {ex.Message}");
                }

                try
                {
                    _unreadChatsCount = await ComputeUnreadChatsCountAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load unread chats count: {ex.Message}");
                }

                await BuildMenuDataAsync();
                await InvokeAsync(StateHasChanged);

                if (!NotificationSignalRService.IsConnected)
                {
                    await NotificationSignalRService.ConnectAsync();
                }

                await NotificationSignalRService.JoinUserGroupAsync(_currentUserId.ToString());
                await ConnectChatSignalRAsync();
                await RefreshChatGroupSubscriptionsAsync(currentUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to ensure SignalR connections: {ex.Message}");
            }
            finally
            {
                _isConnectingSignalR = false;
            }
        }

        private async Task ConnectChatSignalRAsync()
        {
            try
            {
                if (_currentUserId == Guid.Empty)
                    return;

                if (!ChatSignalRService.IsConnected)
                {
                    await ChatSignalRService.ConnectAsync();
                    await ChatSignalRService.JoinUserGroupAsync(_currentUserId.ToString());
                    _chatHandlersRegistered = false;
                }

                if (!_chatHandlersRegistered)
                {
                    ChatSignalRService.OnChatMessageReceived(HandleChatMessageNotification);
                    _chatHandlersRegistered = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to chat SignalR: {ex.Message}");
            }
        }

        private async Task RefreshChatGroupSubscriptionsAsync(UI.Models.User.UserDto currentUser)
        {
            if (_currentUserId == Guid.Empty)
                return;

            try
            {
                var organizations = currentUser.Organizations ?? new List<UI.Models.Organization.OrganizationSummaryDto>();
                foreach (var organization in organizations)
                {
                    var chats = await ChatSystemService.GetChatsAsync(_currentUserId, organization.Id);
                    foreach (var chat in chats)
                    {
                        if (_joinedChatIds.Add(chat.Id))
                        {
                            await ChatSignalRService.JoinChatGroupAsync(chat.Id.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to join chat groups: {ex.Message}");
            }
        }

        private void HandleChatMessageNotification(ChatMessageDto message)
        {
            if (_currentUserId == Guid.Empty || message.SenderId == _currentUserId)
                return;

            if (Navigation.Uri.Contains("/chats", StringComparison.OrdinalIgnoreCase))
                return;

            _unreadChatsCount++;

            _ = InvokeAsync(async () =>
            {
                await BuildMenuDataAsync();
                var preview = message.Content.Length > 80 ? message.Content.Substring(0, 80) + "..." : message.Content;
                chatNotificationMessage = $"{message.SenderName}: {preview}";
                showChatNotification = true;
                StateHasChanged();

                _ = Task.Delay(5000).ContinueWith(async _ =>
                {
                    showChatNotification = false;
                    await InvokeAsync(StateHasChanged);
                });
            });
        }

        private void HandleNotification(object notificationObj)
        {
            if (notificationObj is UI.Models.Notification.Notification notification)
            {
                notificationMessage = notification.Text;
                notificationType = notification.Type;
                showNotification = true;
                _unreadNotificationsCount++;
                NotificationCountState.SetCount(_unreadNotificationsCount);

                _ = InvokeAsync(async () =>
                {
                    await BuildMenuDataAsync();
                    StateHasChanged();
                });

                _ = Task.Delay(5000).ContinueWith(async t =>
                {
                    showNotification = false;
                    await InvokeAsync(StateHasChanged);
                });
            }
        }

        private async Task UpdateNotificationBadgeAsync()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("notificationHelpers.updateBadge", _unreadNotificationsCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update notification badge: {ex.Message}");
            }
        }

        private void HideNotification()
        {
            showNotification = false;
            StateHasChanged();
        }

        private void HideChatNotification()
        {
            showChatNotification = false;
            StateHasChanged();
        }

        private async Task LoadInvitationsCountAsync()
        {
            try
            {
                var currentUser = await AuthService.GetCurrentUserAsync();
                if (currentUser == null) return;

                var invitations = await InvitationService.GetPendingInvitationsAsync();
                _invitationsCount = invitations.BoardInvitations.Count + invitations.OrganizationInvitations.Count;
                await BuildMenuDataAsync();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load invitations count: {ex.Message}");
            }
        }

        private async Task<int> ComputeUnreadChatsCountAsync()
        {
            var currentUser = await AuthService.GetCurrentUserAsync();
            if (currentUser == null || _currentUserId == Guid.Empty)
                return 0;

            var organizations = currentUser.Organizations ?? new List<UI.Models.Organization.OrganizationSummaryDto>();
            int count = 0;

            foreach (var organization in organizations)
            {
                var chats = await ChatSystemService.GetChatsAsync(_currentUserId, organization.Id);
                foreach (var chat in chats)
                {
                    if (chat.LastMessage == null || chat.LastMessage.SenderId == _currentUserId)
                        continue;

                    var member = chat.Members.FirstOrDefault(m => m.UserId == _currentUserId);
                    if (member == null || member.LastReadAt == null || chat.LastMessage.CreatedAt > member.LastReadAt)
                        count++;
                }
            }

            return count;
        }
    }
}