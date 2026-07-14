# alaasmagi-dotnet-template

A .NET 10 backend solution template for Alaasmagi services. The template is shaped after the existing
`email-hub` and `invoice-service` backends and uses the `alaasmagi.Base.*` packages as the default
foundation for domain entities, DTO mapping, EF repositories, application services, Keycloak
authentication, Redis caching, and RabbitMQ messaging.

The template includes one real cross-project entity, `AppUser`, because every service needs a local
projection of the authenticated Keycloak user.

## Features

- Clean layered solution split into `Domain`, `DTO`, `Contracts`, `DataAccess`, `Application`,
  `External`, and `Web`.
- `AppUser` domain model, EF entity, web DTO, bidirectional mappers, repository, application service,
  and CRUD API controller.
- Keycloak OpenID Connect authentication through `alaasmagi.Base.Keycloak`.
- RabbitMQ publishing and consuming through `alaasmagi.Base.Message.RabbitMQ`.
- Redis-ready caching through `alaasmagi.Base.Cache`.
- Keycloak user lifecycle event consumer that keeps the local `AppUsers` table synchronized.
- MVC shell with login/logout and health endpoint.
- API endpoints under `/api`.
- Health endpoint at `/health`.
- Swagger UI at `/swagger`.
- URL-segment API versioning with `/api/v1/...` routes.
- Fixed-window API rate limiting.
- PostgreSQL EF Core context with design-time factory for migrations.
- Environment-driven configuration loaded from `.env`.
- Complete `.env.example` with example values for database, Redis, Keycloak, RabbitMQ, Glitchtip, and ports.
- Central build settings in `Directory.Build.props`.

## Technology

- .NET 10 / C# latest
- ASP.NET Core MVC and API controllers
- Entity Framework Core with PostgreSQL
- Keycloak OIDC authentication
- Redis caching
- RabbitMQ topic-exchange messaging
- Sentry/Glitchtip-compatible error reporting
- `alaasmagi.Base.*` version `1.1.9`

## Folder Tree

```text
.
|-- Application
|   `-- AppUserService.cs
|-- Contracts
|   |-- Application
|   |   `-- IAppUserService.cs
|   |-- DataAccess
|   |   `-- IAppUserRepository.cs
|   `-- External
|       |-- IAppCache.cs
|       `-- IAppEventPublisher.cs
|-- DTO
|   |-- DataAccess
|   |   |-- AppUserEntity.cs
|   |   `-- Mappers
|   |       `-- AppUserEntityMapper.cs
|   `-- Web
|       |-- AppUserDto.cs
|       `-- Mappers
|           `-- AppUserDtoMapper.cs
|-- DataAccess
|   |-- AppUserRepository.cs
|   |-- DataAccessUow.cs
|   `-- Context
|       |-- AppDbContext.cs
|       `-- AppDbContextFactory.cs
|-- Domain
|   `-- AppUser.cs
|-- External
|   |-- Cache
|   |   `-- RedisCache.cs
|   `-- RabbitMQ
|       |-- AppMessagingOptions.cs
|       |-- RabbitMqEventConsumer.cs
|       |-- RabbitMqEventHandler.cs
|       `-- RabbitMqEventPublisher.cs
|-- Web
|   |-- API
|   |   `-- Controllers
|   |       `-- AppUsersController.cs
|   |-- Configuration
|   |   |-- DotEnvConfiguration.cs
|   |   |-- GlitchtipConfiguration.cs
|   |   |-- LoggingConfiguration.cs
|   |   |-- RequiredConfiguration.cs
|   |   `-- ServiceConfiguration.cs
|   |-- MVC
|   |   |-- Controllers
|   |   |-- Models
|   |   `-- Views
|   `-- Program.cs
|-- .env.example
|-- Directory.Build.props
`-- alaasmagi-dotnet-template.sln
```

## Project Responsibilities

### `Domain`

Contains business/domain models. `AppUser` inherits `BaseEntityWithMetaConcurrency` from
`alaasmagi.Base.Domain`, so the entity has:

- `Id`
- `CreatedAt` / `CreatedBy`
- `UpdatedAt` / `UpdatedBy`
- `ConcurrencyToken`

Use this project for domain concepts and behavior that should not depend on EF, ASP.NET Core, or
transport infrastructure.

### `DTO`

Contains boundary models and mappers.

- `DTO/DataAccess` contains EF persistence entities.
- `DTO/DataAccess/Mappers` maps domain models to EF entities.
- `DTO/Web` contains API/MVC DTOs.
- `DTO/Web/Mappers` maps web DTOs to domain models.

The mapper contract is `IMapper<TUpper, TLower>` from `alaasmagi.Base.Contracts.DTO`.

`AppUserEntity` keeps its persistence constraints as data annotations, by convention:

- `[Required]`
- `[MaxLength(...)]`

`AppDbContext` keeps schema, table, index, and relationship configuration.

### `Contracts`

Contains interfaces only.

- `IAppUserRepository` extends `IBaseRepository<AppUser>`.
- `IAppUserService` extends `IBaseService<AppUser>`.
- `IAppCache` extends `IBaseCache`.
- `IAppEventPublisher` abstracts event publishing.

