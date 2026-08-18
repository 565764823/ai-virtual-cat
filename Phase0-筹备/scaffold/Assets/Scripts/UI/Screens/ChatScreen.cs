using System;
using System.Text;
using System.Threading;
using AiCat.AI;
using AiCat.Core;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace AiCat.UI.Screens
{
    /// <summary>
    /// 聊天界面：用户输入 → 本地 AI 引擎流式回复 → 展示。
    /// 迁移原 ChatSceneController（依赖改 [Inject] 注入，UI 走 UiBuilder）。
    /// </summary>
    public sealed class ChatScreen : ScreenBase
    {
        public override string ScreenName => "ChatScreen";

        [Inject] ChatService _chat;
        [Inject] SessionState _session;
        [Inject] ScreenManager _screens;

        Text _chatText;
        InputField _input;
        Button _sendButton;
        ScrollRect _scroll;

        // 已完成对话（固定文本）+ 当前流式缓冲（打字机）
        readonly StringBuilder _history = new StringBuilder();
        readonly StringBuilder _streaming = new StringBuilder();
        CancellationTokenSource _cts;

        protected override void Build()
        {
            string catName = _session.HasCat ? _session.CurrentCat.Name : "奶盖";

            // 标题
            var title = Ui.AddText("Title", Root, $"与 {catName} 聊天 · 本地 AI", 30,
                TextAnchor.MiddleLeft, new Color(0.25f, 0.2f, 0.15f, 1f));
            Ui.SetRect(title.gameObject, new Vector2(0, 1), Vector2.one, new Vector2(0.5f, 1),
                new Vector2(24, -56), new Vector2(-180, -12));

            // 返回主界面
            var back = Ui.AddButton("Back", Root, "返回", 22);
            Ui.SetRect(back.gameObject, new Vector2(1, 1), Vector2.one, new Vector2(1f, 1f),
                new Vector2(-110, -56), new Vector2(-24, -12));
            back.onClick.AddListener(() => _screens.Show<MainScreen>());

            // 对话区
            var chatPanel = Ui.AddImage("ChatPanel", Root, new Color(1f, 1f, 1f, 0.55f));
            Ui.SetRect(chatPanel.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(24, 100), new Vector2(-24, -68));
            var (scroll, chatText) = Ui.AddScrollText("ChatScroll", chatPanel.transform, 24,
                new Color(0.2f, 0.18f, 0.15f, 1f));
            _scroll = scroll;
            _chatText = chatText;

            // 底部输入行
            var inputRow = Ui.AddImage("InputRow", Root, new Color(0f, 0f, 0f, 0f));
            Ui.SetRect(inputRow.gameObject, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                new Vector2(24, 24), new Vector2(-24, 88));

            _input = Ui.AddInput("Input", inputRow.transform, 24, $"跟 {catName} 说点什么…");
            Ui.SetRect(_input.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(-140, 0));

            _sendButton = Ui.AddButton("Send", inputRow.transform, "发送", 24);
            Ui.SetRect(_sendButton.gameObject, new Vector2(1, 0), Vector2.one, new Vector2(1f, 0.5f),
                new Vector2(-130, 0), Vector2.zero);
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

            string catName = _session.HasCat ? _session.CurrentCat.Name : "奶盖";

            _history.Append("铲屎官：").Append(userText).Append("\n\n");
            _history.Append(catName).Append("：");
            _streaming.Clear();
            Redraw();

            _cts = new CancellationTokenSource();
            try
            {
                string reply = await _chat.SendAsync(userText, OnToken, _cts.Token);
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
            Canvas.ForceUpdateCanvases();
            if (_scroll != null)
                _scroll.verticalNormalizedPosition = 0f;
        }

        void OnDestroy()
        {
            _cts?.Cancel();
        }
    }
}
