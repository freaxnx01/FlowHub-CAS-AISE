using System.Collections.Concurrent;

namespace NotificationService.Notifications;

public class NotificationStore
{
    private readonly ConcurrentBag<Notification> _notifications = [];

    public void Add(Notification notification) =>
        _notifications.Add(notification);

    public IReadOnlyList<Notification> GetAll() =>
        _notifications.ToList();
}
