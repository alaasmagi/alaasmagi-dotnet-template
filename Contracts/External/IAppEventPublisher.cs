namespace Contracts.External;

public interface IAppEventPublisher
{
    Task PublishAsync<TContent>(
        string type,
        string action,
        TContent content,
        CancellationToken ct = default);
}
