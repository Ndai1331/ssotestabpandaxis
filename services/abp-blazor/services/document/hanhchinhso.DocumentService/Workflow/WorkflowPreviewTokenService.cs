using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace hanhchinhso.DocumentService.Workflows;

public sealed record WorkflowPreviewTokenPayload(
    Guid? TenantId,
    Guid DocumentId,
    Guid SourceFileId,
    string SourceFileSha256,
    Guid WorkflowId,
    Guid WorkflowTemplateId,
    Guid? PreviousInstanceId,
    Guid InitiatorUserId,
    string TemplateConcurrencyStamp,
    string CandidateHash,
    DateTime ExpiresAtUtc);

public class WorkflowPreviewTokenService : ITransientDependency
{
    private readonly IDataProtector _protector;

    public WorkflowPreviewTokenService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(
            "DocumentService.WorkflowPreview.v2");
    }

    public string Protect(WorkflowPreviewTokenPayload payload) =>
        _protector.Protect(JsonSerializer.Serialize(payload));

    public WorkflowPreviewTokenPayload Unprotect(string token)
    {
        try
        {
            return JsonSerializer.Deserialize<WorkflowPreviewTokenPayload>(
                       _protector.Unprotect(token))
                   ?? throw Invalid();
        }
        catch (Exception exception) when (
            exception is CryptographicException or JsonException)
        {
            throw Invalid();
        }
    }

    public static string HashCandidates(
        IEnumerable<WorkflowStepSubmitPreviewDto> steps)
    {
        var canonical = string.Join(
            "\n",
            steps.OrderBy(x => x.Order).Select(step =>
                $"{step.WorkflowStepTemplateId:N}|{step.Order}|{step.Name}|" +
                $"{step.Type}|{step.AllowReturn}|{step.SlaDays}|" +
                string.Join(",", step.Candidates
                    .OrderBy(x => x.UserId)
                    .Select(x =>
                        $"{x.UserId:N}:{x.ProvenanceOrganizationUnitId:N}:{x.ProvenanceRoleId:N}:{x.IsPrimary}")) +
                "|" + string.Join(",", step.ViewUserIds.Order()) +
                "|" + string.Join(",", step.ViewOrganizationUnitIds.Order())));
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static UserFriendlyException Invalid() =>
        new("The workflow preview token is invalid or expired. Preview again.");
}
