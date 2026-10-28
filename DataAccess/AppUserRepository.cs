using Base.Contracts.DTO;
using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess;

namespace DataAccess;

public class AppUserRepository : BaseRepository<AppUser, AppUserEntity, IMapper<AppUser, AppUserEntity>>, IAppUserRepository
{
    public AppUserRepository(AppDbContext repositoryDbContext, IMapper<AppUser, AppUserEntity> repositoryMapper)
        : base(repositoryDbContext, repositoryMapper)
    {
    }
}
