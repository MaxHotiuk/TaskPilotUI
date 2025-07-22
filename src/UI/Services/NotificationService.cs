using UI.Interfaces.Api;
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

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _notificationApi.GetByUserIdAsync(userId, cancellationToken);
    }

    public async Task<IEnumerable<Notification>> GetByUserIdWithRangeAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _notificationApi.GetByUserIdWithRangeAsync(userId, page, pageSize, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _notificationApi.DeleteAsync(id, cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _notificationApi.GetUnreadCountAsync(userId, cancellationToken);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _notificationApi.MarkAllAsReadAsync(userId, cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _notificationApi.MarkAsReadAsync(id, cancellationToken);
    }
}