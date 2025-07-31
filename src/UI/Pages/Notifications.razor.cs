using Microsoft.AspNetCore.Components;
using UI.Models.User;
using UI.Interfaces.Services;
using UI.Extensions;
using Notification = UI.Models.Notification.Notification;
using NotificationType = UI.Models.Notification.NotificationType;

namespace UI.Pages;

public partial class Notifications : ComponentBase
{
    private List<Notification> notifications = new();
    private bool isLoading = true;
    private bool isLoadingMore = false;
    private bool hasMoreNotifications = true;
    private Guid currentUserId;
    private int currentPage = 1;
    private const int pageSize = 7;

    protected override async Task OnInitializedAsync()
    {
        await GetCurrentUserIdAsync();
        
        if (currentUserId != Guid.Empty)
        {
            await ConnectToSignalRAsync();
            await LoadNotificationsAsync();
        }
        
        isLoading = false;
    }

    private async Task GetCurrentUserIdAsync()
    {
        var user = await AuthService.GetCurrentUserAsync();
        if (user != null && Guid.TryParse(user.Id?.ToString(), out var parsedId) && parsedId != Guid.Empty)
        {
            currentUserId = parsedId;
            return;
        }
    }

    private async Task ConnectToSignalRAsync()
    {
        try
        {
            if (!NotificationSignalRService.IsConnected)
            {
                await NotificationSignalRService.ConnectAsync();
                await NotificationSignalRService.JoinUserGroupAsync(currentUserId.ToString());
            }

            NotificationSignalRService.OnNotificationReceived(async (notification) =>
            {
                await InvokeAsync(async () =>
                {
                    currentPage = 1;
                    hasMoreNotifications = true;
                    await LoadNotificationsAsync(isRefresh: true);
                    StateHasChanged();
                });
            });
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to connect to notification service: {ex.Message}");
        }
    }

    private async Task LoadNotificationsAsync(bool isRefresh = false)
    {
        try
        {
            var result = await NotificationService.GetByUserIdWithRangeAsync(currentUserId, currentPage, pageSize);
            var notificationsList = result.OrderByDescending(n => n.CreatedAt).ToList();
            
            if (isRefresh || currentPage == 1)
            {
                notifications = notificationsList;
            }
            else
            {
                notifications.AddRange(notificationsList);
            }
            
            hasMoreNotifications = notificationsList.Count == pageSize;
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to load notifications: {ex.Message}");
        }
    }

    private async Task RefreshNotificationsAsync()
    {
        isLoading = true;
        currentPage = 1;
        hasMoreNotifications = true;
        StateHasChanged();
        
        await LoadNotificationsAsync(isRefresh: true);
        
        isLoading = false;
        StateHasChanged();
        
        MessageService.Success("Notifications refreshed");
    }

    private async Task LoadMoreNotificationsAsync()
    {
        if (!hasMoreNotifications || isLoadingMore)
            return;
            
        isLoadingMore = true;
        StateHasChanged();
        
        currentPage++;
        await LoadNotificationsAsync();
        
        isLoadingMore = false;
        StateHasChanged();
    }

    private async Task MarkAsReadAsync(Guid notificationId)
    {
        try
        {
            await NotificationService.MarkAsReadAsync(notificationId);
            
            var notification = notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to mark notification as read: {ex.Message}");
        }
    }

    private async Task MarkAllAsReadAsync()
    {
        try
        {
            await NotificationService.MarkAllAsReadAsync(currentUserId);
            
            notifications.ForEach(n => n.IsRead = true);
            StateHasChanged();
            
            MessageService.Success("All notifications marked as read");
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to mark all notifications as read: {ex.Message}");
        }
    }

    private async Task DeleteAsync(Guid notificationId)
    {
        try
        {
            await NotificationService.DeleteAsync(notificationId);

            notifications.RemoveAll(n => n.Id == notificationId);
            StateHasChanged();
            await RefreshNotificationsAsync();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to delete notification: {ex.Message}");
        }
    }

    private void NavigateToItem(Notification notification)
    {
        if (notification.BoardId.HasValue)
        {
            Navigation.NavigateTo($"/board/{notification.BoardId}");
            MarkAsReadAsync(notification.Id).ConfigureAwait(false);
        }
    }

    private string GetNotificationIcon(NotificationType type)
    {
        return type switch
        {
            NotificationType.AddedToBoard => "team",
            NotificationType.AssignedToTask => "file-text",
            NotificationType.CommentedOnTask => "message",
            _ => "bell"
        };
    }

    private string GetNotificationColor(NotificationType type)
    {
        return type switch
        {
            NotificationType.AddedToBoard => "#52c41a",
            NotificationType.AssignedToTask => "#1890ff",
            NotificationType.CommentedOnTask => "#fa8c16",
            _ => "#d9d9d9"
        };
    }

    private string GetNotificationTitle(NotificationType type)
    {
        return type switch
        {
            NotificationType.AddedToBoard => "Added to Board",
            NotificationType.AssignedToTask => "Task Assignment",
            NotificationType.CommentedOnTask => "New Comment",
            _ => "Notification"
        };
    }

    public void Dispose()
    {
        if (currentUserId != Guid.Empty)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await NotificationSignalRService.LeaveUserGroupAsync(currentUserId.ToString());
                    await NotificationSignalRService.DisconnectAsync();
                }
                catch
                {
                }
            });
        }
    }
}