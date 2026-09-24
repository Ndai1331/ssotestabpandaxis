using HCS.OrganizationService.Application;
using HCS.OrganizationService.Contracts;
using HCS.OrganizationService.Data;
using HCS.OrganizationService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Guids;

namespace HCS.OrganizationService.Tests;

public sealed class OrganizationAppServiceTests : OrganizationTestBase
{
    [Fact]
    public async Task Create_department_is_idempotency_safe_for_codes()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var service = new OrganizationAppService(db, new TestGuidGenerator());
        await service.CreateDepartmentAsync(new UpsertDepartmentDto { Code = "KHTH", Name = "Kế hoạch tổng hợp" }, ct);

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            service.CreateDepartmentAsync(new UpsertDepartmentDto { Code = "KHTH", Name = "Duplicate" }, ct));

        exception.Code.ShouldBe(OrganizationErrorCodes.DuplicateCode);
        db.Departments.Count().ShouldBe(1);
    }

    [Fact]
    public async Task Unit_must_reference_an_existing_department()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var service = new OrganizationAppService(db, new TestGuidGenerator())
        {
            UnitDepartmentLookup = new TestUnitDepartmentLookup(Guid.NewGuid())
        };
        var exception = await Should.ThrowAsync<BusinessException>(() =>
            service.CreateUnitAsync(new UpsertUnitDto
            {
                DepartmentId = Guid.NewGuid(), Code = "U1", Name = "Unit 1"
            }, ct));
        exception.Code.ShouldBe(OrganizationErrorCodes.InvalidDepartment);
    }

    [Fact]
    public async Task Unit_can_reference_identity_department_without_legacy_department()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var departmentId = Guid.NewGuid();
        var service = new OrganizationAppService(db, new TestGuidGenerator())
        {
            UnitDepartmentLookup = new TestUnitDepartmentLookup(departmentId)
        };
        var unit = await service.CreateUnitAsync(new UpsertUnitDto
        {
            DepartmentId = departmentId, Code = "NEW-OU", Name = "Identity department unit"
        }, ct);
        unit.DepartmentId.ShouldBe(departmentId);
        (await db.Departments.AnyAsync(x => x.Id == departmentId, ct)).ShouldBeFalse();

        await Should.ThrowAsync<BusinessException>(() => service.UpdateUnitAsync(unit.Id,
            new UpsertUnitDto { DepartmentId = Guid.NewGuid(), Code = "NEW-OU", Name = "Invalid" }, ct));
    }

    private sealed class TestUnitDepartmentLookup(Guid id) : IUnitDepartmentLookup
    {
        public Task<bool> ExistsAsync(Guid candidate, CancellationToken cancellationToken = default) =>
            Task.FromResult(candidate == id);
    }

    [Fact]
    public async Task A_user_can_have_only_one_primary_mapping()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var service = new OrganizationAppService(db, new TestGuidGenerator());
        var department = await service.CreateDepartmentAsync(new UpsertDepartmentDto { Code = "D1", Name = "Department" }, ct);
        var userId = Guid.NewGuid();
        await service.CreateUserMappingAsync(new UpsertUserOrganizationMappingDto
        {
            UserId = userId, DepartmentId = department.Id, IsPrimary = true
        }, ct);

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            service.CreateUserMappingAsync(new UpsertUserOrganizationMappingDto
            {
                UserId = userId, DepartmentId = department.Id, IsPrimary = true
            }, ct));
        exception.Code.ShouldBe(OrganizationErrorCodes.MultiplePrimaryMappings);
    }

    [Fact]
    public async Task User_department_lookup_returns_primary_department_and_name()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var service = new OrganizationAppService(db, new TestGuidGenerator());
        var primary = await service.CreateDepartmentAsync(new UpsertDepartmentDto { Code = "PRIMARY", Name = "Primary department" }, ct);
        var secondary = await service.CreateDepartmentAsync(new UpsertDepartmentDto { Code = "SECONDARY", Name = "Secondary department" }, ct);
        var userId = Guid.NewGuid();

        await service.CreateUserMappingAsync(new UpsertUserOrganizationMappingDto
        {
            UserId = userId, DepartmentId = secondary.Id, IsPrimary = false
        }, ct);
        await service.CreateUserMappingAsync(new UpsertUserOrganizationMappingDto
        {
            UserId = userId, DepartmentId = primary.Id, IsPrimary = true
        }, ct);

        var result = (await service.GetUserDepartmentsAsync([userId], ct)).Single();

        result.UserId.ShouldBe(userId);
        result.DepartmentId.ShouldBe(primary.Id);
        result.DepartmentName.ShouldBe("Primary department");
    }

    [Fact]
    public async Task Department_hierarchy_cannot_contain_a_cycle()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var service = new OrganizationAppService(db, new TestGuidGenerator());
        var root = await service.CreateDepartmentAsync(new UpsertDepartmentDto { Code = "ROOT", Name = "Root" }, ct);
        var child = await service.CreateDepartmentAsync(new UpsertDepartmentDto
        {
            Code = "CHILD", Name = "Child", ParentId = root.Id
        }, ct);

        var exception = await Should.ThrowAsync<BusinessException>(() => service.UpdateDepartmentAsync(root.Id,
            new UpsertDepartmentDto { Code = "ROOT", Name = "Root", ParentId = child.Id }, ct));
        exception.Code.ShouldBe(OrganizationErrorCodes.DepartmentHierarchyCycle);
    }

    [Fact]
    public async Task Department_list_applies_filter_active_status_and_server_paging()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var service = new OrganizationAppService(db, new TestGuidGenerator());

        await service.CreateDepartmentAsync(new UpsertDepartmentDto
        {
            Code = "OPS-1", Name = "Operations one", SortOrder = 1, IsActive = true
        }, ct);
        await service.CreateDepartmentAsync(new UpsertDepartmentDto
        {
            Code = "OPS-2", Name = "Operations two", SortOrder = 2, IsActive = true
        }, ct);
        await service.CreateDepartmentAsync(new UpsertDepartmentDto
        {
            Code = "OPS-3", Name = "Operations three", SortOrder = 3, IsActive = false
        }, ct);

        var result = await service.GetDepartmentsAsync(new OrganizationListInput
        {
            Filter = "OPS",
            IsActive = true,
            SkipCount = 1,
            MaxResultCount = 1
        }, ct);

        result.TotalCount.ShouldBe(2);
        result.Items.Count.ShouldBe(1);
        result.Items[0].Code.ShouldBe("OPS-2");
    }

    [Fact]
    public async Task Master_data_rejects_a_type_outside_the_shared_allow_list()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var service = new OrganizationAppService(db, new TestGuidGenerator());

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            service.CreateMasterDataAsync(new UpsertMasterDataItemDto
            {
                Type = "arbitrary-type", Code = "X", Name = "Not allowed"
            }, ct));

        exception.Code.ShouldBe(OrganizationErrorCodes.InvalidMasterDataType);
        db.MasterDataItems.Count().ShouldBe(0);
    }

    [Fact]
    public async Task Health_ranges_validate_minimum_and_maximum_values()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var service = new OrganizationAppService(db, new TestGuidGenerator());

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            service.CreateBloodGlucoseRangeAsync(new UpsertBloodGlucoseRangeDto
            {
                Title = "Invalid", MinValue = 8.1m, MaxValue = 5.2m
            }, ct));

        exception.Code.ShouldBe(OrganizationErrorCodes.InvalidRange);
        db.BloodGlucoseRanges.Count().ShouldBe(0);
    }

    [Fact]
    public async Task Province_and_commune_lists_include_parent_codes()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var service = new OrganizationAppService(db, new TestGuidGenerator());
        var country = await service.CreateCountryAsync(new UpsertCountryDto
        {
            Code = "VN", Name = "Việt Nam", CountryCode = "VNM"
        }, ct);
        var province = await service.CreateProvinceAsync(new UpsertProvinceDto
        {
            Code = "BD", Name = "Bình Dương", CountryId = country.Id
        }, ct);
        var commune = await service.CreateCommuneAsync(new UpsertCommuneDto
        {
            Code = "TDM", Name = "Thủ Dầu Một", ProvinceId = province.Id
        }, ct);

        var provinceResult = await service.GetProvincesAsync(new OrganizationListInput(), ct);
        var communeResult = await service.GetCommunesAsync(new OrganizationListInput(), ct);

        provinceResult.Items.Single().CountryCode.ShouldBe("VN");
        communeResult.Items.Single().ProvinceCode.ShouldBe("BD");
        commune.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Province_rejects_an_unknown_country()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var service = new OrganizationAppService(db, new TestGuidGenerator());

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            service.CreateProvinceAsync(new UpsertProvinceDto
            {
                Code = "P1", Name = "Province", CountryId = Guid.NewGuid()
            }, ct));

        exception.Code.ShouldBe(OrganizationErrorCodes.InvalidCountry);
    }

    [Fact]
    public async Task Duplicate_non_primary_user_mapping_is_rejected_before_database_write()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var service = new OrganizationAppService(db, new TestGuidGenerator());
        var department = await service.CreateDepartmentAsync(new UpsertDepartmentDto { Code = "D2", Name = "Department 2" }, ct);
        var input = new UpsertUserOrganizationMappingDto
        {
            UserId = Guid.NewGuid(), DepartmentId = department.Id, IsPrimary = false
        };
        await service.CreateUserMappingAsync(input, ct);

        var exception = await Should.ThrowAsync<BusinessException>(() => service.CreateUserMappingAsync(input, ct));
        exception.Code.ShouldBe(OrganizationErrorCodes.DuplicateUserMapping);
    }

    [Fact]
    public async Task User_mapping_materializes_a_department_for_an_identity_unit_id()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var service = new OrganizationAppService(db, new TestGuidGenerator());
        var departmentId = Guid.NewGuid();
        var position = await service.CreatePositionAsync(new UpsertPositionDto
        {
            Code = "BS", Name = "Bác sĩ", SignOrder = 1
        }, ct);

        var mapping = await service.CreateUserMappingAsync(new UpsertUserOrganizationMappingDto
        {
            UserId = Guid.NewGuid(),
            DepartmentId = departmentId,
            PositionId = position.Id,
            IsPrimary = true,
            DepartmentCode = "00001",
            DepartmentName = "Khoa nội"
        }, ct);

        mapping.DepartmentId.ShouldBe(departmentId);
        mapping.PositionId.ShouldBe(position.Id);
        var department = await db.Departments.FindAsync([departmentId], ct);
        department.ShouldNotBeNull();
        department!.Code.ShouldBe("00001");
        department.Name.ShouldBe("Khoa nội");
    }

    [Fact]
    public void Model_is_single_tenant_and_uses_owned_schema()
    {
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        db.Model.GetDefaultSchema().ShouldBe(OrganizationDbContext.Schema);
        foreach (var entity in db.Model.GetEntityTypes())
            entity.FindProperty("TenantId").ShouldBeNull();
    }

    private sealed class TestGuidGenerator : IGuidGenerator
    {
        public Guid Create() => Guid.NewGuid();
    }
}
