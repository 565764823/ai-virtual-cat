using AiCat.Data.Models;

namespace AiCat.Data.Repositories
{
    /// <summary>消息仓储（薄封装）。</summary>
    public sealed class MessageRepository : SqliteRepository<Message>
    {
        public MessageRepository(IDatabase db) : base(db) { }
    }
}
