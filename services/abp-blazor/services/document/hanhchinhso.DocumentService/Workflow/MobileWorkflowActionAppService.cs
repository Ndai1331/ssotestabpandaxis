using hanhchinhso.DocumentService.Data;
using hanhchinhso.DocumentService.Permissions;
using hanhchinhso.DocumentService.Signing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;

namespace hanhchinhso.DocumentService.Workflows;

[Authorize(DocumentServicePermissions.WorkflowRuntime.Act)]
public class MobileWorkflowActionAppService :
    ApplicationService,
    IMobileWorkflowActionAppService
{
    private readonly DocumentServiceDbContext _db;
    private readonly IWorkflowActionAppService _actions;
    private readonly ISigningExecutionAppService _signing;

    public MobileWorkflowActionAppService(
        DocumentServiceDbContext db,
        IWorkflowActionAppService actions,
        ISigningExecutionAppService signing)
    {
        _db = db;
        _actions = actions;
        _signing = signing;
    }

    public async Task<MobileWorkflowActionResultDto> ProcessAsync(
        MobileWorkflowActionInput input)
    {
        _ = CurrentUser.Id ?? throw new AbpAuthorizationException();
        var actionInput = new WorkflowAssignmentActionInput
        {
            AssignmentConcurrencyStamp = input.AssignmentConcurrencyStamp,
            Comment = input.Comment
        };
        if (input.Action is MobileWorkflowAction.ELECTRONIC
            or MobileWorkflowAction.DIGITAL)
        {
            return await SignAsync(
                input,
                actionInput,
                input.Action == MobileWorkflowAction.ELECTRONIC
                    ? SignatureType.Electronic
                    : SignatureType.Digital);
        }

        var instance = input.Action switch
        {
            MobileWorkflowAction.APPROVE => await _actions.ApproveAsync(
                input.AssignmentId, actionInput),
            MobileWorkflowAction.RETURN => await _actions.ReturnAsync(
                input.AssignmentId, actionInput),
            MobileWorkflowAction.REJECT => await _actions.RejectAsync(
                input.AssignmentId, actionInput),
            _ => throw new BusinessException(
                "DocumentService:UnsupportedMobileWorkflowAction")
        };
        return new MobileWorkflowActionResultDto { Instance = instance };
    }

    private async Task<MobileWorkflowActionResultDto> SignAsync(
        MobileWorkflowActionInput input,
        WorkflowAssignmentActionInput actionInput,
        SignatureType signatureType)
    {
        if (input.UserSignatureId is not { } userSignatureId ||
            userSignatureId == Guid.Empty)
        {
            throw new BusinessException(
                "DocumentService:MobileUserSignatureRequired");
        }

        var assignment = await _db.DocumentAssignments
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == input.AssignmentId)
            ?? throw new EntityNotFoundException(
                typeof(DocumentAssignment), input.AssignmentId);
        var succeeded = await _db.SigningAttempts
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.AssignmentId == input.AssignmentId &&
                x.UserSignatureId == userSignatureId &&
                x.SignatureType == signatureType &&
                x.Status == SigningAttemptStatus.Succeeded);
        // Reuse the source file of a previous successful attempt so retries
        // sign the same input instead of chaining onto their own output.
        var sourceFileId = succeeded?.SourceFileId;
        if (sourceFileId is null)
        {
            var instance = await GetInstanceAsync(assignment.InstanceId);
            sourceFileId =
                instance.CurrentSignedFileId ?? instance.SourceFileId;
            await _actions.RequestSignAsync(input.AssignmentId, actionInput);
        }

        var signInput = new DigitalSignInput
        {
            SourceFileId = sourceFileId.Value,
            UserSignatureId = userSignatureId,
            AssignmentConcurrencyStamp =
                input.AssignmentConcurrencyStamp,
            Comment = input.Comment
        };
        var attempt = signatureType == SignatureType.Electronic
            ? await _signing.ExecuteElectronicAsync(
                input.AssignmentId, signInput)
            : await _signing.ExecuteDigitalAsync(
                input.AssignmentId, signInput);
        return new MobileWorkflowActionResultDto
        {
            Instance = MapInstance(
                await GetInstanceAsync(assignment.InstanceId)),
            SigningAttempt = attempt
        };
    }

    private Task<DocumentWorkflowInstance> GetInstanceAsync(Guid instanceId) =>
        _db.DocumentWorkflowInstances
            .AsNoTracking()
            .SingleAsync(x => x.Id == instanceId);

    private static DocumentWorkflowInstanceDto MapInstance(
        DocumentWorkflowInstance x) => new()
    {
        Id = x.Id,
        DocumentId = x.DocumentId,
        WorkflowId = x.WorkflowId,
        WorkflowTemplateId = x.WorkflowTemplateId,
        InitiatorUserId = x.InitiatorUserId,
        SignMode = x.SignMode,
        Status = x.Status,
        SourceFileId = x.SourceFileId,
        CurrentSignedFileId = x.CurrentSignedFileId,
        CurrentCommittedStepId = x.CurrentCommittedStepId,
        StartedAtUtc = x.StartedAtUtc,
        DeadlineAtUtc = x.DeadlineAtUtc,
        FinishedAtUtc = x.FinishedAtUtc,
        OverdueAtUtc = x.OverdueAtUtc,
        PreviousInstanceId = x.PreviousInstanceId,
        ExtensionCount = x.ExtensionCount,
        TotalExtensionBusinessDays = x.TotalExtensionBusinessDays,
        ConcurrencyStamp = x.ConcurrencyStamp
    };
}
