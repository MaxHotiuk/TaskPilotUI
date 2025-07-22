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
    private Guid currentUserId;

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
                    await LoadNotificationsAsync();
                    StateHasChanged();
                });
            });
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to connect to notification service: {ex.Message}");
        }
    }

    private async Task LoadNotificationsAsync()
    {
        try
        {
            var result = await NotificationService.GetByUserIdAsync(currentUserId);
            notifications = result.OrderByDescending(n => n.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to load notifications: {ex.Message}");
        }
    }

    private async Task RefreshNotificationsAsync()
    {
        isLoading = true;
        StateHasChanged();
        
        await LoadNotificationsAsync();
        
        isLoading = false;
        StateHasChanged();
        
        MessageService.Success("Notifications refreshed");
    }

    private void LoadMoreNotificationsAsync()
    {
        isLoadingMore = true;
        StateHasChanged();
        
        // Implement pagination logic here if your API supports it
        // For now, this is a placeholder
        
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
            
            MessageService.Success("Notification marked as read");
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
            
            MessageService.Success("Notification deleted");
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to delete notification: {ex.Message}");
        }
    }

    private void NavigateToItem(Notification notification)
    {
        if (notification.TaskId.HasValue)
        {
            Navigation.NavigateTo($"/tasks/{notification.TaskId}");
        }
        else if (notification.BoardId.HasValue)
        {
            Navigation.NavigateTo($"/boards/{notification.BoardId}");
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

    private RenderFragment[] GetNotificationActions(Notification notification)
    {
        var actions = new List<RenderFragment>();

        if (!notification.IsRead)
        {
            actions.Add(builder =>
            {
                builder.OpenComponent<Button>(0);
                builder.AddAttribute(1, "Size", ButtonSize.Small);
                builder.AddAttribute(2, "Type", ButtonType.Link);
                builder.AddAttribute(3, "OnClick", EventCallback.Factory.Create(this, () => MarkAsReadAsync(notification.Id)));
                builder.AddAttribute(4, "ChildContent", (RenderFragment)((builder2) => 
                {
                    builder2.AddContent(5, "Mark as read");
                }));
                builder.CloseComponent();
            });
        }

        if (notification.BoardId.HasValue || notification.TaskId.HasValue)
        {
            actions.Add(builder =>
            {
                builder.OpenComponent<Button>(0);
                builder.AddAttribute(1, "Size", ButtonSize.Small);
                builder.AddAttribute(2, "Type", ButtonType.Link);
                builder.AddAttribute(3, "OnClick", EventCallback.Factory.Create(this, () => NavigateToItem(notification)));
                builder.AddAttribute(4, "ChildContent", (RenderFragment)((builder2) => 
                {
                    builder2.AddContent(5, "View");
                }));
                builder.CloseComponent();
            });
        }

        actions.Add(builder =>
        {
            builder.OpenComponent<Popconfirm>(0);
            builder.AddAttribute(1, "Title", "Are you sure you want to delete this notification?");
            builder.AddAttribute(2, "OnConfirm", EventCallback.Factory.Create(this, () => DeleteAsync(notification.Id)));
            builder.AddAttribute(3, "OkText", "Yes");
            builder.AddAttribute(4, "CancelText", "No");
            builder.AddAttribute(5, "ChildContent", (RenderFragment)((builder2) => 
            {
                builder2.OpenComponent<Button>(6);
                builder2.AddAttribute(7, "Size", ButtonSize.Small);
                builder2.AddAttribute(8, "Type", ButtonType.Link);
                builder2.AddAttribute(9, "Danger", true);
                builder2.AddAttribute(10, "Icon", "delete");
                builder2.AddAttribute(11, "ChildContent", (RenderFragment)((builder3) => 
                {
                    builder3.AddContent(12, "Delete");
                }));
                builder2.CloseComponent();
            }));
            builder.CloseComponent();
        });

        return actions.ToArray();
    }
}
