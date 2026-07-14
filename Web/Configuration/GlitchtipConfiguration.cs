namespace Web.Configuration;

public static class GlitchtipConfiguration
{
    public static void ConfigureGlitchtip(this WebApplicationBuilder builder)
    {
        var dsn = RequiredConfiguration.GlitchtipDsn();
        if (string.IsNullOrWhiteSpace(dsn))
        {
            return;
        }

        builder.WebHost.UseSentry(options =>
        {
            options.Dsn = dsn;
            options.Release = RequiredConfiguration.GlitchtipRelease();
            options.Environment = builder.Environment.EnvironmentName;
            options.MinimumBreadcrumbLevel = LogLevel.Information;
            options.MinimumEventLevel = LogLevel.Error;
            options.TracesSampleRate = 0;
        });
    }
}
