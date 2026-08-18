using System;
using AiCat.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace AiCat.UI.Widgets
{
    /// <summary>
    /// 选猫卡片：品种·性格标题 + 描述，可点选高亮。无业务逻辑，仅持有选中态并转发点击。
    /// </summary>
    public sealed class CatCardWidget
    {
        private static readonly Color NormalColor = new Color(0.98f, 0.97f, 0.94f, 1f);
        private static readonly Color SelectedColor = new Color(1f, 0.85f, 0.5f, 1f);

        public CatDefinition Definition { get; }
        public bool Selected { get; private set; }

        private readonly Image _background;

        /// <summary>点击回调（参数为本卡片，便于外层做互斥选中）。</summary>
        public event Action<CatCardWidget> Clicked;

        public CatCardWidget(UiBuilder ui, Transform parent, CatDefinition definition)
        {
            Definition = definition;

            var go = new GameObject("Card_" + definition.Breed, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = 250;
            layout.preferredHeight = 200;

            _background = go.AddComponent<Image>();
            _background.color = NormalColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = _background;
            btn.onClick.AddListener(() => Clicked?.Invoke(this));

            var title = ui.AddText("Title", go.transform, $"{definition.Breed} · {definition.Personality}",
                26, TextAnchor.MiddleCenter, new Color(0.25f, 0.2f, 0.15f, 1f));
            ui.SetRect(title.gameObject, new Vector2(0, 1), Vector2.one, new Vector2(0.5f, 1),
                new Vector2(12, -60), new Vector2(-12, -12));

            var desc = ui.AddText("Desc", go.transform, definition.Description,
                16, TextAnchor.UpperLeft, new Color(0.35f, 0.3f, 0.25f, 1f));
            desc.verticalOverflow = VerticalWrapMode.Overflow;
            // 锚点拉伸到整卡，但用 offset 限制在 title（顶 12~60px）下方：顶距 68px、底距 12px，避免重叠。
            ui.SetRect(desc.gameObject, Vector2.zero, new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(16, 12), new Vector2(-16, -68));
        }

        public void SetSelected(bool selected)
        {
            Selected = selected;
            _background.color = selected ? SelectedColor : NormalColor;
        }
    }
}
