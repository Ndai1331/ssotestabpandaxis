using System.Text;
using System.IO.Compression;
using hanhchinhso.DocumentService.Controllers;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace hanhchinhso.DocumentService.Tests.Document;

public class DocumentFileValidationTests
{
    [Fact]
    public void Should_Strip_Path_From_Uploaded_File_Name()
    {
        DocumentFilesController.SanitizeDisplayName("../../safe.pdf").ShouldBe("safe.pdf");
    }

    [Fact]
    public async Task Should_Accept_Pdf_Magic_Bytes()
    {
        await using var stream = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.7"));
        await DocumentFilesController.EnsureMagicBytesAsync(stream, ".pdf", default);
        stream.Position.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Reject_Content_That_Does_Not_Match_Extension()
    {
        await using var stream = new MemoryStream(Encoding.ASCII.GetBytes("not-a-pdf"));
        await Should.ThrowAsync<UserFriendlyException>(
            () => DocumentFilesController.EnsureMagicBytesAsync(stream, ".pdf", default));
    }

    [Fact]
    public async Task Should_Reject_Generic_Zip_Disguised_As_Docx()
    {
        await using var stream = CreateZip(("payload.txt", "not a word document"));
        await Should.ThrowAsync<UserFriendlyException>(
            () => DocumentFilesController.EnsureMagicBytesAsync(stream, ".docx", default));
    }

    [Fact]
    public async Task Should_Accept_Minimal_Valid_Docx_Package()
    {
        await using var stream = CreateZip(
            ("[Content_Types].xml", "<Types/>"),
            ("word/document.xml", "<w:document/>"));
        await DocumentFilesController.EnsureMagicBytesAsync(stream, ".docx", default);
        stream.Position.ShouldBe(0);
    }

    private static MemoryStream CreateZip(params (string Name, string Content)[] entries)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = archive.CreateEntry(name);
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                writer.Write(content);
            }
        }
        stream.Position = 0;
        return stream;
    }
}
