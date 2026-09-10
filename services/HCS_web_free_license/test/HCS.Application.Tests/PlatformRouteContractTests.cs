using HCS.Auditing;
using HCS.Controllers.Auditing;
using HCS.Controllers.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Reflection;
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

    [Fact]
    public void Audit_list_input_is_bound_from_query_only()
    {
        var parameter = typeof(AuditViewerController)
            .GetMethod(nameof(AuditViewerController.GetListAsync))!
            .GetParameters()
            .Single(parameter => parameter.ParameterType == typeof(GetAuditLogsInput));

        Assert.NotNull(parameter.GetCustomAttribute<FromQueryAttribute>());
    }

    [Fact]
    public void Language_lookup_is_an_anonymous_get_action()
    {
        var method = typeof(LanguagesController).GetMethod(nameof(LanguagesController.GetEnabledListAsync))!;

        Assert.Contains(method.GetCustomAttributes(typeof(HttpGetAttribute), true)
            .Cast<HttpGetAttribute>(), attribute => attribute.Template == "enabled");
        Assert.NotNull(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void Application_services_are_proxyable_by_abp_interceptors()
    {
        var sealedApplicationServices = typeof(HCSApplicationModule).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.IsSealed)
            .Where(type => typeof(HCSAppService).IsAssignableFrom(type))
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(sealedApplicationServices);
    }
}
