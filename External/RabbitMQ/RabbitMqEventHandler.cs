using System.Text.Json;
using Base.Contracts.Message;
using Base.Keycloak.Events;
using Base.Message;
using Contracts.Application;
using Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace External.RabbitMQ;

public class RabbitMqEventHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqEventHandler> logger) : IBaseEventHandler<JsonElement>
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task HandleAsync(IBaseEventEnvelope<JsonElement> @event, CancellationToken ct)
    {
        if (string.Equals(@event.Source, DefaultSources.IdentityHub, StringComparison.Ordinal))
        {
            await HandleIdentityEventAsync(@event);
            return;
        }

        logger.LogInformation(
            "Received event {EventAction} from {EventSource}. Add a handler branch for this event.",
            @event.Action,
            @event.Source);
    }

    private async Task HandleIdentityEventAsync(IBaseEventEnvelope<JsonElement> @event)
    {
        switch (@event.Action)
        {
            case "user-created":
            case "user-updated":
            case "user-enabled":
            case "user-disabled":
            {
                var content = ReadUserEventContent(@event, out var userId);
                using var scope = scopeFactory.CreateScope();
                var appUserService = scope.ServiceProvider.GetRequiredService<IAppUserService>();
                var result = await appUserService.SyncFromIdentityAsync(
                    AppUser.FromIdentity(userId, content.Email, content.Username, content.FullName, content.Locale));
                if (!result.Successful)
                {
                    throw new InvalidOperationException(
                        $"Could not synchronize Keycloak user {userId}: {result.Error?.Code} {result.Error?.Message}");
                }

                logger.LogInformation("Synchronized Keycloak user {UserId}.", userId);
                break;
            }

            case "user-deleted":
            {
                ReadUserEventContent(@event, out var userId);
                using var scope = scopeFactory.CreateScope();
                var appUserService = scope.ServiceProvider.GetRequiredService<IAppUserService>();
                var result = await appUserService.RemoveFromIdentityAsync(userId);
                if (!result.Successful)
                {
                    throw new InvalidOperationException(
                        $"Could not delete Keycloak user projection {userId}: {result.Error?.Code} {result.Error?.Message}");
                }

                logger.LogInformation("Deleted Keycloak user projection {UserId}.", userId);
                break;
            }

            default:
                logger.LogDebug("Ignoring unsupported identity event action {EventAction}.", @event.Action);
                break;
        }
    }

    private static UserEventContent ReadUserEventContent(IBaseEventEnvelope<JsonElement> @event, out Guid userId)
    {
        var content = @event.Content.Deserialize<UserEventContent>(SerializerOptions);
        if (content == null || !Guid.TryParse(content.UserId, out userId))
        {
            throw new InvalidOperationException($"User event contains invalid userId '{content?.UserId}'.");
        }

        return content;
    }
}
