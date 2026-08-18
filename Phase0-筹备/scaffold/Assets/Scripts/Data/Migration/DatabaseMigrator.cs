using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AiCat.Data.Repositories;
using AiCat.Data.Serialization;
using Cysharp.Threading.Tasks;
using SQLite;

namespace AiCat.Data.Migration
{
    /// <summary>
    /// 数据库迁移器：对齐 Phase 0 的 init_db.py / migrations.md 机制。
    /// user_version 为权威版本号，schema_migrations 为审计表，单迁移单事务，迁移前备份。
    /// </summary>
    public sealed class DatabaseMigrator
    {
        private readonly IDatabase _db;

        public DatabaseMigrator(IDatabase db)
        {
            _db = db;
        }

        /// <summary>
        /// 执行全部未应用的迁移。幂等：二次启动跳过已应用版本。
        /// </summary>
        public UniTask MigrateAsync()
        {
            return _db.RunAsync(Migrate);
        }

        private void Migrate(SQLiteConnection conn)
        {
            // 1. 完整性检查（失败抛异常，自恢复见 Phase 1 安全任务）
            var integrity = conn.ExecuteScalar<string>("PRAGMA integrity_check;");
            if (!string.Equals(integrity, "ok", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"数据库完整性检查失败：{integrity}");
            }

            // 外键约束每个连接都要重开（非持久）
            conn.Execute("PRAGMA foreign_keys = ON;");

            // 2. 读取当前版本
            var current = conn.ExecuteScalar<int>("PRAGMA user_version;");

            // 3. 逐条应用未迁移版本
            foreach (var m in Migrations.All)
            {
                if (m.Version <= current)
                {
                    continue;
                }

                ApplyMigration(conn, m, current);
                current = m.Version;
            }
        }

        private void ApplyMigration(SQLiteConnection conn, IMigration m, int current)
        {
            // 迁移前备份（此刻库处于一致已提交状态，无活动事务）
            var backupPath = $"{_db.FilePath}.bak.{current}";
            File.Copy(_db.FilePath, backupPath, overwrite: true);

            try
            {
                conn.BeginTransaction();
                foreach (var sql in m.SqlStatements)
                {
                    conn.Execute(sql);
                }

                conn.Execute(
                    "INSERT INTO schema_migrations (version, name, description, checksum, applied_at) VALUES (?, ?, ?, ?, ?)",
                    m.Version, m.Name, m.Description, ComputeChecksum(m), TimeUtil.NowString());

                conn.Execute($"PRAGMA user_version = {m.Version};");
                conn.Commit();
            }
            catch
            {
                conn.Rollback();
                throw;
            }
        }

        /// <summary>
        /// 计算迁移 DDL 的 SHA-256 校验和（自洽：DDL 与校验和同源，用于变更审计）。
        /// </summary>
        private static string ComputeChecksum(IMigration m)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(string.Join("\n", m.SqlStatements));
            var hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
