using Refit;
using Notification = UI.Models.Notification.Notification;
namespace UI.Interfaces.Api;

public interface INotificationApi
{
    [Get("/api/notifications/{userId}")]
    Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    [Delete("/api/notifications/{id}")]
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    [Get("/api/notifications/unread-count/{userId}")]
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    [Post("/api/notifications/mark-all-read/{userId}")]
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    [Post("/api/notifications/{id}/mark-read")]
    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);
}