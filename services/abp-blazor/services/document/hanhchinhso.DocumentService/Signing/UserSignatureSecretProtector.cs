using Microsoft.AspNetCore.DataProtection;
using Volo.Abp.DependencyInjection;

namespace hanhchinhso.DocumentService.Signing;

public interface IUserSignatureSecretProtector
{
    string Protect(Guid? tenantId, Guid signatureId, string providerCode, string secret);
    string Unprotect(Guid? tenantId, Guid signatureId, string providerCode, string protectedSecret);
}

public sealed class UserSignatureSecretProtector :
    IUserSignatureSecretProtector,
    ITransientDependency
{
    private readonly IDataProtectionProvider _provider;

    public UserSignatureSecretProtector(IDataProtectionProvider provider)
    {
        _provider = provider;
    }

    public string Protect(
        Guid? tenantId,
        Guid signatureId,
        string providerCode,
        string secret) =>
        CreateProtector(tenantId, signatureId, providerCode).Protect(secret);

    public string Unprotect(
        Guid? tenantId,
        Guid signatureId,
        string providerCode,
        string protectedSecret) =>
        CreateProtector(tenantId, signatureId, providerCode)
            .Unprotect(protectedSecret);

    private IDataProtector CreateProtector(
        Guid? tenantId,
        Guid signatureId,
        string providerCode) =>
        _provider.CreateProtector(
            "hanhchinhso.DocumentService.UserSignatureSecret.v1",
            tenantId?.ToString("N") ?? "host",
            signatureId.ToString("N"),
            SignatureSetting.NormalizeProviderCode(providerCode));
}
