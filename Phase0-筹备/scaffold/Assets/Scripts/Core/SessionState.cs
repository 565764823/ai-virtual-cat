using AiCat.Data.Models;

namespace AiCat.Core
{
    /// <summary>
    /// 运行时会话状态：当前用户 + 当前领养猫。
    /// 进程级单例，由 AppBootstrapper 启动时赋值，供所有 Screen / Service 读取。
    /// </summary>
    public sealed class SessionState
    {
        /// <summary>当前用户（游客）。</summary>
        public User CurrentUser { get; private set; }

        /// <summary>当前领养猫；未领养时为 null（应进入选猫界面）。</summary>
        public Cat CurrentCat { get; private set; }

        /// <summary>是否已有领养猫。</summary>
        public bool HasCat => CurrentCat != null;

        public void Set(User user, Cat cat)
        {
            CurrentUser = user;
            CurrentCat = cat;
        }
    }
}
