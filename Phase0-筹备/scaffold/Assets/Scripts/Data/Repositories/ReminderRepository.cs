using AiCat.Data.Models;

namespace AiCat.Data.Repositories
{
    /// <summary>提醒仓储（薄封装，提醒任务再加业务方法）。</summary>
    public sealed class ReminderRepository : SqliteRepository<Reminder>
    {
        public ReminderRepository(IDatabase db) : base(db) { }
    }
}
