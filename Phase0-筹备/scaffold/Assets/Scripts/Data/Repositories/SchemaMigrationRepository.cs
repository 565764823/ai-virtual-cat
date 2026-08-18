using AiCat.Data.Models;

namespace AiCat.Data.Repositories
{
    /// <summary>迁移审计仓储（只读用途，写入由 DatabaseMigrator 负责）。</summary>
    public sealed class SchemaMigrationRepository : SqliteRepository<SchemaMigration>
    {
        public SchemaMigrationRepository(IDatabase db) : base(db) { }
    }
}
