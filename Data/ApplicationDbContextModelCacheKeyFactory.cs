// Project comment: ApplicationDbContextModelCacheKeyFactory.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace IT_Form.Data;

/// <summary>
/// Defines the ApplicationDbContextModelCacheKeyFactory class used by the IT Form application.
/// </summary>
public sealed class ApplicationDbContextModelCacheKeyFactory : IModelCacheKeyFactory
{
    /// <summary>
    /// Creates data for the Create operation.
    /// </summary>
    public object Create(DbContext context, bool designTime)
    {
        if (context is ApplicationDbContext applicationDbContext)
        {
            return (context.GetType(), applicationDbContext.EncryptionModelCacheKey, designTime);
        }

        return (context.GetType(), designTime);
    }
}
