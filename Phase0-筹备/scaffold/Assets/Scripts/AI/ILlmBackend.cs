using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiCat.AI
{
    /// <summary>一条聊天消息（OpenAI 兼容协议）。</summary>
    public readonly struct ChatMessage
    {
        /// <summary>角色：system / user / assistant。</summary>
        public string Role { get; }

        /// <summary>消息文本。</summary>
        public string Content { get; }

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    /// <summary>
    /// 本地 LLM 后端抽象：对话补全，支持流式逐 token 回调。
    /// 当前实现为 <see cref="LlmServerHttpBackend"/>（调 llama-server 的 OpenAI 兼容接口）。
    /// 未来 LLMUnity（进程内 llama.cpp）也实现本接口，实现后端平滑替换（协议同为 OpenAI 兼容）。
    /// </summary>
    public interface ILlmBackend
    {
        /// <summary>
        /// 发起一次对话补全。
        /// </summary>
        /// <param name="messages">完整消息列表（含 system 人设 + 历史 + 当前用户消息）。</param>
        /// <param name="onToken">若非 null，则逐 token 回调（流式，打字机式回复）；在主线程触发。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>完整回复文本（流式下为累加结果）。</returns>
        Task<string> ChatAsync(
            IReadOnlyList<ChatMessage> messages,
            Action<string> onToken,
            CancellationToken ct = default);
    }
}
