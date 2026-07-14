using System.Text.Json;
using Base.Contracts.Message;
using Base.Keycloak.Events;
using Base.Message;
using Contracts.DataAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace External.RabbitMQ;

public class RabbitMqEventHandler(
    IServiceScopeFactory scopeFactory,
    AppMessagingOptions options,
    ILogger<RabbitMqEventHandler> logger) : IBaseEventHandler<JsonElement>
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task HandleAsync(IBaseEventEnvelope<JsonElement> @event, CancellationToken ct)
    {
        if (IsIdentityUserEvent(@event))
        {
            await HandleIdentityUserEventAsync(@event, ct);
            return;
        }

        logger.LogInformation(
            "Received event {EventType}/{EventAction} from {EventSource}. Add a handler branch for this event.",
            @event.Type,
            @event.Action,
            @event.Source);
    }

    private bool IsIdentityUserEvent(IBaseEventEnvelope<JsonElement> @event)
    {
        return string.Equals(@event.Type, DefaultMessageTypes.User, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(@event.Source, options.IdentitySource, StringComparison.OrdinalIgnoreCase);
    }

    private async Task HandleIdentityUserEventAsync(IBaseEventEnvelope<JsonElement> @event, CancellationToken ct)
    {
        var content = @event.Content.Deserialize<UserEventContent>(SerializerOptions);
        if (content == null || !Guid.TryParse(content.UserId, out var userId))
        {
            throw new InvalidOperationException($"User event contains invalid userId '{content?.UserId}'.");
        }

        switch (@event.Action)
        {
            case DefaultMessageActions.UserCreated:
            case DefaultMessageActions.UserUpdated:
            case DefaultMessageActions.UserEnabled:
            case DefaultMessageActions.UserDisabled:
                using (var scope = scopeFactory.CreateScope())
                {
                    var appUserRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
                    await appUserRepository.UpsertFromIdentityEventAsync(
                        userId,
                        content.Email,
                        content.Username,
                        content.FullName,
                        content.Locale,
                        ct);
                }
                logger.LogInformation("Synchronized Keycloak user {UserId}.", userId);
                break;

            case DefaultMessageActions.UserDeleted:
                using (var scope = scopeFactory.CreateScope())
                {
                    var appUserRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
                    await appUserRepository.DeleteFromIdentityEventAsync(userId, ct);
                }
                logger.LogInformation("Deleted Keycloak user projection {UserId}.", userId);
                break;

            default:
                logger.LogDebug("Ignoring unsupported user event action {EventAction}.", @event.Action);
                break;
        }
    }
}
