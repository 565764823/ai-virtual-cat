using System;
using System.Collections.Generic;
using System.IO;
using AiCat.Data.Models;
using UnityEngine;

namespace AiCat.AI
{
    /// <summary>
    /// 人设 Prompt 加载器：读取 persona_prompt.md，取最后一个代码块作为默认实例（奶盖）。
    /// 领养后按当前猫的品种/性格/名字/口癖动态渲染通用模板（见 <see cref="BuildSystemPrompt"/>）。
    /// </summary>
    public static class PersonaPrompt
    {
        /// <summary>性格 → 口癖描述（领养后按猫的性格渲染人设；后续可按品种/性格细化）。</summary>
        static readonly Dictionary<string, string> QuirkByPersonality = new Dictionary<string, string>
        {
            ["话痨"] = "句尾爱加「喵～」；兴奋时「喵喵喵」连叫不停",
            ["黏人"] = "喜欢蹭人，句尾软软地加「喵～」",
            ["高冷"] = "话少，偶尔傲娇地「哼」一声，句尾加「喵」",
            ["懒散"] = "说话慢悠悠，爱打哈欠，句尾拖长音「喵——」",
            ["活泼"] = "语气欢快，句尾加「喵！」",
        };

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

        /// <summary>从 StreamingAssets 读取「通用模板」代码块（含 {{占位符}}），供领养后动态渲染。</summary>
        public static string LoadTemplate(string fileName = "persona_prompt.md")
        {
            string path = Path.Combine(Application.streamingAssetsPath, fileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[PersonaPrompt] 未找到 {path}，聊天将使用默认人设。");
                return "";
            }

            return ExtractTemplateBlock(File.ReadAllText(path));
        }

        /// <summary>提取含 {{占位符}} 的代码块作为通用模板（默认实例不含占位符，会被跳过）。</summary>
        static string ExtractTemplateBlock(string text)
        {
            string[] parts = text.Split(new[] { "```" }, StringSplitOptions.None);
            for (int i = 1; i < parts.Length; i += 2)
            {
                if (parts[i].Contains("{{"))
                    return parts[i].Trim();
            }

            return "";
        }

        /// <summary>按当前领养猫渲染完整 System Prompt（名字/品种/性格/口癖/自称动态填充）。</summary>
        public static string BuildSystemPrompt(Cat cat, string template)
        {
            if (cat == null || string.IsNullOrWhiteSpace(template))
                return "";

            string quirk;
            if (!QuirkByPersonality.TryGetValue(cat.Personality ?? "", out quirk))
                quirk = "句尾爱加「喵～」";

            var values = new Dictionary<string, string>
            {
                ["名字"] = cat.Name ?? "",
                ["称呼"] = "铲屎官",
                ["品种"] = cat.Breed ?? "",
                ["年龄"] = string.IsNullOrEmpty(cat.GrowthStage) ? "幼猫" : cat.GrowthStage,
                ["性格标签"] = cat.Personality ?? "",
                ["口癖描述"] = quirk,
                ["自称"] = "本喵",
            };

            return Render(template, values);
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
