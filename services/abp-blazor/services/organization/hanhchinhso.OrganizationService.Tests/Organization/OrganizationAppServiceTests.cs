using hanhchinhso.OrganizationService.Organization;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace hanhchinhso.OrganizationService.Tests.Organization;

public class OrganizationAppServiceTests : OrganizationServiceIntegrationTestBase
{
    [Fact]
    public async Task Should_CRUD_And_Filter_Organization_Aggregates()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var units = ServiceProvider.GetRequiredService<IUnitAppService>();
            var positions = ServiceProvider.GetRequiredService<IPositionAppService>();
            var unit = await units.CreateAsync(new CreateUpdateUnitDto
            {
                Code = "UBND", Name = "Ủy ban nhân dân", SortOrder = 1
            });
            var position = await positions.CreateAsync(new CreateUpdatePositionDto
            {
                Code = "DIRECTOR", Name = "Giám đốc", SignOrder = 1
            });
            (await units.GetAsync(unit.Id)).SortOrder.ShouldBe(1);
            (await positions.GetAsync(position.Id)).SignOrder.ShouldBe(1);
        });
    }

    [Fact]
    public async Task Should_Isolate_Units_By_Tenant()
    {
        var currentTenant = ServiceProvider.GetRequiredService<ICurrentTenant>();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        using (currentTenant.Change(tenantA))
        {
            await WithUnitOfWorkAsync(async () =>
                await ServiceProvider.GetRequiredService<IUnitAppService>().CreateAsync(
                    new CreateUpdateUnitDto { Code = "VP", Name = "Văn phòng A" }));
        }

        using (currentTenant.Change(tenantB))
        {
            await WithUnitOfWorkAsync(async () =>
            {
                var service = ServiceProvider.GetRequiredService<IUnitAppService>();
                await service.CreateAsync(new CreateUpdateUnitDto { Code = "VP", Name = "Văn phòng B" });
                var result = await service.GetListAsync(new OrganizationListInput());
                result.TotalCount.ShouldBe(1);
                result.Items.Single().Name.ShouldBe("Văn phòng B");
            });
        }
    }
}
