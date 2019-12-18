using EnhancedUI.EnhancedScroller;
using Nekoyume.Helper;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.SimpleLocalization;

namespace Nekoyume.UI.Scroller
{
    public class MailCellView : EnhancedScrollerCellView
    {
        public Action<MailCellView> onClickSubmitButton;

        private static readonly Vector2 _leftBottom = new Vector2(-14f, -10.5f);
        private static readonly Vector2 _minusRightTop = new Vector2(14f, 13f);
        private static readonly Color _highlightedColor = ColorHelper.HexToColorRGB("a35400");
        public Game.Mail.Mail data;
        public Image icon;
        public Image buttonImage;
        public TextMeshProUGUI content;
        public Button button;
        public TextMeshProUGUI submitText;
        public TextMeshProUGUI submitTextSelected;

        private Mail _mail;

        #region Mono

        private void Awake()
        {
            button.onClick.AddListener(OnClickButton);
        }

        private void OnDisable()
        {
            button.interactable = true;
        }

        #endregion

        public void SetData(Game.Mail.Mail mail)
        {   
            _mail = Widget.Find<Mail>();
            data = mail;
            var text = mail.ToInfo();
            var isNew = mail.New;
            var color = isNew ? ColorHelper.HexToColorRGB("fff9dd") : ColorHelper.HexToColorRGB("7a7a7a");
            button.interactable = isNew;
            submitText.text = isNew
                ? LocalizationManager.Localize("UI_RECEIVE")
                : LocalizationManager.Localize("UI_RECEIVED");
            submitTextSelected.text = submitText.text;
            buttonImage.rectTransform.offsetMin = isNew ? _leftBottom : Vector2.zero;
            buttonImage.rectTransform.offsetMax = isNew ? _minusRightTop : Vector2.zero;
            icon.overrideSprite = Mail.mailIcons[mail.MailType];
            content.text = text;
            content.color = color;
            submitText.gameObject.SetActive(!isNew);
            submitTextSelected.gameObject.SetActive(isNew);
        }

        private void Read()
        {
            submitText.text = LocalizationManager.Localize("UI_RECEIVED");
            buttonImage.rectTransform.offsetMin = Vector2.zero;
            buttonImage.rectTransform.offsetMax = Vector2.zero;
            if (!data.New)
                return;

            data.New = false;
            button.interactable = false;
            submitText.gameObject.SetActive(true);
            submitTextSelected.gameObject.SetActive(false);
            content.color = ColorHelper.HexToColorRGB("7a7a7a");
            data.Read(_mail);
        }

        private void OnClickButton()
        {
            Read();
            onClickSubmitButton?.Invoke(this);
        }
    }
}
