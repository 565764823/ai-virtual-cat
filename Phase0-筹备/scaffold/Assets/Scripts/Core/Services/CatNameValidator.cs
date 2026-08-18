using System.Globalization;

namespace AiCat.Core.Services
{
    /// <summary>
    /// 猫咪命名校验（纯函数，无 IO）。重名校验由 AdoptionService 查库完成。
    /// </summary>
    public static class CatNameValidator
    {
        /// <summary>名字最大字素簇数（正确处理中文/emoji）。</summary>
        public const int MaxLength = 12;

        /// <summary>
        /// 校验名字。返回 (是否合法, 中文错误文案)。
        /// 规则：① Trim 后非空；② 长度 1~12 字素簇；③ 不含控制字符。
        /// </summary>
        public static (bool IsValid, string Error) Validate(string raw)
        {
            string name = raw?.Trim();

            if (string.IsNullOrEmpty(name))
                return (false, "名字不能为空");

            if (new StringInfo(name).LengthInTextElements > MaxLength)
                return (false, $"名字不能超过 {MaxLength} 个字");

            foreach (char c in name)
            {
                if (char.IsControl(c))
                    return (false, "名字不能包含特殊控制字符");
            }

            return (true, null);
        }

        /// <summary>规范化名字（Trim，去首尾空白）。</summary>
        public static string Trim(string raw) => raw?.Trim();
    }
}
