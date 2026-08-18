using System;

namespace AiCat.Data.Serialization
{
    /// <summary>
    /// 统一时间戳工具：生成与 schema.sql 一致的「YYYY-MM-DD HH:MM:SS」本地时间文本。
    /// 数据库时间字段一律用 string，避免 sqlite-net 把 DateTime 存成 ticks 破坏 TEXT 约定。
    /// </summary>
    public static class TimeUtil
    {
        /// <summary>当前本地时间戳（YYYY-MM-DD HH:MM:SS）。</summary>
        public static string NowString() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>当前日期（YYYY-MM-DD），用于 record_date / date 等日期字段。</summary>
        public static string TodayString() => DateTime.Now.ToString("yyyy-MM-dd");
    }
}
