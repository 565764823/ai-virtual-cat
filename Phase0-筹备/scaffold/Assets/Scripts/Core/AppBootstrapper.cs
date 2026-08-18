using AiCat.Core.Services;
using AiCat.Data.Migration;
using AiCat.UI;
using AiCat.UI.Screens;
using Cysharp.Threading.Tasks;

namespace AiCat.Core
{
    /// <summary>
    /// 启动编排：迁移建库 → 判档（游客/领养猫）→ 决定进入选猫界面还是主界面。
    /// </summary>
    public sealed class AppBootstrapper
    {
        private readonly DatabaseMigrator _migrator;
        private readonly IUserService _users;
        private readonly IAdoptionService _adoption;
        private readonly SessionState _session;
        private readonly ScreenManager _screens;

        public AppBootstrapper(
            DatabaseMigrator migrator,
            IUserService users,
            IAdoptionService adoption,
            SessionState session,
            ScreenManager screens)
        {
            _migrator = migrator;
            _users = users;
            _adoption = adoption;
            _session = session;
            _screens = screens;
        }

        public async UniTask StartAsync()
        {
            await _migrator.MigrateAsync();

            var user = await _users.GetOrCreateGuestAsync();
            var cat = await _adoption.GetAdoptedCatAsync(user.Id);
            _session.Set(user, cat);

            if (cat == null)
                _screens.Show<CatSelectScreen>();
            else
                _screens.Show<MainScreen>();
        }
    }
}
