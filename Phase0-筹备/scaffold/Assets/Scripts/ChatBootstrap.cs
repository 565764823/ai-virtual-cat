using AiCat.AI;
using AiCat.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AiCat
{
    /// <summary>
    /// 程序化生成最小对话界面（避免二进制 .unity 场景文件入库）。
    /// 点 Play 即自动构建「标题 + 对话区 + 输入框 + 发送按钮」并接通本地 AI 引擎。
    /// </summary>
    public static class ChatBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Build()
        {
            Font font = LoadChineseFont();

            var config = new AiConfig();
            var backend = new LlmServerHttpBackend(config);
            string systemPrompt = PersonaPrompt.LoadDefault(config.personaFile);
            var service = new ChatService(backend, systemPrompt);

            BuildUi(service, font);
        }

        // ------------------------------------------------------------------
        // UI 构建
        // ------------------------------------------------------------------

        static void BuildUi(ChatService service, Font font)
        {
            // EventSystem（uGUI 交互必需：输入框聚焦、按钮点击都依赖它）
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            // Canvas
            var canvasGo = new GameObject("ChatCanvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // 背景
            var bg = AddImage("Background", canvasGo.transform, new Color(0.96f, 0.92f, 0.86f, 1f));
            SetRect(bg.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // 标题
            var title = AddText("Title", canvasGo.transform, "奶盖（橘猫）· 本地 AI 对话", font, 30,
                TextAnchor.MiddleLeft, new Color(0.25f, 0.2f, 0.15f, 1f));
            SetRect(title.gameObject, new Vector2(0, 1), Vector2.one, new Vector2(0.5f, 1),
                new Vector2(24, -56), new Vector2(-24, -12));

            // 对话区
            var chatPanel = AddImage("ChatPanel", canvasGo.transform, new Color(1f, 1f, 1f, 0.55f));
            SetRect(chatPanel.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(24, 100), new Vector2(-24, -68));
            var (scroll, chatText) = AddScrollText("ChatScroll", chatPanel.transform, font, 24,
                new Color(0.2f, 0.18f, 0.15f, 1f));

            // 底部输入行
            var inputRow = AddImage("InputRow", canvasGo.transform, new Color(0f, 0f, 0f, 0f));
            SetRect(inputRow.gameObject, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                new Vector2(24, 24), new Vector2(-24, 88));

            var input = AddInput("Input", inputRow.transform, font, 24);
            SetRect(input.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(-140, 0));

            var sendBtn = AddButton("Send", inputRow.transform, "发送", font, 24);
            SetRect(sendBtn.gameObject, new Vector2(1, 0), Vector2.one, new Vector2(1f, 0.5f),
                new Vector2(-130, 0), Vector2.zero);

            // 控制器（注入依赖）
            var controllerGo = new GameObject("ChatSceneController");
            var controller = controllerGo.AddComponent<ChatSceneController>();
            controller.Init(service, chatText, input, sendBtn, scroll);
        }

        // ------------------------------------------------------------------
        // 组件与布局 helper
        // ------------------------------------------------------------------

        static Font LoadChineseFont()
        {
            Font font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "SimHei", "PingFang SC", "Noto Sans CJK SC" }, 24);
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        static void SetRect(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 oMin, Vector2 oMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.offsetMin = oMin;
            rt.offsetMax = oMax;
        }

        static Image AddImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        static Text AddText(string name, Transform parent, string content, Font font, int size,
            TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = size;
            txt.text = content;
            txt.alignment = anchor;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            txt.supportRichText = true;
            return txt;
        }

        static InputField AddInput(string name, Transform parent, Font font, int size)
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
            text.font = font;
            text.fontSize = size;
            text.color = new Color(0.15f, 0.14f, 0.12f, 1f);
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            SetRect(textGo, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(12, 4), new Vector2(-12, -4));

            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(go.transform, false);
            var ph = phGo.AddComponent<Text>();
            ph.font = font;
            ph.fontSize = size;
            ph.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            ph.text = "跟奶盖说点什么…";
            ph.alignment = TextAnchor.MiddleLeft;
            ph.fontStyle = FontStyle.Italic;
            ph.supportRichText = false;
            SetRect(phGo, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(12, 4), new Vector2(-12, -4));

            input.textComponent = text;
            input.placeholder = ph;
            return input;
        }

        static Button AddButton(string name, Transform parent, string label, Font font, int size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 0.6f, 0.2f, 1f);
            var btn = go.AddComponent<Button>();

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.text = label;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.supportRichText = false;
            SetRect(textGo, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            return btn;
        }

        static (ScrollRect, Text) AddScrollText(string name, Transform parent, Font font, int size, Color color)
        {
            var scrollGo = new GameObject(name, typeof(RectTransform));
            scrollGo.transform.SetParent(parent, false);
            SetRect(scrollGo, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(10, 10), new Vector2(-10, -10));
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Viewport（矩形遮罩，不依赖 alpha）
            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            viewportGo.AddComponent<RectMask2D>();
            SetRect(viewportGo, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Content（Text + ContentSizeFitter 撑高）
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentText = contentGo.AddComponent<Text>();
            contentText.font = font;
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
