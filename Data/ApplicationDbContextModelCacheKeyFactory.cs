using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace IT_Form.Data;

public sealed class ApplicationDbContextModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        if (context is ApplicationDbContext applicationDbContext)
        {
            return (context.GetType(), applicationDbContext.EncryptionModelCacheKey, designTime);
        }

        return (context.GetType(), designTime);
    }
}
