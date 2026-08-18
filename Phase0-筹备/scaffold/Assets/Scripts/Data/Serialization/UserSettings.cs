using System;

namespace AiCat.Data.Serialization
{
    /// <summary>
    /// 用户偏好 DTO：序列化进 users.settings_json（预留）。
    /// Phase 1 仅建模；通知开关 / 模型选择等具体字段后续阶段填充。
    /// </summary>
    [Serializable]
    public sealed class UserSettings
    {
        /// <summary>桌面通知开关（预留）。</summary>
        public bool notificationsEnabled = true;

        /// <summary>模型选择（预留，轻量/大模型可选下载）。</summary>
        public string preferredModel = "";
    }
}
