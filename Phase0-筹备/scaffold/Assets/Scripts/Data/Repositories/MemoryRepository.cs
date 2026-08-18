using AiCat.Data.Models;

namespace AiCat.Data.Repositories
{
    /// <summary>长期记忆仓储（薄封装，P2 启用）。</summary>
    public sealed class MemoryRepository : SqliteRepository<Memory>
    {
        public MemoryRepository(IDatabase db) : base(db) { }
    }
}
