using System.Security.Claims;
using hanhchinhso.IdentityService.Internal;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Shouldly;
using Volo.Abp.Authorization;
using Volo.Abp.Security.Claims;
using Volo.Abp.Identity;
using Xunit;

namespace hanhchinhso.IdentityService.Tests.ApplicationServices;

public class IdentityReferenceValidationAppServiceTests :
    IdentityServiceIntegrationTestBase
{
    [Fact]
    public async Task Should_Reject_Non_Document_Service_Client()
    {
        var principalAccessor =
            GetRequiredService<ICurrentPrincipalAccessor>();
        using (principalAccessor.Change(CreatePrincipal("BlazorWebApp")))
        {
            var service =
                GetRequiredService<IIdentityReferenceValidationAppService>();
            await Should.ThrowAsync<AbpAuthorizationException>(() =>
                service.ValidateAsync(new IdentityReferenceValidationRequest()));
        }
    }

    [Fact]
    public async Task Should_Batch_Validate_And_Report_Missing_References()
    {
        var userId = Guid.NewGuid();
        var organizationUnitId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var principalAccessor =
            GetRequiredService<ICurrentPrincipalAccessor>();

        using (principalAccessor.Change(
                   CreatePrincipal("DocumentService.Internal")))
        {
            var service =
                GetRequiredService<IIdentityReferenceValidationAppService>();
            var result = await service.ValidateAsync(
                new IdentityReferenceValidationRequest
                {
                    UserIds = [Guid.Empty, userId, userId],
                    OrganizationUnitIds = [organizationUnitId],
                    RoleIds = [roleId]
                });

            result.MissingOrInactiveUserIds.ShouldBe([userId]);
            result.MissingOrganizationUnitIds.ShouldBe([organizationUnitId]);
            result.MissingRoleIds.ShouldBe([roleId]);
        }
    }

    [Fact]
    public async Task Should_Resolve_Only_Requested_User_Ou_Memberships()
    {
        var userId = Guid.NewGuid();
        var matchingOuId = Guid.NewGuid();
        var otherOuId = Guid.NewGuid();
        await WithUnitOfWorkAsync(async () =>
        {
            var organizationUnitManager =
                GetRequiredService<OrganizationUnitManager>();
            var organizationUnitRepository =
                GetRequiredService<IOrganizationUnitRepository>();
            var userManager = GetRequiredService<IdentityUserManager>();
            var user = new IdentityUser(
                userId,
                $"user-{userId:N}",
                $"{userId:N}@test.local");
            await userManager.CreateAsync(user);
            var matchingOu = new OrganizationUnit(
                matchingOuId,
                "Matching OU");
            await organizationUnitManager.CreateAsync(matchingOu);
            await organizationUnitRepository.InsertAsync(
                matchingOu,
                autoSave: true);
            var otherOu = new OrganizationUnit(
                otherOuId,
                "Other OU");
            await organizationUnitManager.CreateAsync(otherOu);
            await organizationUnitRepository.InsertAsync(
                otherOu,
                autoSave: true);
            await userManager.AddToOrganizationUnitAsync(
                userId,
                matchingOuId);
        });

        var principalAccessor =
            GetRequiredService<ICurrentPrincipalAccessor>();
        using (principalAccessor.Change(
                   CreatePrincipal("DocumentService.Internal")))
        {
            var result = await GetRequiredService<
                    IIdentityReferenceValidationAppService>()
                .ResolveUserOrganizationUnitMembershipsAsync(new()
                {
                    UserId = userId,
                    OrganizationUnitIds =
                        [matchingOuId, matchingOuId, otherOuId, Guid.Empty]
                });
            result.OrganizationUnitIds.ShouldBe([matchingOuId]);
        }
    }

    [Fact]
    public async Task Should_Reject_Membership_Resolution_From_Foreign_Client()
    {
        using (GetRequiredService<ICurrentPrincipalAccessor>()
               .Change(CreatePrincipal("BlazorWebApp")))
        {
            await Should.ThrowAsync<AbpAuthorizationException>(() =>
                GetRequiredService<IIdentityReferenceValidationAppService>()
                    .ResolveUserOrganizationUnitMembershipsAsync(new()
                    {
                        UserId = Guid.NewGuid(),
                        OrganizationUnitIds = [Guid.NewGuid()]
                    }));
        }
    }

    private static ClaimsPrincipal CreatePrincipal(string clientId) =>
        new(new ClaimsIdentity(
            [
                new Claim(OpenIddictConstants.Claims.ClientId, clientId)
            ],
            "Test"));
}
