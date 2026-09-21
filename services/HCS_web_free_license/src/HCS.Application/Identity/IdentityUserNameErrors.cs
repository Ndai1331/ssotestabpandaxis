using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Volo.Abp;
using Volo.Abp.Identity;

namespace HCS.Identity;

internal static class IdentityUserNameErrors
{
    public static Exception MapOrOriginal(AbpIdentityResultException exception)
    {
        if (HasCode(exception, "DuplicateUserName"))
        {
            return new BusinessException(HCSDomainErrorCodes.AccountUserNameTaken);
        }

        if (HasCode(exception, "InvalidUserName"))
        {
            return new BusinessException(HCSDomainErrorCodes.AccountUserNameInvalid);
        }

        return exception;
    }

    private static bool HasCode(AbpIdentityResultException exception, string identityCode)
    {
        if (exception.IdentityResult?.Errors.Any(error =>
                string.Equals(error.Code, identityCode, StringComparison.OrdinalIgnoreCase)) == true)
        {
            return true;
        }

        return ContainsToken(exception.Code, identityCode)
               || ContainsToken(exception.Message, identityCode)
               || ContainsToken(exception.Details, identityCode);
    }

    private static bool ContainsToken(string? value, string token) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains(token, StringComparison.OrdinalIgnoreCase);
}
