namespace Web.Configuration;

public record ApiRateLimitOptions(
    int PermitLimit,
    int WindowSeconds,
    int QueueLimit);
