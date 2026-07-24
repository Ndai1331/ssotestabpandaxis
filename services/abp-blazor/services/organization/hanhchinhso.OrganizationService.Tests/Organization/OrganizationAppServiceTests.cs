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
            var departments = ServiceProvider.GetRequiredService<IDepartmentAppService>();
            var units = ServiceProvider.GetRequiredService<IUnitAppService>();
            var positions = ServiceProvider.GetRequiredService<IPositionAppService>();
            var memberships = ServiceProvider.GetRequiredService<IUserDepartmentAppService>();

            var department = await departments.CreateAsync(new CreateUpdateDepartmentDto
            {
                Code = "CNTT", Name = "Phòng Công nghệ thông tin", Level = 1, SortOrder = 2
            });
            var unit = await units.CreateAsync(new CreateUpdateUnitDto
            {
                Code = "UBND", Name = "Ủy ban nhân dân", SortOrder = 1
            });
            var position = await positions.CreateAsync(new CreateUpdatePositionDto
            {
                Code = "DIRECTOR", Name = "Giám đốc", SignOrder = 1
            });
            var userId = Guid.NewGuid();
            var membership = await memberships.CreateAsync(new CreateUpdateUserDepartmentDto
            {
                DepartmentId = department.Id, UserId = userId, IsPrimary = true
            });
            var secondaryDepartment = await departments.CreateAsync(new CreateUpdateDepartmentDto
            {
                Code = "VP", Name = "Văn phòng", Level = 1, SortOrder = 1
            });
            await memberships.CreateAsync(new CreateUpdateUserDepartmentDto
            {
                DepartmentId = secondaryDepartment.Id, UserId = userId, IsPrimary = true
            });

            (await departments.GetListAsync(new DepartmentListInput { FilterText = "Công nghệ" }))
                .TotalCount.ShouldBe(1);
            (await units.GetAsync(unit.Id)).SortOrder.ShouldBe(1);
            (await positions.GetAsync(position.Id)).SignOrder.ShouldBe(1);
            var userMemberships = await memberships.GetListAsync(new UserDepartmentListInput { UserId = userId });
            userMemberships.TotalCount.ShouldBe(2);
            userMemberships.Items.Count(x => x.IsPrimary).ShouldBe(1);
            userMemberships.Items.Single(x => x.Id == membership.Id).IsPrimary.ShouldBeFalse();

            await departments.UpdateAsync(department.Id, new CreateUpdateDepartmentDto
            {
                Code = department.Code, Name = "Phòng Chuyển đổi số", Level = 1, SortOrder = 3
            });
            (await departments.GetAsync(department.Id)).Name.ShouldBe("Phòng Chuyển đổi số");
        });
    }

    [Fact]
    public async Task Should_Reject_Cross_Tenant_Department_Assignment()
    {
        var currentTenant = ServiceProvider.GetRequiredService<ICurrentTenant>();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        Guid departmentId = Guid.Empty;

        using (currentTenant.Change(tenantA))
        {
            departmentId = await WithUnitOfWorkAsync(async () =>
                (await ServiceProvider.GetRequiredService<IDepartmentAppService>().CreateAsync(
                    new CreateUpdateDepartmentDto { Code = "A", Name = "Tenant A" })).Id);
        }

        using (currentTenant.Change(tenantB))
        {
            await Should.ThrowAsync<Exception>(() => WithUnitOfWorkAsync(async () =>
                await ServiceProvider.GetRequiredService<IUserDepartmentAppService>().CreateAsync(
                    new CreateUpdateUserDepartmentDto
                    {
                        DepartmentId = departmentId, UserId = Guid.NewGuid()
                    })));
        }
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
