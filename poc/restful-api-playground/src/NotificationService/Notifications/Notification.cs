namespace NotificationService.Notifications;

public record Notification(Guid OrderId, string Message, DateTime CreatedAt);
