namespace Web.Configuration;

public static class DotEnvConfiguration
{
    public static void ConfigureAspNetCoreUrls()
    {
        var configuredAppPort = Environment.GetEnvironmentVariable("APP_PORT") ??
                                Environment.GetEnvironmentVariable("PORT");
        if (!string.IsNullOrWhiteSpace(configuredAppPort) &&
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", $"http://+:{configuredAppPort}");
        }
    }

    public static void LoadFromRepositoryRoot()
    {
        var envPath = FindDotEnvPath();
        if (envPath == null)
        {
            return;
        }

        foreach (var rawLine in File.ReadLines(envPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = Unquote(line[(separatorIndex + 1)..].Trim());

            if (key.Length == 0 || Environment.GetEnvironmentVariable(key) != null)
            {
                continue;
            }

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string? FindDotEnvPath()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value.StartsWith('"') && value.EndsWith('"')) ||
             (value.StartsWith('\'') && value.EndsWith('\''))))
        {
            return value[1..^1];
        }

        return value;
    }
}
