using UnityEngine;
using UnityEngine.UI;

namespace AiCat.UI
{
    /// <summary>
    /// 程序化 uGUI 工厂：从原 ChatBootstrap 抽出的构建 helper，纯 UI 工厂、无业务逻辑。
    /// 持有进程级中文字体。由 RootLifetimeScope 注册为单例。
    /// </summary>
    public sealed class UiBuilder
    {
        /// <summary>进程级中文字体（Microsoft YaHei 等，兜底 LegacyRuntime.ttf）。</summary>
        public Font Font { get; }

        public UiBuilder()
        {
            Font = LoadChineseFont();
        }

        public static Font LoadChineseFont()
        {
            Font font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "SimHei", "PingFang SC", "Noto Sans CJK SC" }, 24);
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        /// <summary>设置 RectTransform 的锚点/轴心/偏移（全屏布局常用）。</summary>
        public void SetRect(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 oMin, Vector2 oMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.offsetMin = oMin;
            rt.offsetMax = oMax;
        }

        public Image AddImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public Text AddText(string name, Transform parent, string content, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = Font;
            txt.fontSize = size;
            txt.text = content;
            txt.alignment = anchor;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            txt.supportRichText = true;
            return txt;
        }

        public InputField AddInput(string name, Transform parent, int size, string placeholder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.95f);
            var input = go.AddComponent<InputField>();
            input.lineType = InputField.LineType.SingleLine;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.color = new Color(0.15f, 0.14f, 0.12f, 1f);
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            SetRect(textGo, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(12, 4), new Vector2(-12, -4));

            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(go.transform, false);
            var ph = phGo.AddComponent<Text>();
            ph.font = Font;
            ph.fontSize = size;
            ph.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            ph.text = placeholder;
            ph.alignment = TextAnchor.MiddleLeft;
            ph.fontStyle = FontStyle.Italic;
            ph.supportRichText = false;
            SetRect(phGo, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(12, 4), new Vector2(-12, -4));

            input.textComponent = text;
            input.placeholder = ph;
            return input;
        }

        public Button AddButton(string name, Transform parent, string label, int size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 0.6f, 0.2f, 1f);
            var btn = go.AddComponent<Button>();

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.text = label;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.supportRichText = false;
            SetRect(textGo, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            return btn;
        }

        public (ScrollRect Scroll, Text Text) AddScrollText(string name, Transform parent, int size, Color color)
        {
            var scrollGo = new GameObject(name, typeof(RectTransform));
            scrollGo.transform.SetParent(parent, false);
            SetRect(scrollGo, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(10, 10), new Vector2(-10, -10));
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            viewportGo.AddComponent<RectMask2D>();
            SetRect(viewportGo, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentText = contentGo.AddComponent<Text>();
            contentText.font = Font;
            contentText.fontSize = size;
            contentText.color = color;
            contentText.alignment = TextAnchor.UpperLeft;
            contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
            contentText.verticalOverflow = VerticalWrapMode.Overflow;
            contentText.supportRichText = true;
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            SetRect(contentGo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);

            scrollRect.viewport = viewportGo.GetComponent<RectTransform>();
            scrollRect.content = contentGo.GetComponent<RectTransform>();
            return (scrollRect, contentText);
        }
    }
}
