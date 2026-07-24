using hanhchinhso.OrganizationService.MasterData;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace hanhchinhso.OrganizationService.Tests.MasterData;

public class MasterDataAppServiceTests : OrganizationServiceIntegrationTestBase
{
    [Fact]
    public async Task Should_Create_And_Read_Master_Data()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var service = ServiceProvider.GetRequiredService<IMasterDataAppService>();
            var created = await service.CreateAsync(new CreateUpdateMasterDataItemDto
            {
                Type = "Unit",
                Code = "UNIT-001",
                Name = "Đơn vị mẫu",
                SortOrder = 10
            });

            created.Id.ShouldNotBe(Guid.Empty);
            created.Code.ShouldBe("UNIT-001");
            created.SortOrder.ShouldBe(10);

            var result = await service.GetListAsync(new MasterDataListInput());
            result.TotalCount.ShouldBe(1);
            result.Items.Single().Name.ShouldBe("Đơn vị mẫu");
        });
    }

    [Fact]
    public async Task Should_Isolate_Master_Data_By_Tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var currentTenant = ServiceProvider.GetRequiredService<ICurrentTenant>();

        using (currentTenant.Change(tenantA))
        {
            await WithUnitOfWorkAsync(async () =>
            {
                var service = ServiceProvider.GetRequiredService<IMasterDataAppService>();
                await service.CreateAsync(new CreateUpdateMasterDataItemDto
                {
                    Type = "Position",
                    Code = "DIRECTOR",
                    Name = "Giám đốc A"
                });
            });
        }

        using (currentTenant.Change(tenantB))
        {
            await WithUnitOfWorkAsync(async () =>
            {
                var service = ServiceProvider.GetRequiredService<IMasterDataAppService>();
                await service.CreateAsync(new CreateUpdateMasterDataItemDto
                {
                    Type = "Position",
                    Code = "DIRECTOR",
                    Name = "Giám đốc B"
                });

                var result = await service.GetListAsync(new MasterDataListInput());
                result.TotalCount.ShouldBe(1);
                result.Items.Single().Name.ShouldBe("Giám đốc B");
            });
        }
    }
}
