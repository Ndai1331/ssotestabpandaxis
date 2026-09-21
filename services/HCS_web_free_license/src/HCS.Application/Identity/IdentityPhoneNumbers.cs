namespace HCS.Identity;

internal static class IdentityPhoneNumbers
{
    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
