using Base.DataAccess.EF;
using DataAccess.Context;

namespace DataAccess;

public class DataAccessUow : BaseUow<AppDbContext>
{
    public DataAccessUow(AppDbContext uowDbContext) : base(uowDbContext)
    {
    }
}
