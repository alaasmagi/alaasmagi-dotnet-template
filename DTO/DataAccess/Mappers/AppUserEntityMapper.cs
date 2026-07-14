using Base.Contracts.DTO;
using Domain;

namespace DTO.DataAccess.Mappers;

public class AppUserEntityMapper : IMapper<AppUser, AppUserEntity>
{
    public AppUser? Map(AppUserEntity? entity)
    {
        return entity == null ? null : new AppUser
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy,
            ConcurrencyToken = entity.ConcurrencyToken,
            Email = entity.Email,
            Username = entity.Username,
            FullName = entity.FullName,
            Locale = entity.Locale,
            BankIban = entity.BankIban
        };
    }

    public IEnumerable<AppUser>? Map(IEnumerable<AppUserEntity>? entities)
    {
        return entities?.Select(entity => Map(entity)!);
    }

    public AppUserEntity? Map(AppUser? entity)
    {
        return entity == null ? null : new AppUserEntity
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy,
            ConcurrencyToken = entity.ConcurrencyToken,
            Email = entity.Email,
            Username = entity.Username,
            FullName = entity.FullName,
            Locale = entity.Locale,
            BankIban = entity.BankIban
        };
    }

    public IEnumerable<AppUserEntity>? Map(IEnumerable<AppUser>? entities)
    {
        return entities?.Select(entity => Map(entity)!);
    }
}
