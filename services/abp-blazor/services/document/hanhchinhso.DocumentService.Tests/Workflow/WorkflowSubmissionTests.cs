using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using hanhchinhso.DocumentService.Data;
using hanhchinhso.DocumentService.Documents;
using hanhchinhso.DocumentService.Signing;
using hanhchinhso.DocumentService.Workflows;
using hanhchinhso.IdentityService.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Security.Claims;
using Volo.Abp.BlobStoring;
using Volo.Abp.Uow;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace hanhchinhso.DocumentService.Tests.Workflow;

public class WorkflowSubmissionTests : DocumentServiceIntegrationTestBase
{
    [Fact]
    public async Task Should_Expose_Authorized_Mobile_Inbox_And_Safe_Detail()
    {
        var initiatorId = Guid.NewGuid();
        using var principal = ChangeUser(initiatorId);
        var setup = await CreateSetupAsync(WorkflowSignMode.Sequential);
        ConfigureResolution();
        var instance = await SubmitSetupAsync(setup);
        var mobile = GetRequiredService<IMobileWorkflowQueryAppService>();

        var sentByMe = await mobile.GetSigningListAsync(new()
        {
            FilterMode = MobileSigningFilterMode.SentByMe,
            MaxResultCount = 20
        });
        sentByMe.TotalCount.ShouldBe(1);
        sentByMe.Items.Single().CanResubmit.ShouldBeFalse();
        sentByMe.SentByMeCount.ShouldBe(1);
        (await mobile.GetSigningListAsync(new()
        {
            FilterMode = MobileSigningFilterMode.Following,
            MaxResultCount = 20
        })).Items.ShouldBeEmpty();

        var detail = await mobile.GetDetailAsync(instance.Id);
        detail.Runtime.Instance.SourceFileId.ShouldBe(setup.SourceFileId);
        detail.Document.Id.ShouldBe(setup.DocumentId);
        detail.Files.Select(x => x.Id).ShouldBe([setup.SourceFileId]);
        detail.Logs.ShouldHaveSingleItem()
            .Action.ShouldBe(WorkflowRuntimeAction.Submit);
        detail.History.ShouldHaveSingleItem()
            .Action.ShouldBe(WorkflowRuntimeAction.Submit);
        var access = GetRequiredService<IWorkflowDocumentAccessService>();
        (await access.CanAccessFileAsync(
            setup.SourceFileId, initiatorId)).ShouldBeTrue();
        await Should.ThrowAsync<BusinessException>(() =>
            GetRequiredService<IDocumentFileAppService>()
                .DeleteAsync(
                    setup.SourceFileId,
                    detail.Files.Single().ConcurrencyStamp));

        using (ChangeUser(setup.FirstUserId))
        {
            var sentToMe = await mobile.GetSigningListAsync(new()
            {
                FilterMode = MobileSigningFilterMode.SentToMe,
                MaxResultCount = 20
            });
            var item = sentToMe.Items.ShouldHaveSingleItem();
            item.MyAssignmentId.ShouldNotBeNull();
            item.CanAct.ShouldBeTrue();
            (await mobile.GetDetailAsync(instance.Id))
                .Files.ShouldHaveSingleItem();
        }
        var outsiderId = Guid.NewGuid();
        using (ChangeUser(outsiderId))
        {
            (await mobile.GetSigningListAsync(new()
            {
                MaxResultCount = 20
            })).Items.ShouldBeEmpty();
            await Should.ThrowAsync<
                Volo.Abp.Authorization.AbpAuthorizationException>(
                () => mobile.GetDetailAsync(instance.Id));
            (await access.CanAccessFileAsync(
                setup.SourceFileId, outsiderId)).ShouldBeFalse();
            await Should.ThrowAsync<
                Volo.Abp.Authorization.AbpAuthorizationException>(
                () => GetRequiredService<IDocumentFileAppService>()
                    .GetListAsync(setup.DocumentId));
            await Should.ThrowAsync<
                Volo.Abp.Authorization.AbpAuthorizationException>(
                () => GetRequiredService<IDocumentFileAppService>()
                    .DeleteAsync(
                        setup.SourceFileId,
                        detail.Files.Single().ConcurrencyStamp));
        }
    }

    [Fact]
    public async Task Should_Reject_Cross_User_Document_Source_Guessing()
    {
        var ownerId = Guid.NewGuid();
        using var owner = ChangeUser(ownerId);
        var setup = await CreateSetupAsync(WorkflowSignMode.Sequential);
        ConfigureResolution();
        using (ChangeUser(Guid.NewGuid()))
        {
            await Should.ThrowAsync<
                Volo.Abp.Authorization.AbpAuthorizationException>(
                () => GetRequiredService<IWorkflowSubmissionAppService>()
                    .PreviewAsync(new()
                    {
                        DocumentId = setup.DocumentId,
                        SourceFileId = setup.SourceFileId,
                        WorkflowTemplateId = setup.TemplateId
                    }));
        }
        await AssertNoWorkflowInstanceAsync();
    }

    [Fact]
    public async Task Should_Bind_Workflow_Source_File_And_Hash_To_Preview()
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(WorkflowSignMode.Sequential);
        ConfigureResolution();
        var alternateBytes = CreatePdfWithPlaceholder("ALTERNATE");
        var alternateId = Guid.NewGuid();
        var alternateBlobName =
            $"documents/{setup.DocumentId:N}-alternate.pdf";
        await WithUnitOfWorkAsync(async () =>
        {
            var db = GetRequiredService<DocumentServiceDbContext>();
            db.DocumentFiles.Add(new DocumentFile(
                alternateId,
                null,
                setup.DocumentId,
                "alternate.pdf",
                alternateBlobName,
                "application/pdf",
                alternateBytes.LongLength,
                Hash(alternateBytes)));
            await db.SaveChangesAsync();
        });
        GetRequiredService<IBlobContainer<DocumentBlobContainer>>()
            .GetAsync(
                alternateBlobName,
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(
                new MemoryStream(alternateBytes, writable: false)));
        var submission = GetRequiredService<IWorkflowSubmissionAppService>();
        var selections = CreateFirstStepSelection(setup);
        var preview = await submission.PreviewAsync(new()
        {
            DocumentId = setup.DocumentId,
            SourceFileId = setup.SourceFileId,
            WorkflowTemplateId = setup.TemplateId,
            Selections = selections
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            submission.SubmitAsync(new()
            {
                DocumentId = setup.DocumentId,
                SourceFileId = alternateId,
                WorkflowTemplateId = setup.TemplateId,
                PreviewToken = preview.PreviewToken,
                DocumentConcurrencyStamp =
                    setup.DocumentConcurrencyStamp,
                Selections = selections
            }));
        await AssertNoWorkflowInstanceAsync();

        var changedBytes = CreatePdfWithPlaceholder("<<Sign03>>");
        GetRequiredService<IBlobContainer<DocumentBlobContainer>>()
            .GetAsync(
                setup.SourceBlobName,
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(
                new MemoryStream(changedBytes, writable: false)));
        var exception = await Should.ThrowAsync<BusinessException>(() =>
            submission.SubmitAsync(new()
            {
                DocumentId = setup.DocumentId,
                SourceFileId = setup.SourceFileId,
                WorkflowTemplateId = setup.TemplateId,
                PreviewToken = preview.PreviewToken,
                DocumentConcurrencyStamp =
                    setup.DocumentConcurrencyStamp,
                Selections = selections
            }));
        exception.Code.ShouldBe(
            "DocumentService:WorkflowSourceHashMismatch");
        await AssertNoWorkflowInstanceAsync();
    }

