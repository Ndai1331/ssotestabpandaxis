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
}
