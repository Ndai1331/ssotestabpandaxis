using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Services;
using Microsoft.AspNetCore.Components.Forms;

namespace HCS.Blazor.Client.Documents;

public sealed class DocumentClient(IHttpClientFactory httpClientFactory)
{
    public const int MaxPageSize = 100;

    public Task<PagedDocumentsResponse> GetDocumentsAsync(DocumentListQuery query, CancellationToken cancellationToken = default) =>
        GetAsync<PagedDocumentsResponse>(BuildListUri(query), cancellationToken);

    public Task<DocumentDto> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync<DocumentDto>($"/api/documents/{id:D}", cancellationToken);

    public Task<DocumentDto> CreateDocumentAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<DocumentDto>(HttpMethod.Post, "/api/documents", request, cancellationToken);

    public Task<DocumentDto> UpdateDocumentAsync(Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<DocumentDto>(HttpMethod.Put, $"/api/documents/{id:D}", request, cancellationToken);

    public Task<DocumentDto> AssignAsync(Guid id, AssignDocumentRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<DocumentDto>(HttpMethod.Post, $"/api/documents/{id:D}/assignments", request, cancellationToken);

    public Task<DocumentDto> SubmitAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync<DocumentDto>(HttpMethod.Post, $"/api/documents/{id:D}/submit", new { }, cancellationToken);

    public Task<DocumentDto> SendDocumentAsync(Guid id, SendDocumentRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<DocumentDto>(HttpMethod.Post, $"/api/documents/{id:D}/send", request, cancellationToken);

    public Task<DocumentDto> RevokeDocumentAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync<DocumentDto>(HttpMethod.Post, $"/api/documents/{id:D}/revoke", new { }, cancellationToken);

    public Task DeleteFileAsync(Guid documentId, Guid fileId, CancellationToken cancellationToken = default) =>
        SendNoContentAsync(HttpMethod.Delete, $"/api/documents/{documentId:D}/files/{fileId:D}", null, cancellationToken);

    public async Task<DocumentFileContent> GetFileContentAsync(Guid documentId, Guid fileId, CancellationToken cancellationToken = default)
    {
        using var response = await CreateClient().GetAsync($"/api/documents/{documentId:D}/files/{fileId:D}/content", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? "file";
        return new DocumentFileContent(bytes, contentType, fileName);
    }

    public async Task<DocumentFileContent> GetWatermarkedFileContentAsync(Guid documentId, Guid fileId, CancellationToken cancellationToken = default)
    {
        using var response = await CreateClient().GetAsync(
            $"/api/documents/{documentId:D}/files/{fileId:D}/watermarked-content", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/pdf";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? "document-watermarked.pdf";
        return new DocumentFileContent(bytes, contentType, fileName);
    }

    public async Task<DocumentFileDto> UploadFileAsync(Guid documentId, IBrowserFile file, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream(50 * 1024 * 1024, cancellationToken);
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, "file", file.Name);
        using var response = await CreateClient().PostAsync($"/api/documents/{documentId:D}/files", content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<DocumentFileDto>(cancellationToken: cancellationToken)
            ?? throw new BffApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }

    public Task<List<WorkflowKindDto>> GetKindsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<WorkflowKindDto>>("/api/workflows/kinds", cancellationToken);

    public Task<Guid> CreateKindAsync(CreateWorkflowKindRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<Guid>(HttpMethod.Post, "/api/workflows/kinds", request, cancellationToken);

    public Task UpdateKindAsync(Guid id, UpdateWorkflowKindRequest request, CancellationToken cancellationToken = default) =>
        SendNoContentAsync(HttpMethod.Put, $"/api/workflows/kinds/{id:D}", request, cancellationToken);

    public Task DeleteKindAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendNoContentAsync(HttpMethod.Delete, $"/api/workflows/kinds/{id:D}", null, cancellationToken);

    public Task<List<WorkflowDefinitionDto>> GetDefinitionsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<WorkflowDefinitionDto>>("/api/workflows/definitions", cancellationToken);

    public Task<WorkflowDefinitionDto> GetDefinitionAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync<WorkflowDefinitionDto>($"/api/workflows/definitions/{id:D}", cancellationToken);

    public Task<Guid> CreateDefinitionAsync(CreateWorkflowDefinitionRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<Guid>(HttpMethod.Post, "/api/workflows/definitions", request, cancellationToken);

    public Task UpdateDefinitionAsync(Guid id, UpdateWorkflowDefinitionRequest request, CancellationToken cancellationToken = default) =>
        SendNoContentAsync(HttpMethod.Put, $"/api/workflows/definitions/{id:D}", request, cancellationToken);

    public Task DeleteDefinitionAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendNoContentAsync(HttpMethod.Delete, $"/api/workflows/definitions/{id:D}", null, cancellationToken);

    public Task<List<WorkflowTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<WorkflowTemplateDto>>("/api/workflows/templates", cancellationToken);

    public Task<WorkflowTemplateDto> CreateTemplateAsync(CreateWorkflowTemplateRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<WorkflowTemplateDto>(HttpMethod.Post, "/api/workflows/templates", request, cancellationToken);

    public Task<WorkflowTemplateDto> SetTemplateActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default) =>
        SendAsync<WorkflowTemplateDto>(HttpMethod.Post, $"/api/workflows/templates/{id:D}/active", isActive, cancellationToken);

    public Task<WorkflowTemplateDto> UpdateTemplateAsync(Guid id, UpdateWorkflowTemplateRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<WorkflowTemplateDto>(HttpMethod.Put, $"/api/workflows/templates/{id:D}", request, cancellationToken);

    public async Task<WorkflowTemplateDto> UploadTemplateFileAsync(Guid templateId, string kind, IBrowserFile file, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream(50 * 1024 * 1024, cancellationToken);
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, "file", file.Name);
        using var response = await CreateClient().PostAsync($"/api/workflows/templates/{templateId:D}/files?kind={Uri.EscapeDataString(kind)}", content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<WorkflowTemplateDto>(cancellationToken: cancellationToken)
            ?? throw new BffApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }

    public async Task<byte[]> GetTemplateFileAsync(Guid templateId, string kind, CancellationToken cancellationToken = default)
    {
        using var response = await CreateClient().GetAsync($"/api/workflows/templates/{templateId:D}/files/{Uri.EscapeDataString(kind)}/content", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public Task<List<WorkflowInstanceDto>> GetInstancesAsync(Guid? documentId = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder("/api/workflows/instances?");
        if (documentId.HasValue) builder.Append($"documentId={documentId.Value:D}&");
        if (!string.IsNullOrWhiteSpace(status)) builder.Append($"status={Uri.EscapeDataString(status)}");
        return GetAsync<List<WorkflowInstanceDto>>(builder.ToString().TrimEnd('?', '&'), cancellationToken);
    }

    public Task<WorkflowInstanceDto> GetInstanceAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync<WorkflowInstanceDto>($"/api/workflows/instances/{id:D}", cancellationToken);

    public Task<List<WorkflowStepCandidateGroupDto>> GetAssigneeCandidatesAsync(Guid definitionId, CancellationToken cancellationToken = default) =>
        GetAsync<List<WorkflowStepCandidateGroupDto>>($"/api/workflows/definitions/{definitionId:D}/assignee-candidates", cancellationToken);

    public Task<WorkflowInstanceDto> StartWorkflowAsync(StartWorkflowRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<WorkflowInstanceDto>(HttpMethod.Post, "/api/workflows/instances", request, cancellationToken);

    public Task<WorkflowInstanceDto> DecideAsync(Guid taskId, DecideApprovalTaskRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<WorkflowInstanceDto>(HttpMethod.Post, $"/api/workflows/tasks/{taskId:D}/decision", request, cancellationToken);

    public Task<WorkflowInstanceDto> ResubmitWorkflowAsync(Guid instanceId, string idempotencyKey, CancellationToken cancellationToken = default) =>
        SendAsync<WorkflowInstanceDto>(HttpMethod.Post, $"/api/workflows/instances/{instanceId:D}/resubmit", idempotencyKey, cancellationToken);

    public Task<List<SigningCredentialDto>> GetCredentialsAsync(Guid? userId = null, CancellationToken cancellationToken = default) =>
        GetAsync<List<SigningCredentialDto>>(SigningUserUri("/api/signing/credentials/current", userId), cancellationToken);

    public Task<SigningCredentialDto> ConfigureCredentialAsync(ConfigureSigningCredentialRequest request, Guid? userId = null, CancellationToken cancellationToken = default) =>
        SendAsync<SigningCredentialDto>(HttpMethod.Put, SigningUserUri("/api/signing/credentials/current", userId), request, cancellationToken);

    public Task<SigningAttemptDto> SignAsync(SignDocumentRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<SigningAttemptDto>(HttpMethod.Post, "/api/signing/attempts", request, cancellationToken);

    public Task<SigningReportDto> GetSigningReportAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        GetAsync<SigningReportDto>($"/api/signing/reports/documents/{documentId:D}", cancellationToken);

    public Task<List<UserSignatureDto>> GetSignaturesAsync(Guid? userId = null, CancellationToken cancellationToken = default) =>
        GetAsync<List<UserSignatureDto>>(SigningUserUri("/api/signing/signatures", userId), cancellationToken);

    public async Task<UserSignatureDto> UploadSignatureAsync(IBrowserFile file, Guid? userId = null,
        CancellationToken cancellationToken = default, UserSignatureType type = UserSignatureType.Electronic)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream(2 * 1024 * 1024, cancellationToken);
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "image/png" : file.ContentType);
        content.Add(fileContent, "file", file.Name);
        content.Add(new StringContent(type.ToString()), "signatureType");
        using var response = await CreateClient().PostAsync(SigningUserUri("/api/signing/signatures", userId), content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<UserSignatureDto>(cancellationToken: cancellationToken)
            ?? throw new BffApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }

    public async Task<UserSignatureDto> UpdateSignatureAsync(Guid id, string fileName, IBrowserFile? file = null,
        Guid? userId = null, CancellationToken cancellationToken = default, UserSignatureType? type = null)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(fileName), "fileName");
        if (type is { } selectedType)
        {
            content.Add(new StringContent(selectedType.ToString()), "signatureType");
        }
        if (file is not null)
        {
            await using var stream = file.OpenReadStream(2 * 1024 * 1024, cancellationToken);
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, "file", file.Name);
            using var responseWithFile = await CreateClient().PutAsync(
                SigningUserUri($"/api/signing/signatures/{id:D}", userId), content, cancellationToken);
            await EnsureSuccessAsync(responseWithFile, cancellationToken);
            return await responseWithFile.Content.ReadFromJsonAsync<UserSignatureDto>(cancellationToken: cancellationToken)
                ?? throw new BffApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
        }

        using var response = await CreateClient().PutAsync(
            SigningUserUri($"/api/signing/signatures/{id:D}", userId), content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<UserSignatureDto>(cancellationToken: cancellationToken)
            ?? throw new BffApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }

    public async Task<UserSignatureDto> SetDefaultSignatureAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        using var response = await CreateClient().PutAsync(
            SigningUserUri($"/api/signing/signatures/{id:D}/default", userId), content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<UserSignatureDto>(cancellationToken: cancellationToken)
            ?? throw new BffApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }

    public Task DeleteSignatureAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default) =>
        SendNoContentAsync(HttpMethod.Delete, SigningUserUri($"/api/signing/signatures/{id:D}", userId), null, cancellationToken);

    public async Task<DocumentFileContent> GetSignatureContentAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        using var response = await CreateClient().GetAsync(
            SigningUserUri($"/api/signing/signatures/{id:D}/content", userId), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? "signature.png";
        return new DocumentFileContent(bytes, contentType, fileName);
    }

    private static string SigningUserUri(string path, Guid? userId) =>
        userId is { } id ? $"{path}{(path.Contains('?') ? "&" : "?")}userId={id:D}" : path;

    internal static string BuildListUri(DocumentListQuery query)
    {
        var parameters = new List<string>
        {
            $"skip={Math.Max(0, query.SkipCount)}",
            $"take={Math.Clamp(query.MaxResultCount, 1, MaxPageSize)}",
            $"mine={query.Mine.ToString().ToLowerInvariant()}"
        };
        if (!string.IsNullOrWhiteSpace(query.Filter))
            parameters.Add($"filter={Uri.EscapeDataString(SearchText.Normalize(query.Filter))}");
        if (!string.IsNullOrWhiteSpace(query.Status))
            parameters.Add($"status={Uri.EscapeDataString(query.Status.Trim())}");
        if (query.SourceType is { } sourceType)
            parameters.Add($"sourceType={sourceType}");
        if (query.DocumentTypeId is { } documentTypeId)
            parameters.Add($"documentTypeId={documentTypeId:D}");
        if (query.SectorId is { } sectorId)
            parameters.Add($"sectorId={sectorId:D}");
        if (query.UrgencyId is { } urgencyId)
            parameters.Add($"urgencyId={urgencyId:D}");
        if (query.ConfidentialityId is { } confidentialityId)
            parameters.Add($"confidentialityId={confidentialityId:D}");
        if (query.From is { } from)
            parameters.Add($"from={Uri.EscapeDataString(from.ToString("O"))}");
        if (query.To is { } to)
            parameters.Add($"to={Uri.EscapeDataString(to.ToString("O"))}");
        return $"/api/documents?{string.Join('&', parameters)}";
    }

    private async Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken)
    {
        using var response = await CreateClient().GetAsync(uri, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new BffApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string uri, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = JsonContent.Create(payload) };
        using var response = await CreateClient().SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return default!;
        }
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new BffApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }

    private async Task SendNoContentAsync(HttpMethod method, string uri, object? payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri);
        if (payload is not null) request.Content = JsonContent.Create(payload);
        using var response = await CreateClient().SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new BffApiException(response.StatusCode, body);
    }

    private HttpClient CreateClient() => httpClientFactory.CreateClient("HCS.Bff");
}
