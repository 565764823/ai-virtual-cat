using AiCat.Data.Models;

namespace AiCat.Data.Repositories
{
    /// <summary>月度回忆仓储（薄封装，P4 启用）。</summary>
    public sealed class MonthlyBookRepository : SqliteRepository<MonthlyBook>
    {
        public MonthlyBookRepository(IDatabase db) : base(db) { }
    }
}
