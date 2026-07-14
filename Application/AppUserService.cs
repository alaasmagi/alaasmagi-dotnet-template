using Base.Application;
using Base.Contracts.DTO;
using Contracts.Application;
using Contracts.DataAccess;
using Domain;

namespace Application;

public class AppUserIdentityMapper : IMapper<AppUser, AppUser>
{
    public AppUser? Map(AppUser? entity)
    {
        return entity;
    }

    public IEnumerable<AppUser>? Map(IEnumerable<AppUser>? entities)
    {
        return entities;
    }
}

public class AppUserService(
    IAppUserRepository appUserRepository,
    Base.Contracts.DataAccess.IBaseUow uow,
    IMapper<AppUser, AppUser> mapper)
    : BaseService<AppUser, AppUser, IAppUserRepository>(uow, appUserRepository, mapper), IAppUserService
{
}
