using Notification = UI.Models.Notification.Notification;

namespace UI.Interfaces.Services;

public interface INotificationService
{
    Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task DeleteNotificationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetUnreadNotificationsCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task MarkAllNotificationsAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task MarkNotificationAsReadAsync(Guid id, CancellationToken cancellationToken = default);
}