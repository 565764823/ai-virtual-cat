using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiCat.AI
{
    /// <summary>
    /// 对话服务：组装「人设 System Prompt + 历史 + 当前用户消息」，驱动后端生成猫咪回复。
    /// MVP 阶段保留全部短期上下文；上下文窗口裁剪留到 Phase 1。
    /// </summary>
    public sealed class ChatService
    {
        readonly ILlmBackend _backend;
        readonly string _systemPrompt;
        readonly List<ChatMessage> _history = new List<ChatMessage>();

        public ChatService(ILlmBackend backend, string systemPrompt)
        {
            _backend = backend;
            _systemPrompt = systemPrompt;
        }

        /// <summary>
        /// 发送一条用户消息，返回猫咪回复。
        /// </summary>
        /// <param name="userText">用户输入。</param>
        /// <param name="onToken">非 null 则流式逐 token 回调。</param>
        /// <param name="ct">取消令牌。</param>
        public async Task<string> SendAsync(
            string userText,
            Action<string> onToken = null,
            CancellationToken ct = default)
        {
            _history.Add(new ChatMessage("user", userText));

            var messages = new List<ChatMessage>(_history.Count + 1);
            messages.Add(new ChatMessage("system", _systemPrompt));
            messages.AddRange(_history);

            string reply = await _backend.ChatAsync(messages, onToken, ct);

            if (!string.IsNullOrEmpty(reply))
                _history.Add(new ChatMessage("assistant", reply));

            return reply;
        }

        /// <summary>清空对话历史（重新开始）。</summary>
        public void Reset()
        {
            _history.Clear();
        }
    }
}
