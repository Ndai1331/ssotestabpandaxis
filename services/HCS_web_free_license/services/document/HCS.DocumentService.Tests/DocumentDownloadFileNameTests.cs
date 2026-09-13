using HCS.DocumentService.Documents;

namespace HCS.DocumentService.Tests;

public sealed class DocumentDownloadFileNameTests
{
    [Fact]
    public void Prefers_document_code_over_archive_number_and_title()
    {
        var name = DocumentDownloadFileName.For("123/QĐ-UBND", "HS-009", "Quyết định ban hành", "scan.pdf");
        Assert.Equal("123-QĐ-UBND.pdf", name);
    }

    [Fact]
    public void Uses_archive_number_when_document_code_is_missing()
    {
        var name = DocumentDownloadFileName.For(null, "HS-009", "Quyết định ban hành", "scan.docx");
        Assert.Equal("HS-009.docx", name);
    }

    [Fact]
    public void Uses_title_when_codes_are_missing()
    {
        var name = DocumentDownloadFileName.For("  ", null, "Quyết định ban hành", "scan.pdf");
        Assert.Equal("Quyết định ban hành.pdf", name);
    }

    [Fact]
    public void Falls_back_to_original_stem_when_document_fields_are_empty()
    {
        var name = DocumentDownloadFileName.For(null, "  ", "", "uploaded file.pdf");
        Assert.Equal("uploaded file.pdf", name);
    }

    [Fact]
    public void Sanitizes_illegal_characters_and_keeps_a_safe_default()
    {
        var name = DocumentDownloadFileName.For("A:B*C?", null, null, "memo.docx");
        Assert.Equal("A-B-C.docx", name);
        Assert.Equal("document.pdf", DocumentDownloadFileName.For(null, null, null, ".pdf"));
    }
}
