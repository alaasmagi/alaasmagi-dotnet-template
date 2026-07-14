using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class AppUserRepository : BaseRepository<AppUser, AppUserEntity, IMapper<AppUser, AppUserEntity>>, IAppUserRepository
{
    private readonly AppDbContext _context;
    private readonly IMapper<AppUser, AppUserEntity> _mapper;

    public AppUserRepository(AppDbContext repositoryDbContext, IMapper<AppUser, AppUserEntity> repositoryMapper)
        : base(repositoryDbContext, repositoryMapper)
    {
        _context = repositoryDbContext;
        _mapper = repositoryMapper;
    }

    public async Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id, ct);

        return _mapper.Map(entity);
    }

    public async Task<AppUser> UpsertFromIdentityEventAsync(
        Guid id,
        string? email,
        string? username,
        string? fullName,
        string? locale,
        CancellationToken ct = default)
    {
        var entity = await _context.AppUsers
            .FirstOrDefaultAsync(user => user.Id == id, ct);

        if (entity == null)
        {
            entity = new AppUserEntity
            {
                Id = id,
                Email = Normalize(email),
                Username = Normalize(username),
                FullName = Normalize(fullName),
                Locale = NormalizeLocale(locale)
            };
            StampNew(entity);
            _context.AppUsers.Add(entity);
        }
        else
        {
            entity.Email = Normalize(email);
            entity.Username = Normalize(username);
            entity.FullName = Normalize(fullName);
            entity.Locale = NormalizeLocale(locale);
            StampExisting(entity);
        }

        await _context.SaveChangesAsync(ct);
        return _mapper.Map(entity)!;
    }

    public async Task<bool> DeleteFromIdentityEventAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.AppUsers
            .FirstOrDefaultAsync(user => user.Id == id, ct);

        if (entity == null)
        {
            return false;
        }

        _context.AppUsers.Remove(entity);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string NormalizeLocale(string? locale)
    {
        return string.IsNullOrWhiteSpace(locale) ? "et" : locale.Trim();
    }

    private static void StampNew(AppUserEntity entity)
    {
        var now = DateTime.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        entity.CreatedBy = "keycloak";
        entity.UpdatedBy = "keycloak";
        entity.ConcurrencyToken = Guid.NewGuid().ToString("N");
    }

    private static void StampExisting(AppUserEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "keycloak";
        entity.ConcurrencyToken = Guid.NewGuid().ToString("N");
    }
}
