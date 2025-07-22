using Refit;
using Notification = UI.Models.Notification.Notification;
namespace UI.Interfaces.Api;

public interface INotificationApi
{
    [Get("/api/notifications/{userId}")]
    Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    [Delete("/api/notifications/{id}")]
    Task DeleteNotificationAsync(Guid id, CancellationToken cancellationToken = default);

    [Get("/api/notifications/unread-count/{userId}")]
    Task<int> GetUnreadNotificationsCountAsync(Guid userId, CancellationToken cancellationToken = default);

    [Post("/api/notifications/mark-all-read/{userId}")]
    Task MarkAllNotificationsAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    [Post("/api/notifications/{id}/mark-read")]
    Task MarkNotificationAsReadAsync(Guid id, CancellationToken cancellationToken = default);
}