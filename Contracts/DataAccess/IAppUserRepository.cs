using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IAppUserRepository : IBaseRepository<AppUser>
{
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<AppUser> UpsertFromIdentityEventAsync(
        Guid id,
        string? email,
        string? username,
        string? fullName,
        string? locale,
        CancellationToken ct = default);

    Task<bool> DeleteFromIdentityEventAsync(Guid id, CancellationToken ct = default);
}
