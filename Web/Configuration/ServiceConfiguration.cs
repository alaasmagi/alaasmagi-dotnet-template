using System.Text.Json;
using Application;
using Base.Cache;
using Base.Contracts.DataAccess;
using Base.Contracts.DTO;
using Base.Contracts.Cache;
using Base.Contracts.Message;
using Base.Keycloak.Authentication;
using Base.Message.RabbitMQ;
using Contracts.Application;
using Contracts.DataAccess;
using Contracts.External;
using DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess;
using DTO.DataAccess.Mappers;
using DTO.Web;
using DTO.Web.Mappers;
using External.Cache;
using External.RabbitMQ;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Web.Configuration;

public static class ServiceConfiguration
{
    public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
    {
        var keycloakOptions = RequiredConfiguration.KeycloakOptions();
        var rabbitMqOptions = RequiredConfiguration.RabbitMqOptions();
        var appMessagingOptions = RequiredConfiguration.AppMessagingOptions(keycloakOptions);
        var redisConnectionString = RequiredConfiguration.RedisConnectionString();
        var baseCacheOptions = RequiredConfiguration.BaseCacheOptions();

        builder.ConfigureApplicationLogging();
        builder.ConfigureGlitchtip();

        builder.Services
            .AddDataAccess()
            .AddApplicationServices()
            .AddApplicationAuthentication(keycloakOptions)
            .AddApplicationCache(redisConnectionString, baseCacheOptions)
            .AddApplicationMessaging(rabbitMqOptions, appMessagingOptions)
            .AddApplicationMvc();

        builder.Services.ConfigureForwardedHeaders();
    }

    private static IServiceCollection AddApplicationCache(
        this IServiceCollection services,
        string redisConnectionString,
        BaseCacheOptions baseCacheOptions)
    {
        services.AddSingleton(baseCacheOptions);
        services.AddSingleton<IBaseCacheSerializer, BaseJsonCacheSerializer>();
        services.AddSingleton<IBaseCacheKeyBuilder, BaseCacheKeyBuilder>();
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddSingleton<IAppCache, RedisCache>();
        services.AddSingleton<IBaseCache>(provider => provider.GetRequiredService<IAppCache>());

        return services;
    }

    private static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(RequiredConfiguration.AppConnectionString()));
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddScoped<IMapper<AppUser, AppUserEntity>, AppUserEntityMapper>();
        services.AddScoped<IMapper<AppUser, AppUser>, AppUserIdentityMapper>();
        services.AddScoped<IMapper<AppUserDto, AppUser>, AppUserDtoMapper>();

        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IBaseUow, DataAccessUow>();

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAppUserService, AppUserService>();

        return services;
    }

    private static IServiceCollection AddApplicationAuthentication(
        this IServiceCollection services,
        KeycloakOptions keycloakOptions)
    {
        services.AddHttpContextAccessor();
        services.AddKeycloakOidc(keycloakOptions);
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }

    private static IServiceCollection AddApplicationMessaging(
        this IServiceCollection services,
        RabbitMqOptions rabbitMqOptions,
        AppMessagingOptions appMessagingOptions)
    {
        services.Configure<HostOptions>(options =>
        {
            options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
        });

        services.AddSingleton(appMessagingOptions);
        services.AddRabbitMqPublisher(rabbitMqOptions);
        services.AddSingleton<IAppEventPublisher, External.RabbitMQ.RabbitMqEventPublisher>();
        services.AddSingleton<IBaseEventHandler<JsonElement>, RabbitMqEventHandler>();
        services.AddRabbitMqConsumer<RabbitMqEventConsumer>();

        return services;
    }

    private static IServiceCollection AddApplicationMvc(this IServiceCollection services)
    {
        services.AddControllersWithViews()
            .AddRazorOptions(options =>
            {
                options.ViewLocationFormats.Clear();
                options.ViewLocationFormats.Add("/MVC/Views/{1}/{0}.cshtml");
                options.ViewLocationFormats.Add("/MVC/Views/Shared/{0}.cshtml");
            });

        return services;
    }

    private static void ConfigureForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost;

            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }
}
