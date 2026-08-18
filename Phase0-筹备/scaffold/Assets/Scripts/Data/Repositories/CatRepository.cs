using System.Collections.Generic;
using System.Linq;
using AiCat.Data.Models;
using Cysharp.Threading.Tasks;

namespace AiCat.Data.Repositories
{
    /// <summary>猫咪仓储（选猫领养）。</summary>
    public interface ICatRepository : IRepository<Cat>
    {
        /// <summary>某用户的全部猫。</summary>
        UniTask<List<Cat>> GetCatsByUserAsync(long userId);

        /// <summary>某用户已领养的猫（is_adopted=1），无则 null。</summary>
        UniTask<Cat> GetAdoptedCatAsync(long userId);

        /// <summary>领养：置 is_adopted=1 并插入，返回新主键 id。</summary>
        UniTask<int> AdoptAsync(Cat cat);
    }

    public sealed class CatRepository : SqliteRepository<Cat>, ICatRepository
    {
        public CatRepository(IDatabase db) : base(db) { }

        public UniTask<List<Cat>> GetCatsByUserAsync(long userId)
        {
            return Db.RunAsync(conn => conn.Table<Cat>().Where(c => c.UserId == userId).ToList());
        }

        public UniTask<Cat> GetAdoptedCatAsync(long userId)
        {
            return Db.RunAsync(conn =>
                conn.Table<Cat>().Where(c => c.UserId == userId && c.IsAdopted).FirstOrDefault());
        }

        public UniTask<int> AdoptAsync(Cat cat)
        {
            cat.IsAdopted = true;
            return Db.RunAsync(conn => conn.Insert(cat));
        }
    }
}
