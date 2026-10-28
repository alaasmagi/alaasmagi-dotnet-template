using Base.Cache;
using Base.Keycloak.Authentication;
using Base.Message.RabbitMQ;
using External.RabbitMQ;

namespace Web.Configuration;

public static class RequiredConfiguration
{
    public static string AppConnectionString()
    {
        return Required(
            Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING"),
            "Application database connection string",
            "DATABASE_CONNECTION_STRING");
    }

    public static KeycloakOptions KeycloakOptions()
    {
        var authority = Required(
            Environment.GetEnvironmentVariable("KEYCLOAK_AUTHORITY"),
            "Keycloak authority",
            "KEYCLOAK_AUTHORITY");

        var clientId = Required(
            Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_ID"),
            "Keycloak client id",
            "KEYCLOAK_CLIENT_ID");

        return new KeycloakOptions
        {
            Authority = authority,
            ClientId = clientId,
            Audience = clientId,
            ClientSecret = Required(
                Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_SECRET"),
                "Keycloak client secret",
                "KEYCLOAK_CLIENT_SECRET"),
            RequireHttpsMetadata = !authority.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase) &&
                                   !authority.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase),
            IncludeClientRoles = true
        };
    }

    public static RabbitMqOptions RabbitMqOptions()
    {
        var uri = new Uri(Required(
            Environment.GetEnvironmentVariable("RABBITMQ_URI"),
            "RabbitMQ URI",
            "RABBITMQ_URI"));

        if (!uri.Scheme.Equals("amqp", StringComparison.OrdinalIgnoreCase) &&
            !uri.Scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("RABBITMQ_URI must use amqp:// or amqps://.");
        }

        var credentials = uri.UserInfo.Split(':', 2);
        if (credentials.Length != 2 ||
            string.IsNullOrWhiteSpace(credentials[0]) ||
            string.IsNullOrWhiteSpace(credentials[1]))
        {
            throw new InvalidOperationException("RABBITMQ_URI must include username and password.");
        }

        return new RabbitMqOptions
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? DefaultRabbitMqPort(uri.Scheme) : uri.Port,
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1]),
            VirtualHost = RabbitMqVirtualHost(uri),
            Exchange = Required(
                Environment.GetEnvironmentVariable("RABBITMQ_EXCHANGE"),
                "RabbitMQ exchange",
                "RABBITMQ_EXCHANGE"),
            UseTls = uri.Scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase)
        };
    }

    public static AppMessagingOptions AppMessagingOptions()
    {
        return new AppMessagingOptions
        {
            Queue = Required(
                Environment.GetEnvironmentVariable("RABBITMQ_QUEUE"),
                "RabbitMQ consumer queue",
                "RABBITMQ_QUEUE"),
            Source = Required(
                Environment.GetEnvironmentVariable("APP_EVENT_SOURCE"),
                "Event source and tenant of published events",
                "APP_EVENT_SOURCE")
        };
    }

    public static string? GlitchtipDsn() => Optional("GLITCHTIP_DSN");

    public static string GlitchtipRelease()
    {
        return Required(
            Environment.GetEnvironmentVariable("GLITCHTIP_RELEASE"),
            "Glitchtip release",
            "GLITCHTIP_RELEASE");
    }

    public static ApiRateLimitOptions ApiRateLimitOptions()
    {
        return new ApiRateLimitOptions(
            PermitLimit: RequiredPositiveInt("RATE_LIMIT_PERMIT_LIMIT"),
            WindowSeconds: RequiredPositiveInt("RATE_LIMIT_WINDOW_SECONDS"),
            QueueLimit: RequiredNonNegativeInt("RATE_LIMIT_QUEUE_LIMIT"));
    }

    public static string RedisConnectionString()
    {
        return Required(
            Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING"),
            "Redis connection string",
            "REDIS_CONNECTION_STRING");
    }

    public static BaseCacheOptions BaseCacheOptions()
    {
        return new BaseCacheOptions
        {
            KeyPrefix = Required(
                Environment.GetEnvironmentVariable("CACHE_KEY_PREFIX"),
                "Cache key prefix",
                "CACHE_KEY_PREFIX"),
            DefaultEntryOptions = new BaseCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(
                    RequiredPositiveInt("CACHE_DEFAULT_ABSOLUTE_EXPIRATION_SECONDS"))
            }
        };
    }

    private static string? Optional(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int RequiredPositiveInt(string key)
    {
        if (int.TryParse(Environment.GetEnvironmentVariable(key), out var parsed) && parsed > 0)
        {
            return parsed;
        }

        throw new InvalidOperationException($"{key} must be a positive integer.");
    }

    private static int RequiredNonNegativeInt(string key)
    {
        if (int.TryParse(Environment.GetEnvironmentVariable(key), out var parsed) && parsed >= 0)
        {
            return parsed;
        }

        throw new InvalidOperationException($"{key} must be a non-negative integer.");
    }

    private static int DefaultRabbitMqPort(string scheme)
    {
        return scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase) ? 5671 : 5672;
    }

    private static string RabbitMqVirtualHost(Uri uri)
    {
        var path = uri.AbsolutePath.Trim('/');
        return string.IsNullOrWhiteSpace(path)
            ? "/"
            : Uri.UnescapeDataString(path);
    }

    private static string Required(string? value, string description, string key)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"Missing required configuration: {description}. Set {key}.");
    }
}
