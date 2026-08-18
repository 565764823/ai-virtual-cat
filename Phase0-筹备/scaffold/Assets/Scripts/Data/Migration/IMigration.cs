namespace AiCat.Data.Migration
{
    /// <summary>
    /// 单条数据库迁移：一个版本 = 一段原子 DDL。
    /// 与 Phase 0 的 migrations/*.sql 一一对应；DDL 内嵌为 C# 字符串数组，
    /// 避免 StreamingAssets 缺文件 / 路径坑，checksum 自洽。
    /// </summary>
    public interface IMigration
    {
        /// <summary>迁移版本号（与 PRAGMA user_version 对应，从 1 起递增）。</summary>
        int Version { get; }

        /// <summary>迁移文件名（如 0001_init.sql）。</summary>
        string Name { get; }

        /// <summary>迁移说明。</summary>
        string Description { get; }

        /// <summary>逐条 DDL（每个元素一条 CREATE/INDEX 语句，避免按 ';' 切分踩坑）。</summary>
        string[] SqlStatements { get; }
    }
}
