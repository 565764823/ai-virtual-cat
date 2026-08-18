using AiCat.Core;
using UnityEngine;
using VContainer;

namespace AiCat.UI.Screens
{
    /// <summary>
    /// 主界面：显示已领养猫的名字/品种/性格与三态，入口聊天。提醒/心情为后续任务占位。
    /// </summary>
    public sealed class MainScreen : ScreenBase
    {
        public override string ScreenName => "MainScreen";

        [Inject] SessionState _session;
        [Inject] ScreenManager _screens;

        protected override void Build()
        {
            var cat = _session.CurrentCat;

            // 标题：名字（品种 · 性格）
            var title = Ui.AddText("Title", Root, $"{cat.Name}（{cat.Breed} · {cat.Personality}）", 32,
                TextAnchor.MiddleCenter, new Color(0.25f, 0.2f, 0.15f, 1f));
            Ui.SetRect(title.gameObject, new Vector2(0, 1), Vector2.one, new Vector2(0.5f, 1),
                new Vector2(24, -80), new Vector2(-24, -20));

            // 三态副标题
            var status = Ui.AddText("Status", Root, $"心情 {cat.Mood} · 精力 {cat.Energy} · 健康 {cat.Health}", 20,
                TextAnchor.MiddleCenter, new Color(0.45f, 0.4f, 0.35f, 1f));
            Ui.SetRect(status.gameObject, new Vector2(0, 1), Vector2.one, new Vector2(0.5f, 1),
                new Vector2(24, -125), new Vector2(-24, -85));

            // 聊天入口
            var chat = Ui.AddButton("ChatBtn", Root, "聊聊天", 30);
            Ui.SetRect(chat.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-120, 40), new Vector2(120, 110));
            chat.onClick.AddListener(() => _screens.Show<ChatScreen>());

            // 占位提示
            var hint = Ui.AddText("Hint", Root, "提醒 · 心情记录（后续开放）", 20,
                TextAnchor.MiddleCenter, new Color(0.5f, 0.5f, 0.5f, 1f));
            Ui.SetRect(hint.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-160, -30), new Vector2(160, 20));
        }
    }
}
