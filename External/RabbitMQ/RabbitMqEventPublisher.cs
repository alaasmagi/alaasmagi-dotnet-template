using Base.Contracts.DTO;
using Base.Contracts.Message;
using Base.DTO;
using Base.Message;
using Contracts.External;

namespace External.RabbitMQ;

public class RabbitMqEventPublisher(
    IBaseEventPublisher publisher,
    AppMessagingOptions options) : IAppEventPublisher
{
    public const string PublishFailedErrorCode = "PUBLISH_FAILED";

    public async Task<IMethodResponse<bool>> PublishAsync<TContent>(
        string action,
        string contentVersion,
        TContent content,
        CancellationToken ct = default)
    {
        // The app slug is both source and tenant, giving routing key <slug>.<slug>.<action>.
        var envelope = BaseEventEnvelope<TContent>.Create(
            source: options.Source,
            tenant: options.Source,
            action: action,
            contentVersion: contentVersion,
            content: content);

        var result = await publisher.PublishAsync(envelope, cancellationToken: ct);

        return result.Success
            ? MethodResponse<bool>.Success(true)
            : MethodResponse<bool>.Failure(new Error(PublishFailedErrorCode, result.FailureReason ?? "The event could not be published."));
    }
}
