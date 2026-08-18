using AiCat.Data.Models;

namespace AiCat.Data.Repositories
{
    /// <summary>健康记录仓储（薄封装，P2+ 启用）。</summary>
    public sealed class HealthRecordRepository : SqliteRepository<HealthRecord>
    {
        public HealthRecordRepository(IDatabase db) : base(db) { }
    }
}
