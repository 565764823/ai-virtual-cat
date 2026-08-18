using System;
using Cysharp.Threading.Tasks;
using SQLite;

namespace AiCat.Data.Repositories
{
    /// <summary>
    /// 数据库连接抽象：进程级单连接，线程池执行 + 串行化。
    /// 所有 DB 操作必须经 RunAsync，禁止业务层直接持有 SQLiteConnection。
    /// </summary>
    public interface IDatabase
    {
        /// <summary>数据库文件绝对路径（cat.db）。</summary>
        string FilePath { get; }

        /// <summary>线程池上执行读/写并返回结果（串行化，不阻塞主线程）。</summary>
        UniTask<T> RunAsync<T>(Func<SQLiteConnection, T> work);

        /// <summary>线程池上执行无返回值操作（串行化）。</summary>
        UniTask RunAsync(Action<SQLiteConnection> work);
    }
}
