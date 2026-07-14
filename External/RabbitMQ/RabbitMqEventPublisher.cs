using Base.Contracts.Message;
using Base.Message;
using Contracts.External;

namespace External.RabbitMQ;

public class RabbitMqEventPublisher(
    IBaseEventPublisher publisher,
    AppMessagingOptions options) : IAppEventPublisher
{
    public Task PublishAsync<TContent>(
        string type,
        string action,
        TContent content,
        CancellationToken ct = default)
    {
        var envelope = new BaseEventEnvelope<TContent>
        {
            Type = type,
            Source = options.Source,
            Action = action,
            Timestamp = DateTime.UtcNow,
            Content = content
        };

        return publisher.PublishAsync(action, envelope, ct);
    }
}
