using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AiCat.AI
{
    /// <summary>
    /// 本地 AI 引擎后端：通过 llama-server 的 OpenAI 兼容接口调用本地 LLM。
    /// 支持流式（SSE）逐 token 输出，实现打字机式回复。这是「本地 AI 引擎接入」的核心。
    /// </summary>
    public sealed class LlmServerHttpBackend : ILlmBackend
    {
        readonly AiConfig _config;
        readonly HttpClient _client;

        public LlmServerHttpBackend(AiConfig config)
        {
            _config = config;
            _client = new HttpClient();
            // 本地 llama-server 推理较慢，给足超时，避免大回复被截断。
            _client.Timeout = TimeSpan.FromMinutes(5);
        }

        public async Task<string> ChatAsync(
            IReadOnlyList<ChatMessage> messages,
            Action<string> onToken,
            CancellationToken ct = default)
        {
            var request = new ChatCompletionRequest
            {
                temperature = _config.temperature,
                top_p = _config.topP,
                repeat_penalty = _config.repeatPenalty,
                max_tokens = _config.maxTokens,
                stream = onToken != null,
            };
            foreach (var m in messages)
                request.messages.Add(new RequestMessage { role = m.Role, content = m.Content });

            string json = JsonUtility.ToJson(request);
            string url = _config.baseUrl.TrimEnd('/') + "/v1/chat/completions";

            using var httpReq = new HttpRequestMessage(HttpMethod.Post, url);
            httpReq.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // ResponseHeadersRead：流式场景可边收边解析，而不是等整个响应缓冲完。
            using var resp = await _client.SendAsync(
                httpReq, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!resp.IsSuccessStatusCode)
            {
                string err = await resp.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"llama-server 返回 {(int)resp.StatusCode}: {err}");
            }

            // 非流式：一次拿完整回复。
            if (onToken == null)
            {
                string body = await resp.Content.ReadAsStringAsync();
                var done = JsonUtility.FromJson<ChatCompletionResponse>(body);
                return FirstContent(done?.choices, static c => c.message?.content);
            }

            // 流式：逐行解析 SSE（data: {...}），累加 delta.content。
            using var stream = await resp.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var sb = new StringBuilder();
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                ct.ThrowIfCancellationRequested();
                if (string.IsNullOrEmpty(line) || !line.StartsWith("data:", StringComparison.Ordinal))
                    continue;

                string payload = line.Substring(5).Trim();
                if (payload == "[DONE]")
                    break;

                ChatChunk chunk;
                try
                {
                    chunk = JsonUtility.FromJson<ChatChunk>(payload);
                }
                catch
                {
                    continue; // 单条解析失败跳过，不影响后续 token。
                }

                string delta = FirstContent(chunk?.choices, static c => c.delta?.content);
                if (string.IsNullOrEmpty(delta))
                    continue;

                sb.Append(delta);
                onToken(delta); // 主线程回调（Unity 的 SynchronizationContext 保证）。
            }

            return sb.ToString().Trim();
        }

        static string FirstContent(ResponseChoice[] choices, Func<ResponseChoice, string> picker)
        {
            if (choices == null || choices.Length == 0)
                return "";
            string content = picker(choices[0]);
            return string.IsNullOrEmpty(content) ? "" : content.Trim();
        }
    }

    // -----------------------------------------------------------------------
    // OpenAI 兼容 JSON DTO（用 JsonUtility 解析，字段名与接口返回值对齐）。
    // 注意：JsonUtility 要求被序列化的类型为 public，故放到顶层并标记 public。
    // -----------------------------------------------------------------------

    [Serializable] public sealed class ChatCompletionRequest
    {
        public string model = "local";
        public List<RequestMessage> messages = new List<RequestMessage>();
        public float temperature = 0.8f;
        public float top_p = 0.9f;
        public float repeat_penalty = 1.15f;
        public int max_tokens = 256;
        public bool stream = false;
    }

    [Serializable] public sealed class RequestMessage
    {
        public string role;
        public string content;
    }

    /// <summary>非流式完整响应。</summary>
    [Serializable] public sealed class ChatCompletionResponse
    {
        public ResponseChoice[] choices;
    }

    /// <summary>流式 SSE 的单个 chunk。</summary>
    [Serializable] public sealed class ChatChunk
    {
        public ResponseChoice[] choices;
    }

    [Serializable] public sealed class ResponseChoice
    {
        public ResponseMessage message;
        public ResponseDelta delta;
    }

    [Serializable] public sealed class ResponseMessage
    {
        public string content;
    }

    [Serializable] public sealed class ResponseDelta
    {
        public string content;
    }
}
