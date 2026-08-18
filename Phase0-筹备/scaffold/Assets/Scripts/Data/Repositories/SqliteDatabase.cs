using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using SQLite;

namespace AiCat.Data.Repositories
{
    /// <summary>
    /// SQLite 进程级单连接实现。
    /// 关键点：storeDateTimeAsTicks=false（时间戳保持 TEXT）、FullMutex 串行、
    /// foreign_keys 每连接重开（非持久）。整个游戏只开一次连接。
    /// </summary>
    public sealed class SqliteDatabase : IDatabase, IDisposable
    {
        private readonly SQLiteConnection _conn;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

        public string FilePath { get; }

        /// <summary>默认库：Unity 持久化目录下的 cat.db。</summary>
        public SqliteDatabase()
            : this(Path.Combine(UnityEngine.Application.persistentDataPath, "cat.db"))
        {
        }

        public SqliteDatabase(string filePath)
        {
            // 初始化原生 sqlite3 提供程序（bundle_green，含 Windows/Linux/macOS）。
            // SQLitePCLRaw 2.x 已移除 Batteries_V2 包，改为显式 SetProvider。
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
            SQLitePCL.raw.FreezeProvider();

            FilePath = filePath;
            _conn = new SQLiteConnection(
                filePath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex,
                storeDateTimeAsTicks: false);

            _conn.Execute("PRAGMA foreign_keys = ON;");
        }

        public async UniTask<T> RunAsync<T>(Func<SQLiteConnection, T> work)
        {
            await _gate.WaitAsync();
            try
            {
                return await UniTask.RunOnThreadPool(() => work(_conn));
            }
            finally
            {
                _gate.Release();
            }
        }

        public async UniTask RunAsync(Action<SQLiteConnection> work)
        {
            await _gate.WaitAsync();
            try
            {
                await UniTask.RunOnThreadPool(() => work(_conn));
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Dispose()
        {
            _conn?.Dispose();
            _gate?.Dispose();
        }
    }
}
