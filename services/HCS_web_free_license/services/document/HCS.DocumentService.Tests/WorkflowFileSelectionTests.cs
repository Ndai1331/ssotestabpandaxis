using HCS.DocumentService.Documents;

namespace HCS.DocumentService.Tests;

public sealed class WorkflowFileSelectionTests
{
    private static readonly DateTime Now = DateTime.UtcNow;

    [Fact]
    public void Attachments_do_not_replace_the_selected_submission_or_its_signed_version()
    {
        var doc = Create();
        var original = Add(doc, "request.pdf", 0);
        doc.SetWorkflowFile(original.Id);
        Add(doc, "attachment.pdf", 1);
        Add(doc, "attachment.docx", 2);
        Add(doc, "attachment-converted.pdf", 3);
        Assert.Equal(original.Id, WorkflowFileSelection.Resolve(doc));
        var signed = Add(doc, "request-Sign01.pdf", 4);
        doc.SetWorkflowFile(signed.Id);
        Add(doc, "later.pdf", 5);
        Assert.Equal(signed.Id, WorkflowFileSelection.Resolve(doc));
        Assert.True(DocumentAppService.Map(doc).Files.Single(x => x.Id == signed.Id).IsWorkflowFile);
        Assert.Single(DocumentAppService.Map(doc).Files, x => x.IsWorkflowFile);
    }

    [Theory]
    [InlineData("request-prepared.pdf")]
    [InlineData("request-prepared-Sign01.pdf")]
    public void Legacy_submissions_follow_generated_versions_instead_of_later_attachments(string mainName)
    {
        var doc = Create();
        Add(doc, "request.pdf", 0);
        var main = Add(doc, mainName, 1);
        Add(doc, "unrelated.pdf", 2);
        Add(doc, "unrelated.docx", 3);
        Add(doc, "unrelated-converted.pdf", 4);
        Assert.Equal(main.Id, WorkflowFileSelection.Resolve(doc));
    }

    [Fact]
    public void Cannot_select_a_file_from_another_document()
    {
        var doc = Create();
        Assert.Throws<InvalidOperationException>(() => doc.SetWorkflowFile(Guid.NewGuid()));
    }

    [Fact]
    public void Legacy_submission_with_multiple_original_pdfs_uses_the_word_pdf_pair()
    {
        var doc = Create();
        Add(doc, "property-report.pdf", 0);
        var word = Add(doc, "request.docx", 0);
        var pdf = Add(doc, "request.pdf", 0);
        word.SetPairedFileId(pdf.Id);
        pdf.SetPairedFileId(word.Id);
        var prepared = Add(doc, "request-prepared.pdf", 1);
        Add(doc, "extra.pdf", 2);
        Assert.Equal(prepared.Id, WorkflowFileSelection.Resolve(doc));
    }

    private static DocumentAggregate Create() => new(Guid.NewGuid(), "WF-1", "Request", null,
        Guid.NewGuid(), Now, DocumentSourceType.Workflow);

    private static DocumentFile Add(DocumentAggregate doc, string name, int seconds) =>
        doc.AddFile(Guid.NewGuid(), name, name.EndsWith(".pdf") ? "application/pdf" : "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            10, Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString(), Guid.NewGuid(), Now.AddSeconds(seconds));
}
