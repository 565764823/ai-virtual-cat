using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace AiCat.Data.Repositories
{
    /// <summary>
    /// 泛型 CRUD 仓储接口。所有方法经 IDatabase.RunAsync 走线程池。
    /// </summary>
    public interface IRepository<T> where T : class, new()
    {
        UniTask<T> GetByIdAsync(long id);

        UniTask<List<T>> GetAllAsync();

        UniTask<int> InsertAsync(T entity);

        UniTask<int> UpdateAsync(T entity);

        UniTask<int> DeleteAsync(long id);
    }
}
