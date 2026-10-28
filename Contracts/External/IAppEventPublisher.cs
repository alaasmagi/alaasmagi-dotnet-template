using Base.Contracts.DTO;

namespace Contracts.External;

public interface IAppEventPublisher
{
    Task<IMethodResponse<bool>> PublishAsync<TContent>(
        string action,
        string contentVersion,
        TContent content,
        CancellationToken ct = default);
}
