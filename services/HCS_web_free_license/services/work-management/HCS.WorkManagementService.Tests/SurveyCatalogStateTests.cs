using System.Text.Json;
using HCS.WorkManagementService.Application;
using HCS.WorkManagementService.Contracts;
using HCS.WorkManagementService.Data;
using Microsoft.EntityFrameworkCore;

namespace HCS.WorkManagementService.Tests;

public sealed class SurveyCatalogStateTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Criteria_preserves_active_state_on_create_and_update(bool isActive)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var service = new SurveyAppService(db, null!);
        var created = await service.CreateCriteriaAsync(new("CR", "Criteria", 1, IsActive: isActive), ct);
        Assert.Equal(isActive, created.IsActive);
        db.ChangeTracker.Clear();
        Assert.Equal(isActive, Assert.Single(await service.GetCriteriaAsync(ct)).IsActive);

        await service.UpdateCriteriaAsync(created.Id, new("Criteria", 1, !isActive), ct);
        db.ChangeTracker.Clear();
        Assert.Equal(!isActive, Assert.Single(await service.GetCriteriaAsync(ct)).IsActive);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Location_preserves_active_state_on_create_and_update(bool isActive)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var service = new SurveyAppService(db, null!);
        var created = await service.CreateLocationAsync(new("LOC", "Location", null, IsActive: isActive), ct);
        Assert.Equal(isActive, created.IsActive);
        db.ChangeTracker.Clear();
        Assert.Equal(isActive, Assert.Single(await service.GetLocationsAsync(ct)).IsActive);

        await service.UpdateLocationAsync(created.Id, new("Location", null, !isActive), ct);
        db.ChangeTracker.Clear();
        Assert.Equal(!isActive, Assert.Single(await service.GetLocationsAsync(ct)).IsActive);
    }

    [Fact]
    public void Older_create_payloads_default_to_active()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Assert.True(JsonSerializer.Deserialize<CreateSurveyCriteriaDto>(
            """{"code":"CR","name":"Criteria","sortOrder":1}""", options)!.IsActive);
        Assert.True(JsonSerializer.Deserialize<CreateSurveyLocationDto>(
            """{"code":"LOC","name":"Location","organizationUnitId":null}""", options)!.IsActive);
    }

    private static WorkManagementDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<WorkManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
