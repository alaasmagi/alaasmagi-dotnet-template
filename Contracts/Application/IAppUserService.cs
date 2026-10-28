using Base.Contracts.Application;
using Base.Contracts.DTO;
using Domain;

namespace Contracts.Application;

public interface IAppUserService : IBaseService<AppUser>
{
    Task<IMethodResponse<AppUser>> SyncFromIdentityAsync(AppUser appUser);

    Task<IMethodResponse<bool>> RemoveFromIdentityAsync(Guid id);
}
