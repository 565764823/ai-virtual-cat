using System;
using System.Collections.Generic;
using AiCat.Core;
using AiCat.Core.Services;
using AiCat.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace AiCat.UI.Screens
{
    /// <summary>
    /// 选猫领养界面：读目录生成卡片 → 命名 → 领养落库 → 切主界面。
    /// </summary>
    public sealed class CatSelectScreen : ScreenBase
    {
        public override string ScreenName => "CatSelectScreen";

        [Inject] ICatCatalog _catalog;
        [Inject] IAdoptionService _adoption;
        [Inject] SessionState _session;
        [Inject] ScreenManager _screens;

        readonly List<CatCardWidget> _cards = new List<CatCardWidget>();
        CatDefinition _selected;
        InputField _nameInput;
        Button _adoptButton;
        Text _errorText;

        protected override void Build()
        {
            // 标题
            var title = Ui.AddText("Title", Root, "领养一只猫咪", 34, TextAnchor.MiddleCenter,
                new Color(0.25f, 0.2f, 0.15f, 1f));
            Ui.SetRect(title.gameObject, new Vector2(0, 1), Vector2.one, new Vector2(0.5f, 1),
                new Vector2(24, -80), new Vector2(-24, -20));

            // 卡片区（水平排列，互斥选中）
            var cardsContainer = new GameObject("Cards", typeof(RectTransform));
            cardsContainer.transform.SetParent(Root, false);
            Ui.SetRect(cardsContainer, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(20, -40), new Vector2(-20, 160));
            var layout = cardsContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            foreach (var def in _catalog.GetDefinitions())
            {
                var card = new CatCardWidget(Ui, cardsContainer.transform, def);
                card.Clicked += OnCardClicked;
                _cards.Add(card);
            }

            // 命名
            var nameLabel = Ui.AddText("NameLabel", Root, "给 TA 取个名字", 20, TextAnchor.MiddleLeft,
                new Color(0.45f, 0.4f, 0.35f, 1f));
            Ui.SetRect(nameLabel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-180, -70), new Vector2(180, -35));

            _nameInput = Ui.AddInput("NameInput", Root, 24, "例如：奶盖");
            Ui.SetRect(_nameInput.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-180, -125), new Vector2(180, -75));

            _adoptButton = Ui.AddButton("Adopt", Root, "领养 TA", 26);
            Ui.SetRect(_adoptButton.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-110, -185), new Vector2(110, -135));
            _adoptButton.onClick.AddListener(OnAdopt);

            _errorText = Ui.AddText("Error", Root, "", 18, TextAnchor.MiddleCenter,
                new Color(0.8f, 0.2f, 0.15f, 1f));
            Ui.SetRect(_errorText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-300, -235), new Vector2(300, -195));
        }

        void OnCardClicked(CatCardWidget card)
        {
            _selected = card.Definition;
            foreach (var c in _cards)
                c.SetSelected(c == card);
            _errorText.text = "";
        }

        async void OnAdopt()
        {
            if (_selected == null)
            {
                _errorText.text = "请先选择一只猫咪";
                return;
            }

            _adoptButton.interactable = false;
            try
            {
                var cat = await _adoption.AdoptAsync(_session.CurrentUser.Id, _selected, _nameInput.text);
                _session.Set(_session.CurrentUser, cat);
                _screens.Show<MainScreen>();
            }
            catch (ArgumentException e)
            {
                _errorText.text = e.Message;
                _adoptButton.interactable = true;
            }
        }
    }
}
