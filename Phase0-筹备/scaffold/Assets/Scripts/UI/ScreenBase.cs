using UnityEngine;

namespace AiCat.UI
{
    /// <summary>
    /// Screen 抽象基类：全屏界面。由 ScreenManager AddComponent + Inject 后调用 BuildScreen 构建。
    /// 子类依赖经 [Inject] 字段注入（VContainer），在 Build() 里组装 UI。
    /// </summary>
    public abstract class ScreenBase : MonoBehaviour
    {
        /// <summary>界面名（同时作为根节点名）。</summary>
        public abstract string ScreenName { get; }

        /// <summary>UI 工厂（中文排版/字体）。</summary>
        protected UiBuilder Ui { get; private set; }

        /// <summary>本界面的全屏根 RectTransform（充满 Screen 层）。</summary>
        protected RectTransform Root { get; private set; }

        public void BuildScreen(UiBuilder ui)
        {
            Ui = ui;
            Root = (RectTransform)transform;
            Root.anchorMin = Vector2.zero;
            Root.anchorMax = Vector2.one;
            Root.offsetMin = Vector2.zero;
            Root.offsetMax = Vector2.zero;
            Build();
        }

        /// <summary>组装本界面的 UI（依赖已注入完毕）。</summary>
        protected abstract void Build();

        /// <summary>界面显示时回调（已切到前台）。</summary>
        public virtual void OnShow() { }

        /// <summary>界面隐藏/销毁前回调。</summary>
        public virtual void OnHide() { }
    }
}
