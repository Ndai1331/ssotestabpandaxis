using HCS.CollaborationService.Api;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace HCS.CollaborationService.Tests;

public sealed class ApiContractTests
{
    [Fact]
    public void Chat_and_notification_routes_keep_gateway_contracts()
    {
        typeof(ChatController).GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single().Template.ShouldBe("api/chat");
        typeof(NotificationController).GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single().Template.ShouldBe("api/notifications");
    }
}
