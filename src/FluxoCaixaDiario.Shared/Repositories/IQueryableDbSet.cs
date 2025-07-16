using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.Shared.Repositories
{
    public interface IQueryableDbSet<TEntity> : IQueryable<TEntity> where TEntity : class
    {
        ValueTask<TEntity?> FindAsync(params object?[]? keyValues);
        ValueTask<TEntity?> FindAsync(object?[]? keyValues, CancellationToken cancellationToken);
    }
}
