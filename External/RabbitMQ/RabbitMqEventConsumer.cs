using System.Text.Json;
using Base.Contracts.Message;
using Base.Message.RabbitMQ;
using Microsoft.Extensions.Logging;

namespace External.RabbitMQ;

public class RabbitMqEventConsumer : RabbitMqConsumerBase<JsonElement>
{
    public RabbitMqEventConsumer(
        RabbitMqConnectionManager connections,
        RabbitMqOptions options,
        AppMessagingOptions appOptions,
        IBaseEventHandler<JsonElement> handler,
        ILogger<RabbitMqEventConsumer> logger)
        : base(
            connections,
            options,
            handler,
            logger,
            queueName: appOptions.Queue,
            routingKeyPatterns: appOptions.ConsumerRoutingKeys)
    {
    }
}
