using System.Net;
using System.Net.Http.Headers;
using HCS.WorkManagementService.Contracts;
using HCS.WorkManagementService.Data;
using HCS.WorkManagementService.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;

namespace HCS.WorkManagementService.Application;

public sealed class EmployeeRatingAlreadySubmittedException : Exception;

public interface IEmployeeDirectoryReader
{
    Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed class EmployeeDirectoryReader(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor) : IEmployeeDirectoryReader, ITransientDependency
{
    public async Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"api/identity/employee-directory/{userId:D}");
        var authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization))
            request.Headers.TryAddWithoutValidation("Authorization", authorization);

        using var response = await httpClientFactory.CreateClient("HCS.Platform")
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;
        if (!response.IsSuccessStatusCode)
            throw new BusinessException("Work:EmployeeDirectoryUnavailable");
        return true;
    }
}

public sealed class EmployeeRatingClock(IConfiguration configuration) : ITransientDependency
{
    public DateOnly Today()
    {
        var timeZoneId = configuration["EmployeeRatings:TimeZoneId"] ?? "Asia/Ho_Chi_Minh";
        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.Utc;
        }

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
    }
}

public sealed class EmployeeRatingAppService(
    WorkManagementDbContext db,
    ICurrentUser currentUser,
    IEmployeeDirectoryReader directory,
    EmployeeRatingClock clock) : ITransientDependency
{
    public async Task<EmployeeRatingDto> SubmitAsync(SubmitEmployeeRatingDto input, CancellationToken cancellationToken)
    {
        var voterUserId = currentUser.Id ?? throw new BusinessException("Work:EmployeeRatingLoginRequired");
        var evaluationDate = clock.Today();
        if (!await directory.IsActiveAsync(input.TargetUserId, cancellationToken))
            throw new BusinessException("Work:EmployeeRatingTargetInactive");

        if (await db.EmployeeRatings.AnyAsync(x => x.VoterUserId == voterUserId
                && x.TargetUserId == input.TargetUserId
                && x.EvaluationDate == evaluationDate, cancellationToken))
            throw new EmployeeRatingAlreadySubmittedException();

        var rating = new EmployeeRating(Guid.NewGuid(), input.TargetUserId, voterUserId,
            input.Score, evaluationDate);
        db.EmployeeRatings.Add(rating);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_EmployeeRatings_DailyVote"
            })
        {
            throw new EmployeeRatingAlreadySubmittedException();
        }

        return new(rating.Id, rating.TargetUserId, rating.Score, rating.EvaluationDate,
            rating.CreationTime == default ? DateTime.UtcNow : rating.CreationTime);
    }

    public async Task<PagedWorkDto<EmployeeRatingSummaryDto>> GetSummariesAsync(
        DateTime? from, DateTime? to, int skip, int take, CancellationToken cancellationToken)
    {
        var query = ApplyRange(db.EmployeeRatings.AsNoTracking(), from, to);
        var rows = await query.Select(x => new RatingRow(x.TargetUserId, x.VoterUserId,
                x.Score, x.EvaluationDate, x.CreationTime)).ToListAsync(cancellationToken);
        var summaries = BuildSummaries(rows, currentUser.Id, clock.Today());
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 100);
        return new(summaries.Count, summaries.Skip(skip).Take(take).ToArray());
    }

    public async Task<EmployeeRatingDetailDto> GetDetailAsync(Guid targetUserId,
        DateTime? from, DateTime? to, string? period, CancellationToken cancellationToken)
    {
        var rows = await ApplyRange(db.EmployeeRatings.AsNoTracking(), from, to)
            .Where(x => x.TargetUserId == targetUserId)
            .Select(x => new RatingRow(x.TargetUserId, x.VoterUserId, x.Score, x.EvaluationDate, x.CreationTime))
            .ToListAsync(cancellationToken);
        var distribution = Distribution(rows.Select(x => x.Score));
        var trend = rows.GroupBy(x => TrendKey(x.EvaluationDate, period))
            .OrderBy(x => x.Key)
            .Select(x => new EmployeeRatingTrendDto(x.Key, Math.Round(x.Average(row => (decimal)row.Score), 2), x.Count()))
            .ToArray();
        return new(targetUserId,
            rows.Count == 0 ? 0 : Math.Round(rows.Average(x => (decimal)x.Score), 2),
            rows.Count, distribution, trend);
    }

    public async Task<EmployeeRatingDashboardDto> GetDashboardAsync(
        DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var rows = await ApplyRange(db.EmployeeRatings.AsNoTracking(), from, to)
            .Select(x => new RatingRow(x.TargetUserId, x.VoterUserId, x.Score, x.EvaluationDate, x.CreationTime))
            .ToListAsync(cancellationToken);
        var summaries = BuildSummaries(rows, null, clock.Today());
        return new(summaries.Count,
            rows.Count == 0 ? 0 : Math.Round(rows.Average(x => (decimal)x.Score), 2),
            rows.Select(x => x.VoterUserId).Distinct().Count(),
            Distribution(rows.Select(x => x.Score)), summaries);
    }

    private static IQueryable<EmployeeRating> ApplyRange(IQueryable<EmployeeRating> query,
        DateTime? from, DateTime? to)
    {
        if (from.HasValue) query = query.Where(x => x.CreationTime >= ToUtc(from.Value));
        if (to.HasValue) query = query.Where(x => x.CreationTime < ToUtc(to.Value));
        return query;
    }

    private static DateTime ToUtc(DateTime value) => value.Kind == DateTimeKind.Utc
        ? value : value.ToUniversalTime();

    private static IReadOnlyList<EmployeeRatingSummaryDto> BuildSummaries(
        IReadOnlyList<RatingRow> rows, Guid? voterUserId, DateOnly today)
    {
        var todayRatings = voterUserId.HasValue
            ? rows.Where(x => x.VoterUserId == voterUserId && x.EvaluationDate == today)
                .GroupBy(x => x.TargetUserId).ToDictionary(x => x.Key, x => x.Max(row => row.Score))
            : new Dictionary<Guid, int>();

        return rows.GroupBy(x => x.TargetUserId)
            .OrderByDescending(group => group.Average(x => x.Score)).ThenBy(x => x.Key)
            .Select(group => new EmployeeRatingSummaryDto(group.Key,
                Math.Round(group.Average(x => (decimal)x.Score), 2), group.Count(),
                Distribution(group.Select(x => x.Score)),
                todayRatings.TryGetValue(group.Key, out var score) ? score : null,
                group.Max(x => x.CreationTime)))
            .ToArray();
    }

    private static Dictionary<int, int> Distribution(IEnumerable<int> scores)
    {
        var result = Enumerable.Range(1, 5).ToDictionary(score => score, _ => 0);
        foreach (var score in scores)
            if (result.ContainsKey(score)) result[score]++;
        return result;
    }

    private static string TrendKey(DateOnly date, string? period) =>
        string.Equals(period, "quarter", StringComparison.OrdinalIgnoreCase)
            ? $"{date.Year}-Q{((date.Month - 1) / 3) + 1}"
            : $"{date.Year}-{date.Month:00}";

    private sealed record RatingRow(Guid TargetUserId, Guid VoterUserId, int Score,
        DateOnly EvaluationDate, DateTime CreationTime);
}