    [Fact]
    public async Task Should_Preview_And_Atomically_Submit_Sequential_Snapshot()
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(WorkflowSignMode.Sequential);
        ConfigureResolution();
        var service = GetRequiredService<IWorkflowSubmissionAppService>();

        var preview = await service.PreviewAsync(new WorkflowSubmitPreviewInput
        {
            DocumentId = setup.DocumentId,
            SourceFileId = setup.SourceFileId,
            WorkflowTemplateId = setup.TemplateId,
            Selections =
            [
                new()
                {
                    WorkflowStepTemplateId = setup.FirstStepId,
                    UserId = setup.FirstUserId
                }
            ]
        });

        preview.SignMode.ShouldBe(WorkflowSignMode.Sequential);
        preview.Steps.Count.ShouldBe(2);
        preview.Steps[0].Candidates.Count.ShouldBe(2);
        preview.Steps[0].Candidates.Single(x => x.IsSelected)
            .UserId.ShouldBe(setup.FirstUserId);
        preview.Steps[1].Candidates.Single().IsSelected.ShouldBeTrue();

        var instance = await service.SubmitAsync(new WorkflowSubmitInput
        {
            DocumentId = setup.DocumentId,
            SourceFileId = setup.SourceFileId,
            WorkflowTemplateId = setup.TemplateId,
            PreviewToken = preview.PreviewToken,
            DocumentConcurrencyStamp = setup.DocumentConcurrencyStamp,
            Selections =
            [
                new()
                {
                    WorkflowStepTemplateId = setup.FirstStepId,
                    UserId = setup.FirstUserId
                }
            ]
        });

