using Base.Contracts.DTO;
using Domain;

namespace DTO.Web.Mappers;

public class AppUserDtoMapper : IMapper<AppUserDto, AppUser>
{
    public AppUserDto? Map(AppUser? entity)
    {
        return entity == null ? null : new AppUserDto
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

    public IEnumerable<AppUserDto>? Map(IEnumerable<AppUser>? entities)
    {
        return entities?.Select(entity => Map(entity)!);
    }

    public AppUser? Map(AppUserDto? entity)
    {
        return entity == null ? null : new AppUser
        {
            Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy ?? string.Empty,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy ?? string.Empty,
            ConcurrencyToken = entity.ConcurrencyToken ?? string.Empty,
            Email = entity.Email,
            Username = entity.Username,
            FullName = entity.FullName,
            Locale = entity.Locale,
            BankIban = entity.BankIban
        };
    }

    public IEnumerable<AppUser>? Map(IEnumerable<AppUserDto>? entities)
    {
        return entities?.Select(entity => Map(entity)!);
    }
}
