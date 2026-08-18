using AiCat.AI;
using AiCat.Core.Services;
using AiCat.Data.Migration;
using AiCat.Data.Repositories;
using AiCat.UI;
using VContainer;
using VContainer.Unity;

namespace AiCat.Core.DI
{
    /// <summary>
    /// 应用级依赖容器（进程级单例注册）。
    /// 挂在入口 GameObject 上，由 <see cref="AiCat.AppBootstrap"/> 创建。
    /// </summary>
    public sealed class RootLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // ---- 数据层 ----
            builder.Register<SqliteDatabase>(Lifetime.Singleton).As<IDatabase>();
            builder.Register<DatabaseMigrator>(Lifetime.Singleton);

            // ---- 仓储（User/Cat 走接口，其余薄仓储自注册）----
            builder.Register<UserRepository>(Lifetime.Singleton).As<IUserRepository>();
            builder.Register<CatRepository>(Lifetime.Singleton).As<ICatRepository>();
            builder.Register<ConversationRepository>(Lifetime.Singleton);
            builder.Register<MessageRepository>(Lifetime.Singleton);
            builder.Register<MemoryRepository>(Lifetime.Singleton);
            builder.Register<EmotionLogRepository>(Lifetime.Singleton);
            builder.Register<ReminderRepository>(Lifetime.Singleton);
            builder.Register<MoodRecordRepository>(Lifetime.Singleton);
            builder.Register<StudyRecordRepository>(Lifetime.Singleton);
            builder.Register<HealthRecordRepository>(Lifetime.Singleton);
            builder.Register<AnniversaryRepository>(Lifetime.Singleton);
            builder.Register<MonthlyBookRepository>(Lifetime.Singleton);
            builder.Register<SchemaMigrationRepository>(Lifetime.Singleton);

            // ---- 领域服务 ----
            builder.Register<UserService>(Lifetime.Singleton).As<IUserService>();
            builder.Register<AdoptionService>(Lifetime.Singleton).As<IAdoptionService>();
            builder.Register<CatCatalog>(Lifetime.Singleton).As<ICatCatalog>();
            builder.Register<SessionState>(Lifetime.Singleton);

            // ---- AI 引擎 ----
            builder.Register<AiConfig>(Lifetime.Singleton);
            builder.Register<LlmServerHttpBackend>(Lifetime.Singleton).As<ILlmBackend>();
            // ChatService 构造需要 (ILlmBackend, systemPrompt)，prompt 由人设文件动态加载。
            // 用 Register<T>(factory, Singleton) 而非 RegisterFactory：保证 Resolve<ChatService>() 可用
            // （ChatScreen 经 [Inject] ChatService 注入的是具体类型，非 Func<T>）。
            builder.Register<ChatService>(r =>
            {
                var backend = r.Resolve<ILlmBackend>();
                var config = r.Resolve<AiConfig>();
                string prompt = PersonaPrompt.LoadDefault(config.personaFile);
                return new ChatService(backend, prompt);
            }, Lifetime.Singleton);

            // ---- UI 框架 ----
            builder.Register<UiBuilder>(Lifetime.Singleton);
            builder.Register<ScreenManager>(Lifetime.Singleton);

            // ---- 入口编排 ----
            builder.Register<AppBootstrapper>(Lifetime.Singleton);
        }
    }
}
