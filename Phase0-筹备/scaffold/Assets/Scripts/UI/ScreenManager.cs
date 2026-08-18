using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;

namespace AiCat.UI
{
    /// <summary>
    /// Screen 管理器：全屏 Canvas 生命周期 + 界面切换。
    /// 每次 Show&lt;T&gt;() 动态 AddComponent + Inject，禁止各 Screen 自行 FindObjectOfType。
    /// </summary>
    public sealed class ScreenManager
    {
        private readonly IObjectResolver _resolver;
        private readonly UiBuilder _ui;
        private readonly RectTransform _screenLayer;
        private ScreenBase _current;

        public ScreenManager(IObjectResolver resolver, UiBuilder ui)
        {
            _resolver = resolver;
            _ui = ui;
            _screenLayer = EnsureCanvas();
        }

        /// <summary>切换到指定 Screen（销毁旧界面，构建并显示新界面）。</summary>
        public T Show<T>() where T : ScreenBase
        {
            if (_current != null)
            {
                _current.OnHide();
                Object.Destroy(_current.gameObject);
                _current = null;
            }

            var go = new GameObject(typeof(T).Name, typeof(RectTransform));
            go.transform.SetParent(_screenLayer, false);
            var screen = go.AddComponent<T>();
            _resolver.Inject(screen);
            screen.BuildScreen(_ui);
            screen.OnShow();
            _current = screen;
            return screen;
        }

        /// <summary>建一次全屏 Canvas：EventSystem + Canvas + 背景层 + Screen 层。</summary>
        private RectTransform EnsureCanvas()
        {
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            var canvasGo = new GameObject("AppCanvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            Object.DontDestroyOnLoad(canvasGo);
            var canvasRt = (RectTransform)canvasGo.transform;

            var bg = _ui.AddImage("BackgroundLayer", canvasRt, new Color(0.96f, 0.92f, 0.86f, 1f));
            _ui.SetRect(bg.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var screenGo = new GameObject("ScreenLayer", typeof(RectTransform));
            screenGo.transform.SetParent(canvasRt, false);
            _ui.SetRect(screenGo, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return (RectTransform)screenGo.transform;
        }
    }
}
