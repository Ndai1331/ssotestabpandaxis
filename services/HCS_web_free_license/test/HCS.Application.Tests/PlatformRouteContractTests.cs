using HCS.Controllers.Auditing;
using HCS.Controllers.Localization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Xunit;

namespace HCS;

public sealed class PlatformRouteContractTests
{
    [Theory]
    [InlineData(typeof(LanguagesController), "api/hcs/languages", "api/language-management/languages")]
    [InlineData(typeof(LanguageTextsController), "api/hcs/language-texts", "api/language-management/language-texts")]
    [InlineData(typeof(AuditViewerController), "api/hcs/audit-logs", "api/audit-logs")]
    public void Platform_controllers_preserve_legacy_and_gateway_routes(Type controller,
        string legacyRoute, string gatewayRoute)
    {
        var routes = controller.GetCustomAttributes(typeof(RouteAttribute), true)
            .Cast<RouteAttribute>().Select(x => x.Template).ToArray();

        Assert.Contains(legacyRoute, routes);
        Assert.Contains(gatewayRoute, routes);
    }
}