This preserves the dependency direction: callers depend on contracts, not implementations.

### `DataAccess`

Contains EF Core implementation.

- `AppDbContext` defines the database context.
- `AppDbContextFactory` supports EF Core design-time commands.
- `AppUserRepository` inherits `BaseRepository<AppUser, AppUserEntity, IMapper<AppUser, AppUserEntity>>`
  from `alaasmagi.Base.DataAccess.EF`.
- `DataAccessUow` inherits `BaseUow<AppDbContext>`.

The Base repository stages changes on the DbContext. The Unit of Work commits them.

### `Application`

Contains application service implementations.

`AppUserService` inherits `BaseService<AppUser, AppUser, IAppUserRepository>` from
`alaasmagi.Base.Application`. The application and domain model are the same type here, so the service
uses an identity mapper.

The Base service handles the usual CRUD flow:

- maps application model to domain model
- calls the repository
- saves through `IBaseUow` on successful writes
- re-reads persisted rows after writes
- returns `IMethodResponse<T>` instead of throwing for expected failures

### `External`

Contains infrastructure adapters for systems outside this service.

Redis cache support is built on `alaasmagi.Base.Contracts.Cache`, `alaasmagi.Base.Cache`, and
`StackExchange.Redis`.

- `IAppCache` is the service-level cache abstraction in `Contracts/External`.
- `RedisCache` inherits `BaseCache` and implements Redis byte operations.
- `BaseJsonCacheSerializer` handles typed value serialization.
- `BaseCacheKeyBuilder` handles key validation and prefixing.
- `BaseCacheOptions` configures the cache key prefix and default entry expiration.

RabbitMQ support is built on `alaasmagi.Base.Message` and `alaasmagi.Base.Message.RabbitMQ`.

- `RabbitMqEventPublisher` publishes fixed-schema Base event envelopes.
- `RabbitMqEventConsumer` subscribes to configured routing keys.
- `RabbitMqEventHandler` handles incoming events.
- Keycloak user lifecycle events are consumed as `UserEventContent` from `alaasmagi.Base.Keycloak.Events`
  and synchronized into the local `AppUsers` table.

### `Web`

Composition root and delivery layer.

- `Program.cs` wires the request pipeline.
- `Configuration` loads env vars and registers services.
- `MVC` contains the default web shell and authentication UI.
- `API` contains JSON API controllers.

The template currently exposes `AppUser` CRUD endpoints:

```text
GET    /api/v1/app-users
GET    /api/v1/app-users/{id}
POST   /api/v1/app-users
PUT    /api/v1/app-users/{id}
DELETE /api/v1/app-users/{id}
```

Updates and deletes use optimistic concurrency. Pass the current `ConcurrencyToken` in the `If-Match`
header for `PUT` and `DELETE`.

Operational endpoints:

```text
GET /health
GET /swagger
```

## `alaasmagi.Base.*` Packages

This template intentionally relies on Base packages rather than re-implementing common backend
plumbing.

| Package | Used for |
| --- | --- |
| `alaasmagi.Base.Domain` | Entity base classes such as `BaseEntityWithMetaConcurrency`. |
| `alaasmagi.Base.DTO` | Result/error implementations and DTO helpers. |
| `alaasmagi.Base.Contracts.Application` | `IBaseService<T>` service contract. |
| `alaasmagi.Base.Contracts.Cache` | `IBaseCache`, cache result, serializer, key builder, and entry option contracts. |
| `alaasmagi.Base.Contracts.DataAccess` | `IBaseRepository<T>` and `IBaseUow`. |
| `alaasmagi.Base.DataAccess.EF` | EF Core `BaseRepository` and `BaseUow`. |
| `alaasmagi.Base.Application` | `BaseService` CRUD application-service implementation. |
| `alaasmagi.Base.Cache` | `BaseCache`, JSON serializer, key builder, cache result, and cache options. |
| `alaasmagi.Base.Message` | Event envelope model and default message constants. |
| `alaasmagi.Base.Message.RabbitMQ` | RabbitMQ publisher, consumer base class, and DI helpers. |
| `alaasmagi.Base.Keycloak` | Keycloak OIDC auth and identity event content DTOs. |

Key Base conventions used here:

- Expected failures return `IMethodResponse<T>`.
- Repositories do not save changes directly for normal CRUD.
- Services coordinate repository calls and Unit of Work commits.
- Optimistic concurrency is enabled by entities implementing the Base concurrency contract.
- Cache access goes through string-keyed `IBaseCache` abstractions.
- Messaging uses the fixed envelope shape `{ type, source, action, timestamp, content }`.

## Configuration

Copy `.env.example` to `.env` and replace example values.

Required runtime values:

```text
DATABASE_CONNECTION_STRING
REDIS_CONNECTION_STRING
RABBITMQ_URI
RABBITMQ_QUEUE
RABBITMQ_EXCHANGE
KEYCLOAK_AUTHORITY
KEYCLOAK_CLIENT_ID
KEYCLOAK_CLIENT_SECRET
```

