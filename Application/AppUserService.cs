using Base.Application;
using Base.Contracts.DataAccess;
using Base.Contracts.DTO;
using Base.DTO;
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
    IBaseUow uow,
    IMapper<AppUser, AppUser> mapper)
    : BaseService<AppUser, AppUser, IAppUserRepository>(uow, appUserRepository, mapper), IAppUserService
{
    public async Task<IMethodResponse<AppUser>> SyncFromIdentityAsync(AppUser appUser)
    {
        var existing = await ServiceRepository.GetByIdAsync(appUser.Id);

        IMethodResponse<AppUser> staged;
        if (existing.Successful)
        {
            staged = await ServiceRepository.UpdateAsync(appUser.Id, appUser, existing.Value!.ConcurrencyToken);
        }
        else if (existing.Error?.Code == ErrorDefaults.Codes.NotFound)
        {
            staged = await ServiceRepository.CreateAsync(appUser);
        }
        else
        {
            return existing;
        }

        if (!staged.Successful)
        {
            return staged;
        }

        var saved = await ServiceUow.TrySaveChangesAsync();
        return saved.Successful ? staged : MethodResponse<AppUser>.Failure(saved.Error!);
    }

    public async Task<IMethodResponse<bool>> RemoveFromIdentityAsync(Guid id)
    {
        var existing = await ServiceRepository.GetByIdAsync(id);
        if (!existing.Successful)
        {
            // An already missing user is the desired end state of a delete event.
            return existing.Error?.Code == ErrorDefaults.Codes.NotFound
                ? MethodResponse<bool>.Success(false)
                : MethodResponse<bool>.Failure(existing.Error!);
        }

        var removed = await ServiceRepository.RemoveAsync(id, existing.Value!.ConcurrencyToken);
        if (!removed.Successful)
        {
            return removed;
        }

        var saved = await ServiceUow.TrySaveChangesAsync();
        return saved.Successful ? removed : MethodResponse<bool>.Failure(saved.Error!);
    }
}
