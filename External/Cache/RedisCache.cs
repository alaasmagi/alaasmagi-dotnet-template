using Base.Cache;
using Base.Contracts.Cache;
using Contracts.External;
using StackExchange.Redis;

namespace External.Cache;

public class RedisCache : BaseCache, IAppCache
{
    private readonly IConnectionMultiplexer _connection;

    public RedisCache(
        IConnectionMultiplexer connection,
        IBaseCacheSerializer cacheSerializer,
        IBaseCacheKeyBuilder cacheKeyBuilder,
        BaseCacheOptions cacheOptions)
        : base(cacheSerializer, cacheKeyBuilder, cacheOptions)
    {
        _connection = connection;
    }

    protected override async Task<byte[]?> GetBytesAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var value = await Database
            .StringGetAsync(key)
            .WaitAsync(cancellationToken);

        return value.IsNull ? null : (byte[])value!;
    }

    protected override async Task SetBytesAsync(
        string key,
        byte[] value,
        IBaseCacheEntryOptions options,
        CancellationToken cancellationToken = default)
    {
        await Database
            .StringSetAsync(key, value, ResolveExpiration(options))
            .WaitAsync(cancellationToken);
    }

    protected override Task<bool> ExistsByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return Database
            .KeyExistsAsync(key)
            .WaitAsync(cancellationToken);
    }

    protected override Task<bool> RemoveByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return Database
            .KeyDeleteAsync(key)
            .WaitAsync(cancellationToken);
    }

    private IDatabase Database => _connection.GetDatabase();

    private static TimeSpan? ResolveExpiration(IBaseCacheEntryOptions options)
    {
        if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            return options.AbsoluteExpirationRelativeToNow;
        }

        if (options.AbsoluteExpiration.HasValue)
        {
            var expiration = options.AbsoluteExpiration.Value - DateTimeOffset.UtcNow;
            return expiration > TimeSpan.Zero ? expiration : TimeSpan.Zero;
        }

        return options.SlidingExpiration;
    }
}
