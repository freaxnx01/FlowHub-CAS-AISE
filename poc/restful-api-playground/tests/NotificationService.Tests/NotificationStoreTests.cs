using FluentAssertions;
using NotificationService.Notifications;
using Xunit;

namespace NotificationService.Tests;

public class NotificationStoreTests
{
    [Fact]
    public void Add_Notification_IsReturnedByGetAll()
    {
        var store = new NotificationStore();
        var notification = new Notification(Guid.NewGuid(), "Hello", DateTime.UtcNow);

        store.Add(notification);

        store.GetAll().Should().ContainSingle().Which.Should().Be(notification);
    }

    [Fact]
    public void GetAll_Empty_ReturnsEmptyList()
    {
        var store = new NotificationStore();

        store.GetAll().Should().BeEmpty();
    }
}
