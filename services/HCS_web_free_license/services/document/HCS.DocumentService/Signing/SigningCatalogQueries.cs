using Microsoft.EntityFrameworkCore;

namespace HCS.DocumentService.Signing;

internal static class SigningCatalogQueries
{
    internal static IQueryable<SigningCredential> WhereVisibleSigningCredentials(this IQueryable<SigningCredential> query) =>
        query.Where(x => x.IsActive && !x.IsDeleted);

    internal static IQueryable<UserSignature> WhereActiveUserSignatures(this IQueryable<UserSignature> query) =>
        query.Where(x => x.IsActive);
}
