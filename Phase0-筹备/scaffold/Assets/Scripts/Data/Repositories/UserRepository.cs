using System.Linq;
using AiCat.Data.Models;
using AiCat.Data.Serialization;
using Cysharp.Threading.Tasks;

namespace AiCat.Data.Repositories
{
    /// <summary>用户仓储（游客存档）。</summary>
    public interface IUserRepository : IRepository<User>
    {
        /// <summary>取游客账号，无则创建。MVP 单游客模型。</summary>
        UniTask<User> GetOrCreateGuestAsync();
    }

    public sealed class UserRepository : SqliteRepository<User>, IUserRepository
    {
        public UserRepository(IDatabase db) : base(db) { }

        public UniTask<User> GetOrCreateGuestAsync()
        {
            return Db.RunAsync(conn =>
            {
                var guest = conn.Table<User>().FirstOrDefault(u => u.IsGuest);
                if (guest != null)
                {
                    return guest;
                }

                var now = TimeUtil.NowString();
                var user = new User
                {
                    IsGuest = true,
                    DisplayName = "游客",
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                conn.Insert(user);
                return user;
            });
        }
    }
}
