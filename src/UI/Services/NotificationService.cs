using UI.Interfaces.Api;
using UI.Interfaces.Services;
using INotificationService = UI.Interfaces.Services.INotificationService;
using Notification = UI.Models.Notification.Notification;

namespace UI.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationApi _notificationApi;

    public NotificationService(INotificationApi notificationApi)
    {
        _notificationApi = notificationApi;
    }

    public async Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _notificationApi.GetNotificationsByUserIdAsync(userId, cancellationToken);
    }

    public async Task DeleteNotificationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _notificationApi.DeleteNotificationAsync(id, cancellationToken);
    }

    public async Task<int> GetUnreadNotificationsCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _notificationApi.GetUnreadNotificationsCountAsync(userId, cancellationToken);
    }

    public async Task MarkAllNotificationsAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _notificationApi.MarkAllNotificationsAsReadAsync(userId, cancellationToken);
    }

    public async Task MarkNotificationAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _notificationApi.MarkNotificationAsReadAsync(id, cancellationToken);
    }
}