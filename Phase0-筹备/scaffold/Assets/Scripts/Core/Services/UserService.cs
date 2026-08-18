using AiCat.Data.Models;
using AiCat.Data.Repositories;
using Cysharp.Threading.Tasks;

namespace AiCat.Core.Services
{
    /// <summary>用户领域服务（游客存档）。</summary>
    public interface IUserService
    {
        /// <summary>取游客账号，无则创建。</summary>
        UniTask<User> GetOrCreateGuestAsync();
    }

    public sealed class UserService : IUserService
    {
        private readonly IUserRepository _users;

        public UserService(IUserRepository users)
        {
            _users = users;
        }

        public UniTask<User> GetOrCreateGuestAsync()
        {
            return _users.GetOrCreateGuestAsync();
        }
    }
}