        instance.Status.ShouldBe(DocumentWorkflowStatus.InProgress);
        instance.SignMode.ShouldBe(WorkflowSignMode.Sequential);
        instance.CurrentCommittedStepId.ShouldNotBeNull();
        await WithUnitOfWorkAsync(async () =>
        {
            var db = GetRequiredService<DocumentServiceDbContext>();
            var persisted = await db.DocumentWorkflowInstances
                .Include(x => x.Steps).ThenInclude(x => x.Receivers)
                .SingleAsync(x => x.Id == instance.Id);
            persisted.Steps.Count.ShouldBe(2);
            persisted.Steps.Single(x => x.TemplateStepId == setup.FirstStepId)
                .Receivers.Count.ShouldBe(2);
            (await db.DocumentAssignments
                    .Where(x => x.InstanceId == instance.Id).ToListAsync())
                .ShouldHaveSingleItem().IsCurrent.ShouldBeTrue();
            (await db.DocumentWorkflowInstanceLogs
                    .CountAsync(x => x.InstanceId == instance.Id)).ShouldBe(1);
            (await db.DocumentHistories
                    .CountAsync(x => x.InstanceId == instance.Id)).ShouldBe(1);
            (await db.Documents.SingleAsync(x => x.Id == setup.DocumentId))
                .CurrentStatus.ShouldBe("WORKFLOW_IN_PROGRESS");
        });
    }

    [Fact]
    public async Task Should_Create_All_Parallel_Assignments()
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(WorkflowSignMode.Parallel);
        ConfigureResolution();
        var service = GetRequiredService<IWorkflowSubmissionAppService>();
        var selections = new List<WorkflowSubmitSelectionDto>
        {
            new()
            {
                WorkflowStepTemplateId = setup.FirstStepId,
                UserId = setup.SecondUserId
            }
        };
        var preview = await service.PreviewAsync(new()
        {
            DocumentId = setup.DocumentId,
            SourceFileId = setup.SourceFileId,
            WorkflowTemplateId = setup.TemplateId,
            Selections = selections
        });
        var instance = await service.SubmitAsync(new()
        {
            DocumentId = setup.DocumentId,
            SourceFileId = setup.SourceFileId,
            WorkflowTemplateId = setup.TemplateId,
            Selections = selections,
            PreviewToken = preview.PreviewToken,
            DocumentConcurrencyStamp = setup.DocumentConcurrencyStamp
        });

        instance.CurrentCommittedStepId.ShouldBeNull();
        await WithUnitOfWorkAsync(async () =>
        {
            var assignments = await GetRequiredService<DocumentServiceDbContext>()
                .DocumentAssignments
                .Where(x => x.InstanceId == instance.Id)
                .ToListAsync();
            assignments.Count.ShouldBe(2);
            assignments.ShouldAllBe(x => x.IsCurrent);
        });
    }

    [Fact]
    public async Task Should_Reject_Stale_Candidate_Set_Without_Persisting()
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(WorkflowSignMode.Sequential);
        ConfigureResolution();
        var service = GetRequiredService<IWorkflowSubmissionAppService>();
        var preview = await service.PreviewAsync(new()
        {
            DocumentId = setup.DocumentId,
            SourceFileId = setup.SourceFileId,
            WorkflowTemplateId = setup.TemplateId,
            Selections =
            [
                new()
                {
                    WorkflowStepTemplateId = setup.FirstStepId,
                    UserId = setup.FirstUserId
                }
            ]
        });
        ConfigureResolution(Guid.NewGuid());

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.SubmitAsync(new()
            {
                DocumentId = setup.DocumentId,
                SourceFileId = setup.SourceFileId,
                WorkflowTemplateId = setup.TemplateId,
                PreviewToken = preview.PreviewToken,
                DocumentConcurrencyStamp = setup.DocumentConcurrencyStamp,
                Selections =
                [
                    new()
                    {
                        WorkflowStepTemplateId = setup.FirstStepId,
                        UserId = setup.FirstUserId
                    }
                ]
            }));
        await WithUnitOfWorkAsync(async () =>
            (await GetRequiredService<DocumentServiceDbContext>()
                .DocumentWorkflowInstances.CountAsync())
            .ShouldBe(0));
    }

    [Fact]
    public async Task Should_Reject_Tampered_Preview_Token_Without_Persisting()
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(WorkflowSignMode.Sequential);
        ConfigureResolution();
        var service = GetRequiredService<IWorkflowSubmissionAppService>();
        var selections = CreateFirstStepSelection(setup);
        var preview = await service.PreviewAsync(new()
        {
            DocumentId = setup.DocumentId,
            SourceFileId = setup.SourceFileId,
            WorkflowTemplateId = setup.TemplateId,
            Selections = selections
        });
        var tampered = preview.PreviewToken[..^1]
            + (preview.PreviewToken[^1] == 'A' ? "B" : "A");

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.SubmitAsync(new()
            {
                DocumentId = setup.DocumentId,
                SourceFileId = setup.SourceFileId,
                WorkflowTemplateId = setup.TemplateId,
                PreviewToken = tampered,
                DocumentConcurrencyStamp = setup.DocumentConcurrencyStamp,
                Selections = selections
            }));
        await AssertNoWorkflowInstanceAsync();
    }

    [Fact]
    public async Task Should_Reject_Stale_Document_Stamp_Without_Persisting()
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(WorkflowSignMode.Sequential);
        ConfigureResolution();
        var service = GetRequiredService<IWorkflowSubmissionAppService>();
        var selections = CreateFirstStepSelection(setup);
        var preview = await service.PreviewAsync(new()
        {
            DocumentId = setup.DocumentId,
            SourceFileId = setup.SourceFileId,
            WorkflowTemplateId = setup.TemplateId,
            Selections = selections
        });

        await Should.ThrowAsync<Volo.Abp.Data.AbpDbConcurrencyException>(() =>
            service.SubmitAsync(new()
            {
                DocumentId = setup.DocumentId,
                SourceFileId = setup.SourceFileId,
                WorkflowTemplateId = setup.TemplateId,
                PreviewToken = preview.PreviewToken,
                DocumentConcurrencyStamp = "stale",
                Selections = selections
            }));
        await AssertNoWorkflowInstanceAsync();
    }

    [Fact]
    public async Task Should_Reject_Second_Active_Workflow()
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(WorkflowSignMode.Sequential);
        ConfigureResolution();
        var service = GetRequiredService<IWorkflowSubmissionAppService>();
        var selections = CreateFirstStepSelection(setup);
        var preview = await service.PreviewAsync(new()
        {
            DocumentId = setup.DocumentId,
            SourceFileId = setup.SourceFileId,
            WorkflowTemplateId = setup.TemplateId,
            Selections = selections
        });
        await service.SubmitAsync(new()
        {
            DocumentId = setup.DocumentId,
            SourceFileId = setup.SourceFileId,
            WorkflowTemplateId = setup.TemplateId,
            PreviewToken = preview.PreviewToken,
            DocumentConcurrencyStamp = setup.DocumentConcurrencyStamp,
            Selections = selections
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.PreviewAsync(new()
            {
                DocumentId = setup.DocumentId,
                SourceFileId = setup.SourceFileId,
                WorkflowTemplateId = setup.TemplateId,
                Selections = selections
            }));
        await WithUnitOfWorkAsync(async () =>
            (await GetRequiredService<DocumentServiceDbContext>()
                .DocumentWorkflowInstances.CountAsync())
            .ShouldBe(1));
    }

    [Fact]
    public async Task Should_Default_Null_SignMode_To_Sequential()
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(null);
        ConfigureResolution();
        var service = GetRequiredService<IWorkflowSubmissionAppService>();
        var preview = await service.PreviewAsync(new()
        {
            DocumentId = setup.DocumentId,
            SourceFileId = setup.SourceFileId,
            WorkflowTemplateId = setup.TemplateId,
            Selections = CreateFirstStepSelection(setup)
        });

        preview.SignMode.ShouldBe(WorkflowSignMode.Sequential);
    }

    [Fact]
    public async Task Should_Advance_Sequential_And_Keep_Sign_Pending()
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(WorkflowSignMode.Sequential);
        ConfigureResolution();
        var submission = GetRequiredService<IWorkflowSubmissionAppService>();
        var selections = CreateFirstStepSelection(setup);
        var preview = await submission.PreviewAsync(new()
            {
                DocumentId = setup.DocumentId,
                SourceFileId = setup.SourceFileId,
                WorkflowTemplateId = setup.TemplateId,
            Selections = selections
        });
        var instance = await submission.SubmitAsync(new()
        {
            DocumentId = setup.DocumentId,
            SourceFileId = setup.SourceFileId,
            WorkflowTemplateId = setup.TemplateId,
            PreviewToken = preview.PreviewToken,
            DocumentConcurrencyStamp = setup.DocumentConcurrencyStamp,
            Selections = selections
        });
        var first = await GetAssignmentAsync(instance.Id, setup.FirstUserId);

        using (ChangeUser(setup.FirstUserId))
        {
            await GetRequiredService<IWorkflowActionAppService>()
                .ApproveAsync(first.Id, new()
                {
                    AssignmentConcurrencyStamp = first.ConcurrencyStamp
                });
        }
        var sign = await GetAssignmentAsync(instance.Id, setup.SignUserId);
        sign.Action.ShouldBe(DocumentAssignmentAction.Sign);
        sign.IsCurrent.ShouldBeTrue();

        using (ChangeUser(setup.SignUserId))
        {
            var result = await GetRequiredService<IWorkflowActionAppService>()
                .RequestSignAsync(sign.Id, new()
                {
                    AssignmentConcurrencyStamp = sign.ConcurrencyStamp
                });
            result.Status.ShouldBe(DocumentWorkflowStatus.InProgress);
        }
        await WithUnitOfWorkAsync(async () =>
        {
            var db = GetRequiredService<DocumentServiceDbContext>();
            (await db.DocumentAssignments.SingleAsync(x => x.Id == sign.Id))
                .Status.ShouldBe(DocumentAssignmentStatus.Pending);
            (await db.DocumentWorkflowInstanceLogs.CountAsync(x =>
                x.AssignmentId == sign.Id &&
                x.Action == WorkflowRuntimeAction.RequestSign)).ShouldBe(1);
        });
    }

    [Theory]
    [InlineData(false, false, false, false, false, false)]
    [InlineData(true, false, false, false, false, false)]
    [InlineData(true, true, false, false, false, false)]
    [InlineData(true, false, true, false, false, false)]
    [InlineData(true, false, false, true, false, false)]
    [InlineData(true, false, false, false, true, false)]
    [InlineData(true, false, false, false, false, true)]
    public async Task Should_Sign_And_Idempotently_Complete_Workflow(
        bool digital,
        bool failFirst,
        bool unchangedProviderOutput,
        bool credentialExpiresDuringProviderCall,
        bool credentialRevokedDuringProviderCall,
        bool artifactSaveFailsFirst)
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(WorkflowSignMode.Sequential);
        ConfigureResolution();
        var instance = await SubmitSetupAsync(setup);
        var first = await GetAssignmentAsync(
            instance.Id, setup.FirstUserId);
        using (ChangeUser(setup.FirstUserId))
        {
            await GetRequiredService<IWorkflowActionAppService>()
                .ApproveAsync(first.Id, new()
                {
                    AssignmentConcurrencyStamp =
                        first.ConcurrencyStamp
                });
        }

        var pdfBytes = CreatePdfWithPlaceholder("<<Sign02>>");
        var imageBytes = await CreatePngAsync();
        var documentBlobs = GetRequiredService<
            IBlobContainer<DocumentBlobContainer>>();
        documentBlobs.GetAsync(
                setup.SourceBlobName,
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(
                new MemoryStream(pdfBytes, writable: false)));
        var artifactSaveCalls = 0;
        documentBlobs.SaveAsync(
                Arg.Any<string>(),
                Arg.Any<Stream>(),
                false,
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                artifactSaveCalls++;
                if (artifactSaveFailsFirst && artifactSaveCalls == 1)
                {
                    throw new BusinessException(
                        "DocumentService:FakeArtifactSaveFailure");
                }
                return Task.CompletedTask;
            });
        var signingBlobs = GetRequiredService<
            IBlobContainer<SigningBlobContainer>>();
        var assetId = Guid.NewGuid();
        var assetBlobName = $"host/signatureimage/{assetId:N}.png";
        signingBlobs.GetAsync(
                assetBlobName,
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(
                new MemoryStream(imageBytes, writable: false)));
        await WithUnitOfWorkAsync(async () =>
        {
            var db = GetRequiredService<DocumentServiceDbContext>();
            db.SigningAssets.Add(new SigningAsset(
                    assetId,
                    null,
                    SigningAssetKind.SignatureImage,
                    setup.SignUserId,
                    "signature.png",
                    assetBlobName,
                    "image/png",
                    imageBytes.LongLength,
                    Hash(imageBytes)));
            await db.SaveChangesAsync();
        });

        using var signer = ChangeUser(setup.SignUserId);
        var setting = await GetRequiredService<
                ISignatureSettingAppService>()
            .CreateAsync(new()
            {
                ProviderCode = $"electronic-{Guid.NewGuid():N}",
                ProviderType = SignatureProviderType.RemoteCa,
                ApiEndpoint = "https://sign.example.test/api",
                DefaultSignatureType = digital
                    ? SignatureType.Digital
                    : SignatureType.Electronic,
                AllowElectronicSign = !digital,
                AllowDigitalSign = digital,
                SignWidth = 120,
                SignHeight = 60,
                SignedFileSuffix = "-signed",
                IsActive = true
            });
        var signature = await GetRequiredService<
                IUserSignatureAppService>()
            .CreateAsync(new()
            {
                SignatureType = digital
                    ? SignatureType.Digital
                    : SignatureType.Electronic,
                ProviderCode = setting.ProviderCode,
                SignatureAssetId = assetId,
                TokenReference = digital ? "api-key" : null,
                Secret = digital
                    ? "MDEyMzQ1Njc4OWFiY2RlZg=="
                    : null,
                ValidToUtc = credentialExpiresDuringProviderCall
                    ? DateTime.UtcNow.AddSeconds(3)
                    : null,
                IsActive = true
            });
        var assignment = await GetAssignmentAsync(
            instance.Id, setup.SignUserId);
        await GetRequiredService<IWorkflowActionAppService>()
            .RequestSignAsync(assignment.Id, new()
            {
                AssignmentConcurrencyStamp =
                    assignment.ConcurrencyStamp
            });
        var execution = GetRequiredService<
            ISigningExecutionAppService>();
        var input = new DigitalSignInput
        {
            SourceFileId = setup.SourceFileId,
            UserSignatureId = signature.Id,
            AssignmentConcurrencyStamp =
                assignment.ConcurrencyStamp,
            Comment = "Approved"
        };
        if (!digital)
        {
            var substitutedSourceId = Guid.NewGuid();
            await WithUnitOfWorkAsync(async () =>
            {
                var db = GetRequiredService<DocumentServiceDbContext>();
                db.DocumentFiles.Add(new DocumentFile(
                    substitutedSourceId,
                    null,
                    setup.DocumentId,
                    "substituted.pdf",
                    $"documents/{substitutedSourceId:N}.pdf",
                    "application/pdf",
                    pdfBytes.LongLength,
                    Hash(pdfBytes)));
                await db.SaveChangesAsync();
            });
            var substituted = new ElectronicSignInput
            {
                SourceFileId = substitutedSourceId,
                UserSignatureId = input.UserSignatureId,
                AssignmentConcurrencyStamp =
                    input.AssignmentConcurrencyStamp
            };
            var exception = await Should.ThrowAsync<BusinessException>(() =>
                execution.ExecuteElectronicAsync(
                    assignment.Id, substituted));
            exception.Code.ShouldBe(
                "DocumentService:SigningCanonicalSourceChanged");
        }

        var remoteCa =
            GetRequiredService<IRemoteCaSigningProvider>();
        var providerPdfBytes = unchangedProviderOutput
            ? pdfBytes
            : CreatePdfWithPlaceholder("REMOTE-SIGNED");
        var providerCalls = 0;
        remoteCa.SignAsync(
                Arg.Any<RemoteCaSigningCommand>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                GetRequiredService<IUnitOfWorkManager>()
                    .Current.ShouldBeNull();
                providerCalls++;
                if (failFirst && providerCalls == 1)
                {
                    throw new BusinessException(
                        "DocumentService:FakeProviderFailure");
                }
                if (credentialExpiresDuringProviderCall)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3.2));
                }
                if (credentialRevokedDuringProviderCall)
                {
                    await GetRequiredService<IUserSignatureAppService>()
                        .RevokeCredentialAsync(
                            signature.Id,
                            signature.ConcurrencyStamp);
                }
                return providerPdfBytes;
            });
        if (failFirst || artifactSaveFailsFirst)
        {
            await Should.ThrowAsync<BusinessException>(() =>
                execution.ExecuteDigitalAsync(assignment.Id, input));
            await WithUnitOfWorkAsync(async () =>
            {
                var db = GetRequiredService<DocumentServiceDbContext>();
                var failedAttempt =
                    await db.SigningAttempts.SingleAsync(x =>
                        x.AssignmentId == assignment.Id);
                failedAttempt.Status.ShouldBe(
                    SigningAttemptStatus.Failed);
                failedAttempt.PendingResultFileId.ShouldBeNull();
                failedAttempt.PendingResultBlobName.ShouldBeNull();
                (await db.DocumentAssignments.SingleAsync(x =>
                        x.Id == assignment.Id))
                    .Status.ShouldBe(
                        DocumentAssignmentStatus.Pending);
            });
        }
        if (unchangedProviderOutput)
        {
            var exception = await Should.ThrowAsync<BusinessException>(() =>
                execution.ExecuteDigitalAsync(assignment.Id, input));
            exception.Code.ShouldBe(
                "DocumentService:UnchangedProviderSigningOutput");
            await WithUnitOfWorkAsync(async () =>
            {
                var db = GetRequiredService<DocumentServiceDbContext>();
                var failedAttempt =
                    await db.SigningAttempts.SingleAsync(x =>
                        x.AssignmentId == assignment.Id);
                failedAttempt.Status.ShouldBe(
                    SigningAttemptStatus.Failed);
                failedAttempt.PendingResultFileId.ShouldBeNull();
                failedAttempt.PendingResultBlobName.ShouldBeNull();
                (await db.DocumentAssignments.SingleAsync(x =>
                        x.Id == assignment.Id))
                    .Status.ShouldBe(DocumentAssignmentStatus.Pending);
            });
            await remoteCa.Received(1).SignAsync(
                Arg.Any<RemoteCaSigningCommand>(),
                Arg.Any<CancellationToken>());
            return;
        }
        if (credentialExpiresDuringProviderCall ||
            credentialRevokedDuringProviderCall)
        {
            var exception = await Should.ThrowAsync<BusinessException>(() =>
                execution.ExecuteDigitalAsync(assignment.Id, input));
            exception.Code.ShouldBe(
                "DocumentService:SigningCredentialChanged");
            await WithUnitOfWorkAsync(async () =>
            {
                var db = GetRequiredService<DocumentServiceDbContext>();
                var failedAttempt =
                    await db.SigningAttempts.SingleAsync(x =>
                        x.AssignmentId == assignment.Id);
                failedAttempt.Status.ShouldBe(
                    SigningAttemptStatus.Failed);
                failedAttempt.PendingResultFileId.ShouldBeNull();
                failedAttempt.PendingResultBlobName.ShouldBeNull();
                (await db.DocumentAssignments.SingleAsync(x =>
                        x.Id == assignment.Id))
                    .Status.ShouldBe(DocumentAssignmentStatus.Pending);
            });
            return;
        }
        var completed = digital
            ? await execution.ExecuteDigitalAsync(assignment.Id, input)
            : await execution.ExecuteElectronicAsync(
                assignment.Id, input);
        var replay = digital
            ? await execution.ExecuteDigitalAsync(assignment.Id, input)
            : await execution.ExecuteElectronicAsync(
                assignment.Id, input);

            completed.Status.ShouldBe(SigningAttemptStatus.Succeeded);
        replay.Id.ShouldBe(completed.Id);
        completed.ResultFileId.ShouldNotBeNull();
        await WithUnitOfWorkAsync(async () =>
        {
            var db = GetRequiredService<DocumentServiceDbContext>();
            var result = await db.DocumentFiles.SingleAsync(
                x => x.Id == completed.ResultFileId);
            result.IsSigned.ShouldBeTrue();
            result.SourceFileId.ShouldBe(setup.SourceFileId);
            result.MimeType.ShouldBe("application/pdf");
            result.Size.ShouldBeGreaterThan(0);
            result.Hash.ShouldNotBeNullOrWhiteSpace();
            result.BlobName.ShouldEndWith(".pdf");
            var attempt = await db.SigningAttempts.SingleAsync(
                x => x.Id == completed.Id);
            attempt.PendingResultFileId.ShouldBeNull();
            attempt.PendingResultBlobName.ShouldBeNull();
            attempt.AttemptCount.ShouldBe(
                failFirst || artifactSaveFailsFirst ? 2 : 1);
            (await db.DocumentAssignments.SingleAsync(
                    x => x.Id == assignment.Id))
                .Status.ShouldBe(DocumentAssignmentStatus.Done);
            var storedInstance =
                await db.DocumentWorkflowInstances.SingleAsync(
                    x => x.Id == instance.Id);
            storedInstance.Status.ShouldBe(
                DocumentWorkflowStatus.Completed);
            storedInstance.CurrentSignedFileId.ShouldBe(result.Id);
            (await db.DocumentWorkflowInstanceLogs.CountAsync(x =>
                x.AssignmentId == assignment.Id &&
                x.Action == WorkflowRuntimeAction.ConfirmSign))
                .ShouldBe(1);
        });
        if (digital)
        {
            await remoteCa.Received(
                    failFirst || artifactSaveFailsFirst ? 2 : 1)
                .SignAsync(
                Arg.Is<RemoteCaSigningCommand>(x =>
                    x.AttemptId == completed.Id &&
                    x.ProviderCode == setting.ProviderCode &&
                    x.ApiKey == "api-key" &&
                    x.Placeholder == "<<Sign02>>" &&
                    x.Page == 1),
                Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task Should_Return_When_Snapshot_Allows_It()
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(
            WorkflowSignMode.Sequential,
            allowReturn: true);
        ConfigureResolution();
        var instance = await SubmitSetupAsync(setup);
        var assignment = await GetAssignmentAsync(instance.Id, setup.FirstUserId);

        using (ChangeUser(setup.FirstUserId))
        {
            var returned = await GetRequiredService<IWorkflowActionAppService>()
                .ReturnAsync(assignment.Id, new()
                {
                    AssignmentConcurrencyStamp = assignment.ConcurrencyStamp,
                    Comment = "Needs correction"
                });
            returned.Status.ShouldBe(DocumentWorkflowStatus.Returned);
            returned.CurrentCommittedStepId.ShouldBeNull();
        }
        await WithUnitOfWorkAsync(async () =>
            (await GetRequiredService<DocumentServiceDbContext>()
                .Documents.SingleAsync(x => x.Id == setup.DocumentId))
            .CurrentStatus.ShouldBe("WORKFLOW_RETURNED"));

        string returnedDocumentStamp = string.Empty;
        await WithUnitOfWorkAsync(async () =>
            returnedDocumentStamp = (await GetRequiredService<DocumentServiceDbContext>()
                .Documents.AsNoTracking()
                .SingleAsync(x => x.Id == setup.DocumentId))
            .ConcurrencyStamp);
        var submission = GetRequiredService<IWorkflowSubmissionAppService>();
        var selections = CreateFirstStepSelection(setup);
        var resubmitPreview = await submission.PreviewAsync(new()
        {
            DocumentId = setup.DocumentId,
            SourceFileId = setup.SourceFileId,
            WorkflowTemplateId = setup.TemplateId,
            PreviousInstanceId = instance.Id,
            Selections = selections
        });
        var resubmitted = await submission.SubmitAsync(new()
        {
            DocumentId = setup.DocumentId,
            SourceFileId = setup.SourceFileId,
            WorkflowTemplateId = setup.TemplateId,
            PreviousInstanceId = instance.Id,
            PreviewToken = resubmitPreview.PreviewToken,
            DocumentConcurrencyStamp = returnedDocumentStamp,
            Selections = selections
        });
        resubmitted.PreviousInstanceId.ShouldBe(instance.Id);
        await WithUnitOfWorkAsync(async () =>
        {
            var db = GetRequiredService<DocumentServiceDbContext>();
            (await db.DocumentWorkflowInstances.CountAsync(x =>
                x.DocumentId == setup.DocumentId)).ShouldBe(2);
            var persisted = await db.DocumentWorkflowInstances
                .AsNoTracking()
                .Where(x => x.DocumentId == setup.DocumentId)
                .ToListAsync();
            persisted.Single(x => x.Id == instance.Id)
                .SourceFileId.ShouldBe(setup.SourceFileId);
            persisted.Single(x => x.Id == resubmitted.Id)
                .SourceFileId.ShouldBe(setup.SourceFileId);
            (await db.DocumentWorkflowInstanceLogs.CountAsync(x =>
                x.InstanceId == resubmitted.Id &&
                x.Action == WorkflowRuntimeAction.Resubmit)).ShouldBe(1);
        });
    }

    [Fact]
    public async Task Should_Allow_Initiator_To_Cancel_Before_Signing()
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(WorkflowSignMode.Parallel);
        ConfigureResolution();
        var instance = await SubmitSetupAsync(setup);

        var cancelled = await GetRequiredService<IWorkflowActionAppService>()
            .CancelAsync(instance.Id, new()
            {
                InstanceConcurrencyStamp = instance.ConcurrencyStamp
            });
        cancelled.Status.ShouldBe(DocumentWorkflowStatus.Cancelled);
        var revokedSign = await GetAssignmentAsync(
            instance.Id,
            setup.SignUserId);
        using (ChangeUser(setup.SignUserId))
        {
            await Should.ThrowAsync<UserFriendlyException>(() =>
                GetRequiredService<IWorkflowActionAppService>()
                    .RequestSignAsync(revokedSign.Id, new()
                    {
                        AssignmentConcurrencyStamp =
                            revokedSign.ConcurrencyStamp
                    }));
        }
        using (ChangeUser(Guid.NewGuid()))
        {
            await Should.ThrowAsync<Volo.Abp.Authorization.AbpAuthorizationException>(
                () => GetRequiredService<IWorkflowActionAppService>()
                    .CancelAsync(instance.Id, new()
                    {
                        InstanceConcurrencyStamp =
                            cancelled.ConcurrencyStamp
                    }));
        }
        await WithUnitOfWorkAsync(async () =>
        {
            var db = GetRequiredService<DocumentServiceDbContext>();
            var assignments = await db.DocumentAssignments
                .Where(x => x.InstanceId == instance.Id)
                .ToListAsync();
            assignments.ShouldAllBe(x =>
                x.Status == DocumentAssignmentStatus.Revoked && !x.IsCurrent);
            (await db.DocumentWorkflowInstanceLogs.CountAsync(x =>
                x.InstanceId == instance.Id &&
                x.Action == WorkflowRuntimeAction.RequestSign)).ShouldBe(0);
        });
    }

    [Fact]
    public async Task Should_Complete_Sequential_After_Final_Process_Approval()
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(
            WorkflowSignMode.Sequential,
            secondStepType: WorkflowStepType.Process);
        ConfigureResolution();
        var instance = await SubmitSetupAsync(setup);
        var first = await GetAssignmentAsync(instance.Id, setup.FirstUserId);
        using (ChangeUser(setup.FirstUserId))
        {
            await GetRequiredService<IWorkflowActionAppService>()
                .ApproveAsync(first.Id, new()
                {
                    AssignmentConcurrencyStamp = first.ConcurrencyStamp
                });
        }
        var second = await GetAssignmentAsync(instance.Id, setup.SignUserId);
        using (ChangeUser(setup.SignUserId))
        {
            var completed = await GetRequiredService<IWorkflowActionAppService>()
                .ApproveAsync(second.Id, new()
                {
                    AssignmentConcurrencyStamp = second.ConcurrencyStamp
                });
            completed.Status.ShouldBe(DocumentWorkflowStatus.Completed);
            completed.CurrentCommittedStepId.ShouldBeNull();
        }
        await WithUnitOfWorkAsync(async () =>
        {
            var db = GetRequiredService<DocumentServiceDbContext>();
            (await db.Documents.SingleAsync(x => x.Id == setup.DocumentId))
                .CurrentStatus.ShouldBe("WORKFLOW_COMPLETED");
            (await db.DocumentWorkflowInstanceLogs.CountAsync(x =>
                x.InstanceId == instance.Id &&
                x.Action == WorkflowRuntimeAction.Complete)).ShouldBe(1);
        });
    }

    [Fact]
    public async Task Should_Reject_And_Revoke_Parallel_Siblings()
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(WorkflowSignMode.Parallel);
        ConfigureResolution();
        var instance = await SubmitSetupAsync(setup);
        var assignment = await GetAssignmentAsync(instance.Id, setup.FirstUserId);

        using (ChangeUser(setup.FirstUserId))
        {
            var rejected = await GetRequiredService<IWorkflowActionAppService>()
                .RejectAsync(assignment.Id, new()
                {
                    AssignmentConcurrencyStamp = assignment.ConcurrencyStamp,
                    Comment = "Not approved"
                });
            rejected.Status.ShouldBe(DocumentWorkflowStatus.Rejected);
        }
        await WithUnitOfWorkAsync(async () =>
        {
            var db = GetRequiredService<DocumentServiceDbContext>();
            var assignments = await db.DocumentAssignments
                .Where(x => x.InstanceId == instance.Id)
                .ToListAsync();
            assignments.Single(x => x.Id == assignment.Id)
                .Status.ShouldBe(DocumentAssignmentStatus.Rejected);
            assignments.Single(x => x.Id != assignment.Id)
                .Status.ShouldBe(DocumentAssignmentStatus.Revoked);
            (await db.Documents.SingleAsync(x => x.Id == setup.DocumentId))
                .CurrentStatus.ShouldBe("WORKFLOW_REJECTED");
            (await db.DocumentWorkflowInstanceLogs.CountAsync(x =>
                x.InstanceId == instance.Id &&
                x.Action == WorkflowRuntimeAction.Reject)).ShouldBe(1);
        });
    }

    [Fact]
    public async Task Should_Replace_Current_Signer_From_Committed_Candidates()
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(
            WorkflowSignMode.Sequential,
            multipleSigners: true);
        ConfigureResolution();
        var instance = await SubmitSetupAsync(setup);
        var first = await GetAssignmentAsync(instance.Id, setup.FirstUserId);
        using (ChangeUser(setup.FirstUserId))
        {
            await GetRequiredService<IWorkflowActionAppService>()
                .ApproveAsync(first.Id, new()
                {
                    AssignmentConcurrencyStamp = first.ConcurrencyStamp
                });
        }
        var sign = await GetAssignmentAsync(instance.Id, setup.SignUserId);

        await GetRequiredService<IWorkflowActionAppService>()
            .ReplaceSignerAsync(sign.Id, new()
            {
                NewSignerUserId = setup.AlternateSignUserId,
                AssignmentConcurrencyStamp = sign.ConcurrencyStamp,
                Comment = "Signer unavailable"
            });
        await WithUnitOfWorkAsync(async () =>
        {
            var db = GetRequiredService<DocumentServiceDbContext>();
            (await db.DocumentAssignments.SingleAsync(x => x.Id == sign.Id))
                .ReceiverUserId.ShouldBe(setup.AlternateSignUserId);
            var history = await db.DocumentHistories.SingleAsync(x =>
                x.InstanceId == instance.Id &&
                x.Action == WorkflowRuntimeAction.UpdateSigner);
            history.FromUserId.ShouldBe(setup.SignUserId);
            history.ToUserId.ShouldBe(setup.AlternateSignUserId);
        });
    }

    [Fact]
    public async Task Should_Unlock_Sequential_View_Scope_When_Reached()
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(
            WorkflowSignMode.Sequential,
            includeViewStep: true);
        ConfigureResolution();
        var instance = await SubmitSetupAsync(setup);
        var query = GetRequiredService<IWorkflowRuntimeQueryAppService>();
        using (ChangeUser(setup.ViewUserId))
        {
            await Should.ThrowAsync<Volo.Abp.Authorization.AbpAuthorizationException>(
                () => query.GetAsync(instance.Id));
        }

        var first = await GetAssignmentAsync(instance.Id, setup.FirstUserId);
        using (ChangeUser(setup.FirstUserId))
        {
            await GetRequiredService<IWorkflowActionAppService>()
                .ApproveAsync(first.Id, new()
                {
                    AssignmentConcurrencyStamp = first.ConcurrencyStamp
                });
        }
        using (ChangeUser(setup.ViewUserId))
        {
            var status = await query.GetAsync(instance.Id);
            status.Steps.Single(x => x.Type == WorkflowStepType.View)
                .IsViewUnlocked.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Should_Fail_Closed_When_Replacement_Signer_Is_Disabled()
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(
            WorkflowSignMode.Sequential,
            multipleSigners: true);
        ConfigureResolution();
        var instance = await SubmitSetupAsync(setup);
        var first = await GetAssignmentAsync(instance.Id, setup.FirstUserId);
        using (ChangeUser(setup.FirstUserId))
        {
            await GetRequiredService<IWorkflowActionAppService>()
                .ApproveAsync(first.Id, new()
                {
                    AssignmentConcurrencyStamp = first.ConcurrencyStamp
                });
        }
        var sign = await GetAssignmentAsync(instance.Id, setup.SignUserId);
        GetRequiredService<IWorkflowIdentityReferenceValidator>()
            .ValidateAsync(
                Arg.Is<IEnumerable<Guid>>(ids =>
                    ids.Contains(setup.AlternateSignUserId)),
                Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new UserFriendlyException(
                "Replacement signer is disabled.")));

        await Should.ThrowAsync<UserFriendlyException>(() =>
            GetRequiredService<IWorkflowActionAppService>()
                .ReplaceSignerAsync(sign.Id, new()
                {
                    NewSignerUserId = setup.AlternateSignUserId,
                    AssignmentConcurrencyStamp = sign.ConcurrencyStamp
                }));
        await WithUnitOfWorkAsync(async () =>
        {
            var db = GetRequiredService<DocumentServiceDbContext>();
            (await db.DocumentAssignments.SingleAsync(x => x.Id == sign.Id))
                .ReceiverUserId.ShouldBe(setup.SignUserId);
            (await db.DocumentWorkflowInstanceLogs.CountAsync(x =>
                x.InstanceId == instance.Id &&
                x.Action == WorkflowRuntimeAction.UpdateSigner)).ShouldBe(0);
        });
    }

    [Fact]
    public async Task Should_Mark_Overdue_And_Extend_With_Audit()
    {
        var callerId = Guid.NewGuid();
        using var principal = ChangeUser(callerId);
        var setup = await CreateSetupAsync(
            WorkflowSignMode.Sequential,
            slaDays: 0);
        ConfigureResolution();
        var instance = await SubmitSetupAsync(setup);
        instance.DeadlineAtUtc.ShouldNotBeNull();
        var actions = GetRequiredService<IWorkflowActionAppService>();
        var overdue = await actions.MarkOverdueAsync(instance.Id, new()
        {
            InstanceConcurrencyStamp = instance.ConcurrencyStamp
        });
        overdue.Status.ShouldBe(DocumentWorkflowStatus.Overdue);
        overdue.OverdueAtUtc.ShouldNotBeNull();

        var extended = await actions.ExtendAsync(instance.Id, new()
        {
            InstanceConcurrencyStamp = overdue.ConcurrencyStamp,
            BusinessDays = 2,
            Reason = "Waiting for external records"
        });
        extended.Status.ShouldBe(DocumentWorkflowStatus.InProgress);
        extended.OverdueAtUtc.ShouldBeNull();
        extended.ExtensionCount.ShouldBe(1);
        extended.TotalExtensionBusinessDays.ShouldBe(2);
        extended.DeadlineAtUtc!.Value.ShouldBeGreaterThan(DateTime.UtcNow);
        await WithUnitOfWorkAsync(async () =>
        {
            var db = GetRequiredService<DocumentServiceDbContext>();
            (await db.Documents.SingleAsync(x => x.Id == setup.DocumentId))
                .CurrentStatus.ShouldBe("WORKFLOW_IN_PROGRESS");
            (await db.DocumentWorkflowInstanceLogs.CountAsync(x =>
                x.InstanceId == instance.Id &&
                (x.Action == WorkflowRuntimeAction.MarkOverdue ||
                 x.Action == WorkflowRuntimeAction.Extend))).ShouldBe(2);
        });
    }

    private async Task<DocumentWorkflowInstanceDto> SubmitSetupAsync(
        SubmissionSetup setup)
    {
        var submission = GetRequiredService<IWorkflowSubmissionAppService>();
        var selections = CreateFirstStepSelection(setup);
        var preview = await submission.PreviewAsync(new()
        {
            DocumentId = setup.DocumentId,
            SourceFileId = setup.SourceFileId,
            WorkflowTemplateId = setup.TemplateId,
            Selections = selections
        });
        return await submission.SubmitAsync(new()
        {
            DocumentId = setup.DocumentId,
            SourceFileId = setup.SourceFileId,
            WorkflowTemplateId = setup.TemplateId,
            PreviewToken = preview.PreviewToken,
            DocumentConcurrencyStamp = setup.DocumentConcurrencyStamp,
            Selections = selections
        });
    }

    private async Task<DocumentAssignment> GetAssignmentAsync(
        Guid instanceId,
        Guid receiverUserId)
    {
        DocumentAssignment? result = null;
        await WithUnitOfWorkAsync(async () =>
        {
            result = await GetRequiredService<DocumentServiceDbContext>()
                .DocumentAssignments
                .AsNoTracking()
                .SingleAsync(x =>
                    x.InstanceId == instanceId &&
                    x.ReceiverUserId == receiverUserId);
        });
        return result!;
    }

    private static List<WorkflowSubmitSelectionDto> CreateFirstStepSelection(
        SubmissionSetup setup) =>
    [
        new()
        {
            WorkflowStepTemplateId = setup.FirstStepId,
            UserId = setup.FirstUserId
        },
        new()
        {
            WorkflowStepTemplateId = setup.SignStepId,
            UserId = setup.SignUserId
        }
    ];

    private async Task AssertNoWorkflowInstanceAsync()
    {
        await WithUnitOfWorkAsync(async () =>
            (await GetRequiredService<DocumentServiceDbContext>()
                .DocumentWorkflowInstances.CountAsync())
            .ShouldBe(0));
    }

    private void ConfigureResolution(Guid? replacementUserId = null)
    {
        var resolver = GetRequiredService<IWorkflowAssigneeResolver>();
        resolver.ResolveAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyCollection<WorkflowStepAssignmentConfiguration>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var configs = call.ArgAt<
                    IReadOnlyCollection<WorkflowStepAssignmentConfiguration>>(1);
                var result = new WorkflowAssigneeResolutionResult();
                foreach (var config in configs)
                {
                    foreach (var userId in config.Users.Select(x =>
                                 replacementUserId ?? x.UserId))
                    {
                        result.Candidates.Add(new WorkflowResolvedCandidate
                        {
                            ConfigurationId = config.Id,
                            UserId = userId,
                            DisplayName = $"User {userId:N}",
                            IsPrimaryConfiguration = config.IsPrimary,
                            ConfigurationCreationTime = config.CreationTime
                        });
                    }
                }
                return Task.FromResult(result);
            });
    }

    private IDisposable ChangeUser(Guid userId)
    {
        var accessor = GetRequiredService<ICurrentPrincipalAccessor>();
        return accessor.Change(new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(AbpClaimTypes.UserId, userId.ToString()),
                new Claim(AbpClaimTypes.UserName, $"user-{userId:N}")
            ],
            "Test")));
    }

    private async Task<SubmissionSetup> CreateSetupAsync(
        WorkflowSignMode? signMode,
        bool allowReturn = false,
        WorkflowStepType secondStepType = WorkflowStepType.Sign,
        bool multipleSigners = false,
        bool includeViewStep = false,
        int? slaDays = null)
    {
        var currentUserId = GetRequiredService<ICurrentPrincipalAccessor>()
            .Principal.FindFirst(AbpClaimTypes.UserId)!.Value;
        var parsedCurrentUserId = Guid.Parse(currentUserId);
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var signUserId = Guid.NewGuid();
        var alternateSignUserId = Guid.NewGuid();
        var viewUserId = Guid.NewGuid();
        var definition = await GetRequiredService<IWorkflowDefinitionAppService>()
            .CreateAsync(new()
            {
                Code = $"DEF-{Guid.NewGuid():N}",
                Name = "Definition",
                IsActive = true
            });
        var workflow = await GetRequiredService<IWorkflowAppService>()
            .CreateAsync(new()
            {
                Code = $"WF-{Guid.NewGuid():N}",
                Name = "Workflow",
                WorkflowDefinitionId = definition.Id,
                IsActive = true
            });
        var template = await GetRequiredService<IWorkflowTemplateAppService>()
            .CreateAsync(new()
            {
                Code = $"TPL-{Guid.NewGuid():N}",
                Name = "Template",
                WorkflowId = workflow.Id,
                SignMode = signMode,
                IsActive = true
            });
        var stepService = GetRequiredService<IWorkflowStepTemplateAppService>();
        var firstStep = await stepService.CreateAsync(new()
        {
            Order = 1,
            Name = "Process",
            Type = WorkflowStepType.Process,
            WorkflowTemplateId = template.Id,
            AllowReturn = allowReturn,
            SlaDays = slaDays,
            IsActive = true
        });
        WorkflowStepTemplateDto? viewStep = null;
        if (includeViewStep)
        {
            viewStep = await stepService.CreateAsync(new()
            {
                Order = 2,
                Name = "View",
                Type = WorkflowStepType.View,
                WorkflowTemplateId = template.Id,
                IsActive = true
            });
        }
        var signStep = await stepService.CreateAsync(new()
        {
            Order = includeViewStep ? 3 : 2,
            Name = secondStepType.ToString(),
            Type = secondStepType,
            WorkflowTemplateId = template.Id,
            IsActive = true
        });
        var configService =
            GetRequiredService<IWorkflowStepAssignmentConfigurationAppService>();
        await configService.CreateAsync(new()
        {
            WorkflowStepTemplateId = firstStep.Id,
            AssigneeType = WorkflowAssigneeType.SpecificUser,
            UserIds = [firstUserId, secondUserId],
            IsPrimary = true,
            IsActive = true
        });
        if (viewStep is not null)
        {
            await configService.CreateAsync(new()
            {
                WorkflowStepTemplateId = viewStep.Id,
                AssigneeType = WorkflowAssigneeType.SpecificUser,
                UserIds = [viewUserId],
                IsPrimary = true,
                IsActive = true
            });
        }
        await configService.CreateAsync(new()
        {
            WorkflowStepTemplateId = signStep.Id,
            AssigneeType = WorkflowAssigneeType.SpecificUser,
            UserIds = multipleSigners
                ? [signUserId, alternateSignUserId]
                : [signUserId],
            IsPrimary = true,
            IsActive = true
        });

        var documentId = Guid.NewGuid();
        var sourceFileId = Guid.NewGuid();
        var sourceBlobName = $"documents/{documentId:N}.pdf";
        var sourceBytes = CreatePdfWithPlaceholder("<<Sign02>>");
        string stamp = string.Empty;
        await WithUnitOfWorkAsync(async () =>
        {
            var db = GetRequiredService<DocumentServiceDbContext>();
            var document = new hanhchinhso.DocumentService.Documents.Document(
                documentId,
                null,
                new CreateUpdateDocumentDto
                {
                    Title = "Document",
                    StorageNumber = $"ST-{Guid.NewGuid():N}",
                    IncomingDate = DateTime.UtcNow,
                    SourceType = DocumentSourceType.Workflow
                },
                parsedCurrentUserId);
            db.Documents.Add(document);
            db.DocumentFiles.Add(new DocumentFile(
                sourceFileId,
                null,
                documentId,
                "document.pdf",
                sourceBlobName,
                "application/pdf",
                sourceBytes.LongLength,
                Hash(sourceBytes)));
            await db.SaveChangesAsync();
            stamp = document.ConcurrencyStamp;
        });
        GetRequiredService<IBlobContainer<DocumentBlobContainer>>()
            .GetAsync(
                sourceBlobName,
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(
                new MemoryStream(sourceBytes, writable: false)));
        return new SubmissionSetup(
            documentId,
            stamp,
            template.Id,
            firstStep.Id,
            signStep.Id,
            firstUserId,
            secondUserId,
            signUserId,
            alternateSignUserId,
            viewUserId,
            sourceFileId,
            sourceBlobName);
    }

    private sealed record SubmissionSetup(
        Guid DocumentId,
        string DocumentConcurrencyStamp,
        Guid TemplateId,
        Guid FirstStepId,
        Guid SignStepId,
        Guid FirstUserId,
        Guid SecondUserId,
        Guid SignUserId,
        Guid AlternateSignUserId,
        Guid ViewUserId,
        Guid SourceFileId,
        string SourceBlobName);

    private static async Task<byte[]> CreatePngAsync()
    {
        await using var stream = new MemoryStream();
        using var image = new Image<Rgba32>(20, 10);
        await image.SaveAsPngAsync(stream);
        return stream.ToArray();
    }

    private static string Hash(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static byte[] CreatePdfWithPlaceholder(string placeholder)
    {
        var content =
            $"BT /F1 12 Tf 72 720 Td ({placeholder}) Tj ET";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] " +
            "/Resources << /Font << /F1 5 0 R >> >> " +
            "/Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>" +
            $"\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };
        using var stream = new MemoryStream();
        void Write(string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            stream.Write(bytes);
        }
        Write("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(stream.Position);
            Write($"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }
        var xref = stream.Position;
        Write($"xref\n0 {objects.Length + 1}\n");
        Write("0000000000 65535 f \n");
        for (var index = 1; index < offsets.Count; index++)
        {
            Write($"{offsets[index]:D10} 00000 n \n");
        }
        Write($"trailer << /Size {objects.Length + 1} " +
              "/Root 1 0 R >>\n");
        Write($"startxref\n{xref}\n%%EOF");
        return stream.ToArray();
    }
}
