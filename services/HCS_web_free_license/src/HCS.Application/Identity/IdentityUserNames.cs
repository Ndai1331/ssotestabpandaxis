using System.Linq;

namespace HCS.Identity;

internal static class IdentityUserNames
{
    public static string Normalize(string? value) => value?.Trim() ?? string.Empty;

    public static bool HasInvalidFormat(string userName) =>
        string.IsNullOrWhiteSpace(userName) || userName.Any(char.IsWhiteSpace);
}
