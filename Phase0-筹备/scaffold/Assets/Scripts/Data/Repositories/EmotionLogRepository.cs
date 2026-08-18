using AiCat.Data.Models;

namespace AiCat.Data.Repositories
{
    /// <summary>情绪记录仓储（薄封装，P2 启用）。</summary>
    public sealed class EmotionLogRepository : SqliteRepository<EmotionLog>
    {
        public EmotionLogRepository(IDatabase db) : base(db) { }
    }
}
