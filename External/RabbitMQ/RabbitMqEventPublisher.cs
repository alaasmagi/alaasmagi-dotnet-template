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
        var envelope = BaseEventEnvelope<TContent>.Create(
            source: options.Source,
            tenant: string.Empty,
            action: action,
            contentVersion: string.Empty,
            content: content);

        return publisher.PublishAsync(envelope, cancellationToken: ct);
    }
}