Optional values:

```text
APP_PORT
PORT
ASPNETCORE_URLS
HOST_PORT
CACHE_KEY_PREFIX
CACHE_DEFAULT_ABSOLUTE_EXPIRATION_SECONDS
RABBITMQ_CONSUMER_ROUTING_KEYS
APP_EVENT_SOURCE
GLITCHTIP_DSN
GLITCHTIP_RELEASE
RATE_LIMIT_PERMIT_LIMIT
RATE_LIMIT_WINDOW_SECONDS
RATE_LIMIT_QUEUE_LIMIT
```

`DotEnvConfiguration` searches upward from the current working directory for `.env` and loads values
that are not already present in the process environment.

## Architectural Decisions

### Clean layering

Each project owns one kind of responsibility. Domain does not depend on persistence or web concerns.
Contracts keep dependency direction stable. Implementations live in `Application`, `DataAccess`,
`External`, and `Web`.

### AppUser as the template entity

Every backend needs a local user projection for ownership, display, preferences, and user-scoped data.
`AppUser` is the default model in this template and is synchronized from Keycloak user lifecycle events.

### BaseService and BaseRepository for CRUD

Generic CRUD behavior comes from `alaasmagi.Base.Application` and `alaasmagi.Base.DataAccess.EF`.
New CRUD resources should follow the same pattern:

1. Domain model in `Domain`.
2. Persistence entity in `DTO/DataAccess`.
3. Web DTO in `DTO/Web`.
4. Domain-to-EF mapper in `DTO/DataAccess/Mappers`.
5. Web-to-domain mapper in `DTO/Web/Mappers`.
6. Repository interface in `Contracts/DataAccess`.
7. Service interface in `Contracts/Application`.
8. Repository implementation in `DataAccess`.
9. Service implementation in `Application`.
10. API controller in `Web/API/Controllers`.

### Data annotations for persistence constraints

Persistence DTO constraints such as required fields and max lengths are placed on the entity class with
data annotations. `AppDbContext` is reserved for schema/table/index/relationship configuration.

### Environment variables over appsettings for secrets

Secrets and deployment-specific values are read from environment variables. `appsettings.json` should
remain non-secret and generic.

### RabbitMQ topology is configuration-driven

The service reads RabbitMQ host/credentials/exchange/queue/routing keys from env vars. Event payloads
use the Base fixed envelope, so only `content` varies per event type.

### Redis cache adapter lives under External

The template treats cache storage as external infrastructure. The contract is `IAppCache` in
`Contracts/External`, and the Redis implementation lives in `External/Cache`. The implementation
inherits `BaseCache`, so provider-neutral behavior such as serialization, key prefixing, cache results,
and cache-aside `GetOrSetAsync` comes from `alaasmagi.Base.Cache`.

The default cache entry uses an absolute expiration relative to now. Configure Redis and default cache
behavior with:

```text
REDIS_CONNECTION_STRING
CACHE_KEY_PREFIX
CACHE_DEFAULT_ABSOLUTE_EXPIRATION_SECONDS
```

### Web and API live in the same host

The template supports both MVC and JSON API endpoints from one ASP.NET Core host. This matches the
existing Alaasmagi backends and keeps authentication, configuration, and dependency injection in one
composition root.

### API versioning

APIs use URL-segment versioning through `Asp.Versioning.Mvc` and `Asp.Versioning.Mvc.ApiExplorer`.
The first version is exposed as `/api/v1/...`. Version reporting is enabled, so responses include the
supported API version headers.

### API rate limiting

API controllers use ASP.NET Core's built-in rate limiting middleware with a named fixed-window policy.
The default is 100 requests per 60 seconds with no queueing. Configure it with:

```text
RATE_LIMIT_PERMIT_LIMIT
RATE_LIMIT_WINDOW_SECONDS
RATE_LIMIT_QUEUE_LIMIT
```

## Local Development

Restore and build:

```bash
dotnet restore alaasmagi-dotnet-template.sln
dotnet build alaasmagi-dotnet-template.sln
```

Run the web host:

```bash
dotnet run --project Web/Web.csproj
```

Create a migration after changing persistence entities:

```bash
dotnet ef migrations add DbMigration-v1 --project DataAccess/DataAccess.csproj --startup-project Web/Web.csproj --context AppDbContext
```

Apply migrations:

```bash
dotnet ef database update --project DataAccess/DataAccess.csproj --startup-project Web/Web.csproj --context AppDbContext
```

## Adding a New Feature

Use `AppUser` as the reference implementation. For a new feature, add matching files across the same
layers and wire them in `Web/Configuration/ServiceConfiguration.cs`.

Prefer Base implementations first:

- Use `BaseEntity...` types for domain and persistence models.
- Use `BaseRepository` for EF repositories.
- Use `BaseService` for CRUD application services.
- Use `IAppCache` / `IBaseCache` for cache reads and writes.
- Use `BaseEventEnvelope<TContent>` for published events.
- Use `RabbitMqConsumerBase<TContent>` for consumers.

Only write custom behavior where the feature differs from generic CRUD or generic messaging.
