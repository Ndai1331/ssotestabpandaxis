using HCS.DocumentService.Signing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCS.DocumentService.Controllers;

[ApiController, Authorize, Route("api/signing")]
public sealed class SigningController(ISigningAppService signing) : ControllerBase
{
    [HttpGet("credentials/current"), Authorize(Policy = Documents.DocumentPermissions.SigningConfigure)]
    public Task<IReadOnlyList<SigningCredentialDto>> GetCurrent([FromQuery] Guid? userId, CancellationToken cancellationToken) =>
        signing.GetCredentialsAsync(userId, cancellationToken);
    [HttpPut("credentials/current"), Authorize(Policy = Documents.DocumentPermissions.SigningConfigure)]
    public Task<SigningCredentialDto> Configure(ConfigureSigningCredentialRequest input, [FromQuery] Guid? userId, CancellationToken cancellationToken) =>
        signing.ConfigureCredentialAsync(input, userId, cancellationToken);
    [HttpPost("attempts"), Authorize(Policy = Documents.DocumentPermissions.SigningExecute)]
    public Task<SigningAttemptDto> Sign(SignDocumentRequest input, CancellationToken cancellationToken) => signing.SignAsync(input, cancellationToken);
    [HttpGet("reports/documents/{documentId:guid}"), Authorize(Policy = Documents.DocumentPermissions.SigningReport)]
    public Task<SigningReportDto> Report(Guid documentId, CancellationToken cancellationToken) => signing.GetReportAsync(documentId, cancellationToken);
    [HttpGet("signatures"), Authorize(Policy = Documents.DocumentPermissions.SigningExecute)]
    public Task<IReadOnlyList<UserSignatureDto>> GetSignatures([FromQuery] Guid? userId, CancellationToken cancellationToken) =>
        signing.GetSignaturesAsync(userId, cancellationToken);
    [HttpPost("signatures"), Authorize(Policy = Documents.DocumentPermissions.SigningExecute)]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<UserSignatureDto> UploadSignature(IFormFile file, [FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return await signing.UploadSignatureAsync(file.FileName, file.ContentType, stream, file.Length, userId, cancellationToken);
    }
    [HttpDelete("signatures/{id:guid}"), Authorize(Policy = Documents.DocumentPermissions.SigningExecute)]
    public async Task<IActionResult> DeleteSignature(Guid id, [FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        await signing.DeleteSignatureAsync(id, userId, cancellationToken);
        return NoContent();
    }
}
