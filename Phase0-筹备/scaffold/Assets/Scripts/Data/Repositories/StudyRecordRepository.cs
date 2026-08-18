using AiCat.Data.Models;

namespace AiCat.Data.Repositories
{
    /// <summary>学习记录仓储（薄封装，P2+ 启用）。</summary>
    public sealed class StudyRecordRepository : SqliteRepository<StudyRecord>
    {
        public StudyRecordRepository(IDatabase db) : base(db) { }
    }
}
