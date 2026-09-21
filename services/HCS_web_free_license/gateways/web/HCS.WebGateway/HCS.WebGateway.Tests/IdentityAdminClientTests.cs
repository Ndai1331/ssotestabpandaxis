using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HCS.Blazor.Client.Pages;
using Microsoft.Extensions.Http;
using Xunit;

namespace HCS.WebGateway.Tests;

public sealed class IdentityAdminClientTests
{
    [Fact]
    public void Builds_identity_query_with_safe_filter_and_page_bounds()
    {
        var actual = IdentityAdminClient.BuildQuery("api/identity/users", "  admin@example.com  ", -20, 500);

        Assert.Equal("api/identity/users?filter=admin%40example.com&skipCount=0&maxResultCount=100", actual);
    }

    [Fact]
    public async Task Sends_create_user_with_the_community_identity_contract()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { id = Guid.NewGuid(), userName = "new-user", email = "new@example.com" })
        });
        var client = CreateClient(handler);
        var form = new IdentityAdminUserForm
        {
            UserName = " new-user ",
            Password = "Password123!",
            Email = "new@example.com",
            IsActive = true,
            LockoutEnabled = true
        };
        form.RoleNames.Add("operator");

        await client.CreateUserAsync(form);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("/api/identity/users", handler.Request.RequestUri!.PathAndQuery);
        using var json = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("new-user", json.RootElement.GetProperty("userName").GetString());
        Assert.Equal("Password123!", json.RootElement.GetProperty("password").GetString());
        Assert.Equal("", json.RootElement.GetProperty("phoneNumber").GetString());
        Assert.Contains("operator", json.RootElement.GetProperty("roleNames").EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public async Task Sends_user_phone_number_on_create_and_update()
    {
        var userId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { id = userId, userName = "existing", email = "existing@example.com", phoneNumber = "0912345678" })
        });
        var client = CreateClient(handler);
        var form = new IdentityAdminUserForm
        {
            UserName = "existing",
            Email = "existing@example.com",
            PhoneNumber = " 0912345678 "
        };

        await client.CreateUserAsync(form);
        using (var created = JsonDocument.Parse(handler.RequestBody!))
        {
            Assert.Equal("0912345678", created.RootElement.GetProperty("phoneNumber").GetString());
        }

        await client.UpdateUserAsync(userId, form, "stamp");
        using var updated = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("0912345678", updated.RootElement.GetProperty("phoneNumber").GetString());
    }

    [Fact]
    public async Task Sends_update_user_without_password_or_role_names()
    {
        var userId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { id = userId, userName = "existing", email = "existing@example.com" })
        });
        var client = CreateClient(handler);
        var form = new IdentityAdminUserForm
        {
            UserName = "existing",
            Email = "existing@example.com"
        };
        form.RoleNames.Add("admin");
        form.RoleNames.Add("bacsi");

        await client.UpdateUserAsync(userId, form, "stamp");

        using var json = JsonDocument.Parse(handler.RequestBody!);
        Assert.False(json.RootElement.TryGetProperty("password", out _));
        Assert.False(json.RootElement.TryGetProperty("roleNames", out _));
        Assert.False(json.RootElement.TryGetProperty("extraProperties", out _));
        Assert.Equal("stamp", json.RootElement.GetProperty("concurrencyStamp").GetString());
        Assert.Equal($"/api/identity/users/{userId:D}", handler.Request!.RequestUri!.PathAndQuery);
        Assert.Equal(HttpMethod.Put, handler.Request.Method);
    }

    [Fact]
    public async Task Sends_updated_user_name_on_update()
    {
        var userId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { id = userId, userName = "renamed-user", email = "existing@example.com" })
        });
        var client = CreateClient(handler);
        var form = new IdentityAdminUserForm
        {
            UserName = " renamed-user ",
            Email = "existing@example.com"
        };

        var updated = await client.UpdateUserAsync(userId, form, "stamp");

        using var json = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("renamed-user", json.RootElement.GetProperty("userName").GetString());
        Assert.Equal("renamed-user", updated.UserName);
    }

    [Fact]
    public void Maps_duplicate_and_invalid_user_name_identity_errors()
    {
        var duplicate = new IdentityAdminApiException(
            HttpStatusCode.BadRequest,
            """{"error":{"code":"Volo.Abp.Identity:DuplicateUserName","message":"Username 'x' is already taken."}}""");
        var invalid = new IdentityAdminApiException(
            HttpStatusCode.BadRequest,
            """{"error":{"code":"HCS:AccountUserNameInvalid","message":"The username is invalid."}}""");
        var other = new IdentityAdminApiException(
            HttpStatusCode.BadRequest,
            """{"error":{"code":"Users:BadRequest","message":"Invalid data."}}""");

        Assert.True(duplicate.IsDuplicateUserName);
        Assert.False(duplicate.IsInvalidUserName);
        Assert.True(invalid.IsInvalidUserName);
        Assert.False(other.IsDuplicateUserName);
        Assert.False(other.IsInvalidUserName);
    }

    [Fact]
    public async Task Reloads_user_when_update_response_body_is_empty()
    {
        var userId = Guid.NewGuid();
        var handler = new RecordingHandler(request =>
        {
            if (request.Method == HttpMethod.Put)
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { id = userId, userName = "existing", email = "existing@example.com" })
            };
        });
        var client = CreateClient(handler);
        var form = new IdentityAdminUserForm { UserName = "existing", Email = "existing@example.com" };

        var updated = await client.UpdateUserAsync(userId, form, "stamp");

        Assert.Equal(userId, updated.Id);
        Assert.Equal("existing", updated.UserName);
    }

    [Fact]
    public async Task Sends_update_user_roles_payload()
    {
        var userId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(handler);

        await client.UpdateUserRolesAsync(userId, ["admin", "lanhdao"]);

        Assert.Equal(HttpMethod.Put, handler.Request!.Method);
        Assert.Equal($"/api/identity/users/{userId:D}/roles", handler.Request.RequestUri!.PathAndQuery);
        using var json = JsonDocument.Parse(handler.RequestBody!);
        var roleNames = json.RootElement.GetProperty("roleNames").EnumerateArray()
            .Select(item => item.GetString())
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray();
        Assert.Equal(new[] { "admin", "lanhdao" }, roleNames);
    }

    [Fact]
    public async Task Deletes_user_organization_mapping()
    {
        var mappingId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(handler);

        await client.DeleteUserMappingAsync(mappingId);

        Assert.Equal(HttpMethod.Delete, handler.Request!.Method);
        Assert.Equal($"/api/organization/user-mappings/{mappingId:D}", handler.Request.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task Sends_create_role_with_the_community_identity_contract()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { id = Guid.NewGuid(), name = "operator", isDefault = true, isPublic = false })
        });
        var client = CreateClient(handler);

        await client.CreateRoleAsync(" operator ", true, false);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("/api/identity/roles", handler.Request.RequestUri!.PathAndQuery);
        using var json = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("operator", json.RootElement.GetProperty("name").GetString());
        Assert.True(json.RootElement.GetProperty("isDefault").GetBoolean());
        Assert.False(json.RootElement.GetProperty("isPublic").GetBoolean());
    }

    [Fact]
    public async Task Sends_update_role_with_name_flags_and_concurrency_stamp()
    {
        var roleId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { id = roleId, name = "operator", isDefault = false, isPublic = true, concurrencyStamp = "next" })
        });
        var client = CreateClient(handler);

        await client.UpdateRoleAsync(roleId, " operator ", false, true, "stamp");

        Assert.Equal(HttpMethod.Put, handler.Request!.Method);
        Assert.Equal($"/api/identity/roles/{roleId:D}", handler.Request.RequestUri!.PathAndQuery);
        using var json = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("operator", json.RootElement.GetProperty("name").GetString());
        Assert.False(json.RootElement.GetProperty("isDefault").GetBoolean());
        Assert.True(json.RootElement.GetProperty("isPublic").GetBoolean());
        Assert.Equal("stamp", json.RootElement.GetProperty("concurrencyStamp").GetString());
    }

    [Fact]
    public async Task Reloads_role_stamp_when_update_is_called_without_one()
    {
        var roleId = Guid.NewGuid();
        var handler = new RecordingHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { id = roleId, name = "operator", concurrencyStamp = "loaded" })
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { id = roleId, name = "lead", concurrencyStamp = "next" })
            };
        });
        var client = CreateClient(handler);

        var updated = await client.UpdateRoleAsync(roleId, "lead", false, false, null);

        Assert.Equal("lead", updated.Name);
        Assert.Equal($"/api/identity/roles/{roleId:D}", handler.Request!.RequestUri!.PathAndQuery);
        using var json = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("loaded", json.RootElement.GetProperty("concurrencyStamp").GetString());
    }

    [Fact]
    public async Task Uses_role_provider_and_escapes_role_key_for_permissions()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { entityDisplayName = "Role", groups = Array.Empty<object>() })
        });
        var client = CreateClient(handler);

        await client.GetRolePermissionsAsync("role admin");

        Assert.Equal("/api/permission-management/permissions?providerName=R&providerKey=role%20admin", handler.Request!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task Updates_role_permissions_through_the_admin_endpoint()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(handler);

        await client.UpdateRolePermissionsAsync("role admin",
        [
            new IdentityAdminPermission { Name = "Documents.Workflow.View", IsGranted = true }
        ]);

        Assert.Equal(HttpMethod.Put, handler.Request!.Method);
        Assert.Equal("/api/admin/roles/role%20admin/permissions", handler.Request.RequestUri!.PathAndQuery);
        using var json = JsonDocument.Parse(handler.RequestBody!);
        var permission = json.RootElement.GetProperty("permissions")[0];
        Assert.Equal("Documents.Workflow.View", permission.GetProperty("name").GetString());
        Assert.True(permission.GetProperty("isGranted").GetBoolean());
    }

    [Fact]
    public async Task Deletes_role_through_the_identity_endpoint()
    {
        var roleId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(handler);

        await client.DeleteRoleAsync(roleId);

        Assert.Equal(HttpMethod.Delete, handler.Request!.Method);
        Assert.Equal($"/api/identity/roles/{roleId:D}", handler.Request.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task Reads_user_roles_from_the_identity_list_result()
    {
        var userId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                items = new[]
                {
                    new { id = Guid.NewGuid(), name = "bacsi", isDefault = false, isStatic = false, isPublic = true }
                }
            })
        });
        var client = CreateClient(handler);

        var roles = await client.GetUserRolesAsync(userId);

        Assert.Equal($"/api/identity/users/{userId:D}/roles", handler.Request!.RequestUri!.PathAndQuery);
        Assert.Equal(["bacsi"], roles.Select(role => role.Name));
    }

    [Fact]
    public async Task Maps_identity_conflict_to_typed_exception()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("duplicate user")
        });
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<IdentityAdminApiException>(() =>
            client.DeleteUserAsync(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal("duplicate user", exception.ResponseBody);
    }

    [Fact]
    public async Task Maps_role_delete_conflict_to_typed_exception()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("role in use")
        });
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<IdentityAdminApiException>(() =>
            client.DeleteRoleAsync(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal("role in use", exception.ResponseBody);
    }

    [Fact]
    public void Reads_abp_error_code_from_conflict_body()
    {
        var exception = new IdentityAdminApiException(
            HttpStatusCode.Forbidden,
            """{"error":{"code":"HCS:RoleAssignedToUsers","message":"assigned"}}""");

        Assert.Equal("HCS:RoleAssignedToUsers", exception.ErrorCode);
        Assert.Equal("assigned", exception.UserMessage);
    }

    private static IdentityAdminClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://bff.test") };
        return new IdentityAdminClient(new StubHttpClientFactory(httpClient));
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return responder(request);
        }
    }
}
