using HCS.WorkManagementService.Application;
using HCS.WorkManagementService.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCS.WorkManagementService.Controllers;

[ApiController]
[Authorize(Policy = WorkPermissions.EmployeeRatingsRead)]
[Route("api/employee-ratings")]
public sealed class EmployeeRatingsController(EmployeeRatingAppService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = WorkPermissions.EmployeeRatings)]
    public async Task<ActionResult<EmployeeRatingDto>> Submit(
        [FromBody] SubmitEmployeeRatingDto input, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.SubmitAsync(input, cancellationToken));
        }
        catch (EmployeeRatingAlreadySubmittedException)
        {
            return Conflict(new { code = "Work:EmployeeRatingAlreadySubmitted" });
        }
    }

    [HttpGet("summary")]
    public Task<PagedWorkDto<EmployeeRatingSummaryDto>> Summary(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int skip = 0, [FromQuery] int take = 100,
        CancellationToken cancellationToken = default) =>
        service.GetSummariesAsync(from, to, skip, take, cancellationToken);

    [HttpGet("{userId:guid}/detail")]
    [Authorize(Policy = WorkPermissions.EmployeeRatingsManagement)]
    public Task<EmployeeRatingDetailDto> Detail(Guid userId, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] string? period = "month",
        CancellationToken cancellationToken = default) =>
        service.GetDetailAsync(userId, from, to, period, cancellationToken);

    [HttpGet("dashboard")]
    [Authorize(Policy = WorkPermissions.EmployeeRatingsDashboard)]
    public Task<EmployeeRatingDashboardDto> Dashboard([FromQuery] DateTime? from,
        [FromQuery] DateTime? to, CancellationToken cancellationToken = default) =>
        service.GetDashboardAsync(from, to, cancellationToken);
}
