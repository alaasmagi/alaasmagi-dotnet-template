namespace External.RabbitMQ;

public class AppMessagingOptions
{
    public string Queue { get; init; } = default!;
    public string Source { get; init; } = default!;
    public string IdentitySource { get; init; } = default!;
    public string[] ConsumerRoutingKeys { get; init; } = [];
}
