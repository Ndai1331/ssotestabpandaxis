using HCS.DocumentService.Documents;

namespace HCS.DocumentService.Tests;

public sealed class DocumentAggregateTests
{
    private static readonly DateTime Now = new(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc);
    private static DocumentAggregate Create() => new(Guid.NewGuid(), "CV-001", "Công văn", null, Guid.NewGuid(), Now);

    [Fact]
    public void Classification_is_stored_until_the_document_is_immutable()
    {
        var document = Create();
        var typeId = Guid.NewGuid();
        document.Classify(typeId, null, null, null, null, Now);
        Assert.Equal(typeId, document.DocumentTypeId);
        document.AddFile(Guid.NewGuid(), "a.pdf", "application/pdf", 10, new string('a', 64), "documents/a", null, Now);
        document.Submit(null, Now);
        document.StartReview(null, Now);
        document.CompleteReview(true, null, null, Now);
        Assert.Throws<InvalidOperationException>(() => document.Classify(Guid.NewGuid(), null, null, null, null, Now));
    }

    [Fact]
    public void Submission_requires_at_least_one_file()
    {
        var document = Create();
        var error = Assert.Throws<InvalidOperationException>(() => document.Submit(Guid.NewGuid(), Now));
        Assert.Contains("at least one file", error.Message);
    }

    [Fact]
    public void Duplicate_content_is_rejected_and_approved_document_is_immutable()
    {
        var document = Create();
        var hash = new string('a', 64);
        document.AddFile(Guid.NewGuid(), "a.pdf", "application/pdf", 10, hash, "documents/a", null, Now);
        Assert.Throws<InvalidOperationException>(() => document.AddFile(Guid.NewGuid(), "copy.pdf", "application/pdf", 10, hash, "documents/b", null, Now));
        document.Submit(null, Now);
        document.StartReview(null, Now);
        document.CompleteReview(true, null, null, Now);
        Assert.Throws<InvalidOperationException>(() => document.Update("Changed", null, null, Now));
    }

    [Fact]
    public void Assignment_is_idempotent_for_same_user_and_responsibility()
    {
        var document = Create();
        var user = Guid.NewGuid();
        document.Assign(Guid.NewGuid(), user, "approver", null, Now);
        document.Assign(Guid.NewGuid(), user, "approver", null, Now);
        Assert.Single(document.Assignments);
    }

    [Fact]
    public void File_deletion_is_retriable_in_two_phases()
    {
        var document = Create();
        var file = document.AddFile(Guid.NewGuid(), "a.pdf", "application/pdf", 10, new string('a', 64), "documents/a", null, Now);
        Assert.Same(file, document.BeginFileDeletion(file.Id, null, Now));
        Assert.Same(file, document.BeginFileDeletion(file.Id, null, Now));
        Assert.True(file.IsPendingDeletion);
        document.CompleteFileDeletion(file.Id, null, Now);
        Assert.Empty(document.Files);
    }

    [Fact]
    public void Send_creates_view_assignment_and_revoke_hides_inbox()
    {
        var document = Create();
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();
        document.Send(to, null, from, Now);
        Assert.Equal(from, document.FromUserId);
        Assert.Contains(document.Assignments, a => a.AssigneeUserId == to && a.Responsibility == "VIEW" && a.IsCurrent);
        document.RevokeInbox(from, Now);
        Assert.All(document.Assignments.Where(a => a.Responsibility == "VIEW"), a => Assert.False(a.IsCurrent));
        Assert.False(DocumentSendState.IsActivelySent(document.History));
        document.Send(to, null, from, Now.AddMinutes(1));
        Assert.True(DocumentSendState.IsActivelySent(document.History));
    }

    [Fact]
    public void Record_access_is_allowed_after_approval()
    {
        var document = Create();
        var actor = Guid.NewGuid();
        document.AddFile(Guid.NewGuid(), "a.pdf", "application/pdf", 10, new string('a', 64), "documents/a", null, Now);
        document.Submit(null, Now);
        document.StartReview(null, Now);
        document.CompleteReview(true, null, null, Now);
        document.RecordAccess("Viewed", actor, Now);
        document.RecordAccess("printed", actor, Now.AddSeconds(1));
        document.RecordAccess("DOWNLOADED", actor, Now.AddSeconds(2));
        Assert.Contains(document.History, x => x.Action == "Viewed" && x.ActorUserId == actor);
        Assert.Contains(document.History, x => x.Action == "Printed" && x.ActorUserId == actor);
        Assert.Contains(document.History, x => x.Action == "Downloaded" && x.ActorUserId == actor);
        Assert.Throws<ArgumentException>(() => document.RecordAccess("Edited", actor, Now));
    }

