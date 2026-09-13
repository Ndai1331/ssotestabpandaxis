using HCS.OrganizationService.Contracts;
using HCS.OrganizationService.Host.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Security.Claims;

namespace HCS.OrganizationService.Tests;

public sealed class OrganizationApiContractTests
{
    public static TheoryData<Type, string, string> LookupRoutes => new()
    {
        { typeof(DepartmentsController), "api/organization/departments", OrganizationPermissions.Departments },
        { typeof(UnitsController), "api/organization/units", OrganizationPermissions.Units },
        { typeof(PositionsController), "api/organization/positions", OrganizationPermissions.Positions },
        { typeof(MasterDataController), "api/organization/master-data", OrganizationPermissions.MasterData }
    };

    public static TheoryData<Type, string, string> Routes => new()
    {
        { typeof(Icd10Controller), "api/organization/icd10", OrganizationPermissions.Icd10 },
        { typeof(BloodPressureController), "api/organization/blood-pressure", OrganizationPermissions.BloodPressure },
        { typeof(BloodGlucoseController), "api/organization/blood-glucose", OrganizationPermissions.BloodGlucose },
        { typeof(BmiController), "api/organization/bmi", OrganizationPermissions.Bmi },
        { typeof(CountriesController), "api/organization/countries", OrganizationPermissions.Countries },
        { typeof(ProvincesController), "api/organization/provinces", OrganizationPermissions.Provinces },
        { typeof(CommunesController), "api/organization/communes", OrganizationPermissions.Communes },
        { typeof(UserMappingsController), "api/organization/user-mappings", OrganizationPermissions.UserMappings }
    };

    [Fact]
    public void User_department_lookup_is_read_only_and_permission_scoped()
    {
        var controller = typeof(UserDepartmentLookupController);
        controller.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single().Template
            .ShouldBe("api/organization/user-departments");
        controller.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Policy
            .ShouldBeNull();
        controller.GetMethods().Count(x => x.GetCustomAttributes(typeof(HttpGetAttribute), true).Length > 0).ShouldBe(2);
        controller.GetMethods().Count(x => x.GetCustomAttributes(typeof(HttpPostAttribute), true).Length > 0).ShouldBe(0);
    }

    [Theory]
    [MemberData(nameof(LookupRoutes))]
    public void Lookup_controllers_allow_authenticated_reads_and_keep_mutation_permissions(Type controller, string route, string permission)
    {
        controller.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single().Template.ShouldBe(route);
        controller.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Policy.ShouldBeNull();
        controller.GetCustomAttributes(typeof(IgnoreAntiforgeryTokenAttribute), true).Count().ShouldBe(1);
        controller.GetMethods().Count(x => x.GetCustomAttributes(typeof(HttpGetAttribute), true).Length > 0).ShouldBe(1);
        MutationPolicy(controller, typeof(HttpPostAttribute)).ShouldBe(permission);
        MutationPolicy(controller, typeof(HttpPutAttribute)).ShouldBe(permission);
        MutationPolicy(controller, typeof(HttpDeleteAttribute)).ShouldBe(permission);
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public void Controllers_keep_gateway_routes_and_permission_boundaries(Type controller, string route, string permission)
    {
        controller.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single().Template.ShouldBe(route);
        controller.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Policy.ShouldBe(permission);
        controller.GetCustomAttributes(typeof(IgnoreAntiforgeryTokenAttribute), true).Count().ShouldBe(1);
        controller.GetMethods().Count(x => x.GetCustomAttributes(typeof(HttpGetAttribute), true).Length > 0).ShouldBe(1);
        controller.GetMethods().Count(x => x.GetCustomAttributes(typeof(HttpPostAttribute), true).Length > 0).ShouldBe(1);
        controller.GetMethods().Count(x => x.GetCustomAttributes(typeof(HttpPutAttribute), true).Length > 0).ShouldBe(1);
        controller.GetMethods().Count(x => x.GetCustomAttributes(typeof(HttpDeleteAttribute), true).Length > 0).ShouldBe(1);
    }

    [Fact]
    public void Document_viewers_can_read_catalog_lookups()
    {
        var viewer = Principal(new Claim("permission", "Documents.View"));
        OrganizationLookupAccess.CanRead(viewer).ShouldBeTrue();

        var staff = Principal(new Claim(ClaimTypes.Role, "nhanvien"));
        OrganizationLookupAccess.CanRead(staff).ShouldBeTrue();

        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        OrganizationLookupAccess.CanRead(anonymous).ShouldBeFalse();
    }

    private static string MutationPolicy(Type controller, Type verbAttribute)
    {
        var method = controller.GetMethods().Single(x => x.GetCustomAttributes(verbAttribute, true).Length > 0);
        return method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Policy!;
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "test"));
}
