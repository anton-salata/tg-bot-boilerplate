using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace TgBotBoileplate.DAL.Repositories
{
    public interface IRepository<TEntity>
    {
        Task<IEnumerable<TEntity>> GetAll();
        Task<TEntity> GetById(Guid id);
        Task<TEntity> Get(Expression<Func<TEntity, bool>> filter);
        Task Insert(TEntity entity);
        Task Update(Guid id, TEntity entity);
        Task Delete(Guid id);
        Task DeleteMany(Expression<Func<TEntity, bool>> filter);
    }
}
