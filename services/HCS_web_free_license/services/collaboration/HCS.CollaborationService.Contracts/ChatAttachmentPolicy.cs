namespace HCS.CollaborationService.Contracts;

public static class ChatAttachmentPolicy
{
    public const int DefaultMegabytes = 512;
    public const int MinMegabytes = 1;
    public const int MaxMegabytes = 2048;
    public const long RequestCeilingBytes = 3L * 1024 * 1024 * 1024;

    public static int ClampMegabytes(int megabytes) =>
        Math.Clamp(megabytes, MinMegabytes, MaxMegabytes);

    public static long ToBytes(int megabytes) =>
        (long)ClampMegabytes(megabytes) * 1024 * 1024;
}

public sealed record ChatAttachmentPolicyDto(int MaxMegabytes);
