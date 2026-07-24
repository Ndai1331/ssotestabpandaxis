namespace hanhchinhso.AppHost.Hosting;

/// <summary>
/// Resolves light|full run profile from CLI args or HCS_RUN_PROFILE env.
/// </summary>
internal static class RunProfile
{
    public const string Light = "light";
    public const string Full = "full";

    public static string Resolve(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg is "--profile" or "-p" && i + 1 < args.Length)
            {
                return Normalize(args[++i]);
            }

            if (arg.StartsWith("--profile=", StringComparison.OrdinalIgnoreCase))
            {
                return Normalize(arg["--profile=".Length..]);
            }
        }

        var fromEnv = Environment.GetEnvironmentVariable("HCS_RUN_PROFILE");
        return string.IsNullOrWhiteSpace(fromEnv) ? Light : Normalize(fromEnv);
    }

    private static string Normalize(string value)
    {
        var profile = value.Trim().ToLowerInvariant();
        if (profile is not (Light or Full))
        {
            throw new InvalidOperationException(
                $"Unknown run profile '{value}'. Use '{Light}' (default) or '{Full}'.");
        }

        return profile;
    }
}
