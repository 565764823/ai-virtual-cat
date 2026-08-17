using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AiCat.AI
{
    /// <summary>
    /// 人设 Prompt 加载器：读取 persona_prompt.md，取最后一个代码块作为默认实例（奶盖）。
    /// 未来「选猫领养」会按品种/性格/名字/口癖动态渲染通用模板（见 <see cref="Render"/>）。
    /// </summary>
    public static class PersonaPrompt
    {
        /// <summary>从 StreamingAssets 读取人设模板，返回默认实例 System Prompt。</summary>
        public static string LoadDefault(string fileName = "persona_prompt.md")
        {
            string path = Path.Combine(Application.streamingAssetsPath, fileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[PersonaPrompt] 未找到 {path}，将使用空提示词（猫咪可能不像猫）。");
                return "";
            }

            string text = File.ReadAllText(path);
            return ExtractLastCodeBlock(text);
        }

        /// <summary>提取所有 ``` 代码块，返回最后一个非空块（与 POC 的 load_system_prompt 逻辑一致）。</summary>
        static string ExtractLastCodeBlock(string text)
        {
            string[] parts = text.Split(new[] { "```" }, StringSplitOptions.None);
            string last = "";
            for (int i = 1; i < parts.Length; i += 2)
            {
                if (!string.IsNullOrWhiteSpace(parts[i]))
                    last = parts[i].Trim();
            }

            return last;
        }

        /// <summary>渲染通用模板：把 {{占位符}} 替换为实际值（供选猫参数填充）。</summary>
        public static string Render(string template, IReadOnlyDictionary<string, string> values)
        {
            string result = template;
            foreach (KeyValuePair<string, string> kv in values)
                result = result.Replace("{{" + kv.Key + "}}", kv.Value);
            return result;
        }
    }
}
