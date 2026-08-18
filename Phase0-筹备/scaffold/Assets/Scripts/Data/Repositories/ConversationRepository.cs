using AiCat.Data.Models;

namespace AiCat.Data.Repositories
{
    /// <summary>会话仓储（薄封装，业务方法在聊天扩展任务再加）。</summary>
    public sealed class ConversationRepository : SqliteRepository<Conversation>
    {
        public ConversationRepository(IDatabase db) : base(db) { }
    }
}
