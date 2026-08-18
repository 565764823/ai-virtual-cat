using AiCat.Data.Models;

namespace AiCat.Data.Repositories
{
    /// <summary>心情记录仓储（薄封装，心情记录任务再加业务方法）。</summary>
    public sealed class MoodRecordRepository : SqliteRepository<MoodRecord>
    {
        public MoodRecordRepository(IDatabase db) : base(db) { }
    }
}
