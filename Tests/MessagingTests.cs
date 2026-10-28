using System.Text.Json;
using Base.Contracts.DTO;
using Base.Contracts.Message;
using Base.DTO;
using Base.Message;
using Contracts.Application;
using Domain;
using External.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests;

public class RabbitMqEventPublisherTests
{
    private static readonly AppMessagingOptions Options = new() { Queue = "app-service", Source = "app-service" };

    [Test]
    public async Task PublishAsync_UsesSlugAsSourceAndTenant()
    {
        var basePublisher = new FakeBasePublisher(PublishResult.Ok());
        var publisher = new RabbitMqEventPublisher(basePublisher, Options);

        var result = await publisher.PublishAsync("thing-created", "1.0", new { Name = "thing" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Successful, Is.True);
            Assert.That(basePublisher.RoutingKey, Is.EqualTo("app-service.app-service.thing-created"));
            Assert.That(basePublisher.ContentVersion, Is.EqualTo("1.0"));
        });
    }

    [Test]
    public async Task PublishAsync_BrokerFailure_ReturnsPublishFailed()
    {
        var publisher = new RabbitMqEventPublisher(new FakeBasePublisher(PublishResult.Failed("nack")), Options);

        var result = await publisher.PublishAsync("thing-created", "1.0", new { Name = "thing" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Successful, Is.False);
            Assert.That(result.Error?.Code, Is.EqualTo(RabbitMqEventPublisher.PublishFailedErrorCode));
            Assert.That(result.Error?.Message, Is.EqualTo("nack"));
        });
    }

    private class FakeBasePublisher(PublishResult result) : IBaseEventPublisher
    {
        public string? RoutingKey { get; private set; }
        public string? ContentVersion { get; private set; }

        public Task<PublishResult> PublishAsync<TContent>(
            IBaseEventEnvelope<TContent> message,
            string? expiration = null,
            CancellationToken cancellationToken = default)
        {
            RoutingKey = BaseRoutingKey.For(message);
            ContentVersion = message.ContentVersion;
            return Task.FromResult(result);
        }
    }
}

public class RabbitMqEventHandlerTests
{
    private FakeAppUserService _appUserService = default!;
    private RabbitMqEventHandler _handler = default!;

    [SetUp]
    public void SetUp()
    {
        _appUserService = new FakeAppUserService();
        var provider = new ServiceCollection()
            .AddScoped<IAppUserService>(_ => _appUserService)
            .BuildServiceProvider();
        _handler = new RabbitMqEventHandler(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<RabbitMqEventHandler>.Instance);
    }

    private static IBaseEventEnvelope<JsonElement> Envelope(string source, string action, object content)
    {
        return BaseEventEnvelope<JsonElement>.Create(source, "my-realm", action, "1.0", JsonSerializer.SerializeToElement(content));
    }

    [TestCase("user-created")]
    [TestCase("user-updated")]
    [TestCase("user-enabled")]
    [TestCase("user-disabled")]
    public async Task HandleAsync_IdentityUserEvent_SyncsUser(string action)
    {
        var userId = Guid.NewGuid();

        await _handler.HandleAsync(
            Envelope(DefaultSources.IdentityHub, action, new { userId, email = " user@example.com ", username = "user", fullName = "Full Name" }),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(_appUserService.Synced?.Id, Is.EqualTo(userId));
            Assert.That(_appUserService.Synced?.Email, Is.EqualTo("user@example.com"));
            Assert.That(_appUserService.Synced?.Locale, Is.EqualTo(AppUser.DefaultLocale));
        });
    }

    [Test]
    public async Task HandleAsync_IdentityUserDeleted_RemovesUser()
    {
        var userId = Guid.NewGuid();

        await _handler.HandleAsync(Envelope(DefaultSources.IdentityHub, "user-deleted", new { userId }), CancellationToken.None);

        Assert.That(_appUserService.RemovedId, Is.EqualTo(userId));
    }

    [Test]
    public async Task HandleAsync_OtherSource_IsIgnored()
    {
        await _handler.HandleAsync(Envelope("other-service", "user-created", new { userId = Guid.NewGuid() }), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(_appUserService.Synced, Is.Null);
            Assert.That(_appUserService.RemovedId, Is.Null);
        });
    }

    [Test]
    public void HandleAsync_InvalidUserId_Throws()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.HandleAsync(Envelope(DefaultSources.IdentityHub, "user-created", new { userId = "not-a-guid" }), CancellationToken.None));
    }

    [Test]
    public void HandleAsync_ServiceFailure_Throws()
    {
        _appUserService.SyncResult = MethodResponse<AppUser>.Failure(new Error(ErrorDefaults.Codes.SaveFailed, ErrorDefaults.Messages.SaveFailed));

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.HandleAsync(Envelope(DefaultSources.IdentityHub, "user-created", new { userId = Guid.NewGuid() }), CancellationToken.None));
    }

    private class FakeAppUserService : IAppUserService
    {
        public AppUser? Synced { get; private set; }
        public Guid? RemovedId { get; private set; }
        public IMethodResponse<AppUser>? SyncResult { get; set; }

        public Task<IMethodResponse<AppUser>> SyncFromIdentityAsync(AppUser appUser)
        {
            Synced = appUser;
            return Task.FromResult(SyncResult ?? MethodResponse<AppUser>.Success(appUser));
        }

        public Task<IMethodResponse<bool>> RemoveFromIdentityAsync(Guid id)
        {
            RemovedId = id;
            return Task.FromResult<IMethodResponse<bool>>(MethodResponse<bool>.Success(true));
        }

        public Task<IMethodResponse<IEnumerable<AppUser>>> GetAllAsync(Guid actor = default) => throw new NotSupportedException();

        public Task<IMethodResponse<IEnumerable<AppUser>>> GetAllByPageAsync(int pageNr, int pageSize, Guid actor = default) => throw new NotSupportedException();

        public Task<IMethodResponse<int>> GetCountAsync(Guid actor = default) => throw new NotSupportedException();

        public Task<IMethodResponse<bool>> ExistsAsync(Guid id, Guid actor = default) => throw new NotSupportedException();

        public Task<IMethodResponse<AppUser>> GetByIdAsync(Guid id, Guid actor = default) => throw new NotSupportedException();

        public Task<IMethodResponse<AppUser>> CreateAsync(AppUser entity, Guid actor = default) => throw new NotSupportedException();

        public Task<IMethodResponse<AppUser>> UpdateAsync(Guid id, AppUser entity, string? expectedConcurrencyToken = null, Guid actor = default) => throw new NotSupportedException();

        public Task<IMethodResponse<bool>> RemoveAsync(Guid id, string? expectedConcurrencyToken = null, Guid actor = default) => throw new NotSupportedException();
    }
}
