using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SQLite;

namespace AiCat.Data.Repositories
{
    /// <summary>
    /// sqlite-net 泛型 CRUD 基类。只做 ORM 映射（DDL 归迁移 SQL，绝不用 CreateTable&lt;T&gt;）。
    /// </summary>
    public abstract class SqliteRepository<T> : IRepository<T> where T : class, new()
    {
        protected readonly IDatabase Db;

        protected SqliteRepository(IDatabase db)
        {
            Db = db;
        }

        public UniTask<T> GetByIdAsync(long id)
        {
            return Db.RunAsync(conn => conn.Find<T>(id));
        }

        public UniTask<List<T>> GetAllAsync()
        {
            return Db.RunAsync(conn => conn.Table<T>().ToList());
        }

        public UniTask<int> InsertAsync(T entity)
        {
            return Db.RunAsync(conn => conn.Insert(entity));
        }

        public UniTask<int> UpdateAsync(T entity)
        {
            return Db.RunAsync(conn => conn.Update(entity));
        }

        public UniTask<int> DeleteAsync(long id)
        {
            return Db.RunAsync(conn => conn.Delete<T>(id));
        }
    }
}
