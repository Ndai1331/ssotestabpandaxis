using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace HCS.CollaborationService;

/// <summary>
/// Collaboration host uses the shared resource-service JWT contract.
/// </summary>
public static class CollaborationJwtBearer
{
    public const string JwtSubjectClaim = HCS.HcsServiceJwtBearer.JwtSubjectClaim;
    public const string JwtRoleClaim = HCS.HcsServiceJwtBearer.JwtRoleClaim;

    public static IReadOnlyList<string> ResolveIssuers(IConfiguration configuration) =>
        HCS.HcsServiceJwtBearer.ResolveIssuers(configuration);

    public static void AlignAbpClaimTypes() => HCS.HcsServiceJwtBearer.AlignAbpClaimTypes();

    public static void Configure(JwtBearerOptions options, IConfiguration configuration) =>
        HCS.HcsServiceJwtBearer.Configure(options, configuration);
}
