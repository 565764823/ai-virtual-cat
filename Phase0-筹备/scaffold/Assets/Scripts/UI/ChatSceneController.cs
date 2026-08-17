using System;
using System.Text;
using System.Threading;
using AiCat.AI;
using UnityEngine;
using UnityEngine.UI;

namespace AiCat.UI
{
    /// <summary>
    /// 最小对话场景控制器：用户输入 → 本地 AI 引擎流式回复 → 展示。
    /// 由 <see cref="AiCat.ChatBootstrap"/> 创建 UI 后注入依赖。
    /// </summary>
    public sealed class ChatSceneController : MonoBehaviour
    {
        Text _chatText;
        InputField _input;
        Button _sendButton;
        ScrollRect _scroll;
        ChatService _service;

        // 已完成对话（固定文本）+ 当前流式缓冲（打字机）
        readonly StringBuilder _history = new StringBuilder();
        readonly StringBuilder _streaming = new StringBuilder();
        CancellationTokenSource _cts;

        public void Init(
            ChatService service,
            Text chatText,
            InputField input,
            Button sendButton,
            ScrollRect scroll)
        {
            _service = service;
            _chatText = chatText;
            _input = input;
            _sendButton = sendButton;
            _scroll = scroll;
            _sendButton.onClick.AddListener(OnSend);
        }

        async void OnSend()
        {
            if (_cts != null)
                return; // 上一轮生成中，忽略本次点击

            string userText = _input.text;
            if (string.IsNullOrWhiteSpace(userText))
                return;

            _input.text = "";
            _sendButton.interactable = false;

            _history.Append("铲屎官：").Append(userText).Append("\n\n");
            _history.Append("奶盖：");
            _streaming.Clear();
            Redraw();

            _cts = new CancellationTokenSource();
            try
            {
                string reply = await _service.SendAsync(userText, OnToken, _cts.Token);
                _history.Append(reply).Append("\n\n");
            }
            catch (OperationCanceledException)
            {
                _history.Append("（已取消）\n\n");
            }
            catch (Exception e)
            {
                _history.Append("（连接本地 AI 引擎失败：").Append(e.Message).Append("）\n\n");
            }
            finally
            {
                _streaming.Clear();
                _sendButton.interactable = true;
                _cts = null;
                Redraw();
            }
        }

        void OnToken(string token)
        {
            _streaming.Append(token);
            Redraw();
        }

        void Redraw()
        {
            _chatText.text = _history.ToString() + _streaming.ToString();
            // 先强制更新画布布局，再滚到底部，确保滚动位置正确。
            Canvas.ForceUpdateCanvases();
            if (_scroll != null)
                _scroll.verticalNormalizedPosition = 0f;
        }
    }
}