    [Fact]
    public void Duplicate_as_workflow_keeps_parent_and_classification()
    {
        var document = Create();
        var typeId = Guid.NewGuid();
        document.Classify(typeId, null, null, null, null, Now);
        var copy = document.DuplicateAsWorkflow(Guid.NewGuid(), "CV-001-WF", null, Now);
        Assert.Equal(DocumentSourceType.Workflow, copy.SourceType);
        Assert.Equal(document.Id, copy.ParentDocumentId);
        Assert.Equal(typeId, copy.DocumentTypeId);
        Assert.Null(copy.DocumentCode);
        Assert.NotEqual(document.Id, copy.Id);
    }

    [Fact]
    public void Document_code_is_optional_and_copied_for_workflow()
    {
        var document = Create();
        document.SetDocumentCode("QD-1551");
        Assert.Equal("QD-1551", document.DocumentCode);
        document.SetDocumentCode("  ");
        Assert.Null(document.DocumentCode);
        document.SetDocumentCode("CV-12");
        var copy = document.DuplicateAsWorkflow(Guid.NewGuid(), "CV-12-WF", null, Now);
        Assert.Equal("CV-12", copy.DocumentCode);
    }

    [Fact]
    public void Issuing_unit_uses_existing_organization_unit_and_is_copied_for_workflow()
    {
        var document = Create();
        var unitId = Guid.NewGuid();
        document.SetOrganizationUnit(unitId);
        Assert.Equal(unitId, document.OrganizationUnitId);
        document.SetOrganizationUnit(Guid.Empty);
        Assert.Null(document.OrganizationUnitId);
        document.SetOrganizationUnit(unitId);
        var copy = document.DuplicateAsWorkflow(Guid.NewGuid(), "CV-13-WF", null, Now);
        Assert.Equal(unitId, copy.OrganizationUnitId);
    }

    [Fact]
    public void Send_does_not_overwrite_issuing_unit()
    {
        var document = Create();
        var unitId = Guid.NewGuid();
        document.SetOrganizationUnit(unitId);
        document.Send(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);
        Assert.Equal(unitId, document.OrganizationUnitId);
    }

    [Fact]
    public void Workflow_submitter_is_stored_without_overwriting_recipient_department()
    {
        var document = Create().DuplicateAsWorkflow(Guid.NewGuid(), "CV-001-WF", null, Now);
        var submitter = Guid.NewGuid();

        document.SetWorkflowSubmitter(submitter);

        Assert.Equal(submitter, document.FromUserId);
        Assert.Null(document.OrganizationUnitId);
    }

    [Fact]
    public void Generated_document_number_is_date_prefixed_and_compact()
    {
        var number = DocumentAppService.GenerateNumber(Now);

        Assert.Equal("20260803-070000", number);
        Assert.Matches(@"^\d{8}-\d{6}$", number);
    }

    [Fact]
    public void Reviewer_can_be_assigned_while_the_document_is_in_review()
    {
        var document = Create();
        document.AddFile(Guid.NewGuid(), "a.pdf", "application/pdf", 10, new string('a', 64), "documents/a", null, Now);
        document.Submit(null, Now);
        document.StartReview(null, Now);
        var signer = Guid.NewGuid();
        document.Assign(Guid.NewGuid(), signer, "sign", null, Now, "sign");
        Assert.Contains(document.Assignments, a => a.AssigneeUserId == signer && a.StepCode == "sign");
    }

    [Fact]
    public void Review_start_keeps_optional_signing_content_in_history()
    {
        var document = Create();
        document.AddFile(Guid.NewGuid(), "a.pdf", "application/pdf", 10, new string('a', 64), "documents/a", null, Now);
        document.Submit(null, Now);

        document.StartReview(null, Now, "Nội dung trình ký");

        Assert.Equal("Nội dung trình ký", document.History.Single(x => x.Action == "ReviewStarted").Detail);
    }

    [Fact]
    public void Word_and_pdf_files_can_be_paired()
    {
        var document = Create();
        var word = document.AddFile(Guid.NewGuid(), "a.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 12, new string('b', 64), "documents/a.docx", null, Now);
        var pdf = document.AddFile(Guid.NewGuid(), "a.pdf", "application/pdf", 20, new string('c', 64), "documents/a.pdf", null, Now);
        word.SetPairedFileId(pdf.Id);
        pdf.SetPairedFileId(word.Id);
        Assert.Equal(pdf.Id, word.PairedFileId);
        Assert.Equal(word.Id, pdf.PairedFileId);
    }
}
