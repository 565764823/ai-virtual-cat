using AiCat.Data.Models;

namespace AiCat.Data.Repositories
{
    /// <summary>纪念日仓储（薄封装，P4 启用）。</summary>
    public sealed class AnniversaryRepository : SqliteRepository<Anniversary>
    {
        public AnniversaryRepository(IDatabase db) : base(db) { }
    }
}
