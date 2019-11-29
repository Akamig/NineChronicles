using System.Collections;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Dialog : Widget
    {
        public float textInterval = 0.06f;
        public Color itemTextColor;

        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtDialog;
        public Image imgCharacter;

        private string _playerPrefsKey;
        private string _dialogKey;
        private int _dialogIndex;
        private int _dialogNum;
        private int _characterId;
        private string _npc;
        private Coroutine _coroutine = null;
        private string _text;
        private string _itemTextColor;
        private Dictionary<int, DialogEffect> _effects = new Dictionary<int, DialogEffect>();

        public static string GetPlayerPrefsKeyOfCurrentAvatarState(int dialogId)
        {
            var addr = States.Instance.CurrentAvatarState.Value.address.ToString();
            return $"DIALOG_{addr}_{dialogId}";
        }

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            _itemTextColor = $"#{ColorHelper.ColorToHexRGBA(itemTextColor)}";
        }

        #endregion

        public void Show(int dialogId)
        {
            _playerPrefsKey = GetPlayerPrefsKeyOfCurrentAvatarState(dialogId);
            if (PlayerPrefs.GetInt(_playerPrefsKey, 0) > 0)
                return;

            base.Show();

            _dialogKey = $"DIALOG_{dialogId}_{1}_";
            _dialogIndex = 0;
            _dialogNum = LocalizationManager.LocalizedCount(_dialogKey);

            _coroutine = StartCoroutine(CoShowText());
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
        }

        public void Skip()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
                txtDialog.text = _text;
                return;
            }

            _dialogIndex++;
            if (_dialogIndex == _dialogNum)
            {
                PlayerPrefs.SetInt(_playerPrefsKey, 1);
                Close();
                return;
            }

            _coroutine = StartCoroutine(CoShowText());
        }

        public IEnumerator CoShowText()
        {
            string text = LocalizationManager.Localize($"{_dialogKey}{_dialogIndex}");
            if (string.IsNullOrEmpty(text))
                yield break;

            _characterId = 0;
            _npc = null;
            _effects.Clear();
            _text = ParseText(text);

            if (Game.Game.instance.TableSheets.CharacterSheet.TryGetValue(_characterId, out var characterData))
            {
                var localizedName = LocalizationManager.LocalizeCharacterName(_characterId);
                var res = Resources.Load<Sprite>($"Images/character_{characterData.Id}");
                imgCharacter.overrideSprite = res;
                imgCharacter.SetNativeSize();
                imgCharacter.enabled = imgCharacter.sprite != null;
                txtName.text = localizedName;
            }

            // TODO: npc
            if (!string.IsNullOrEmpty(_npc))
            {
                string localizedName;
                try
                {
                    localizedName = LocalizationManager.Localize($"NPC_{_npc}_NAME");
                }
                catch (KeyNotFoundException)
                {
                    localizedName = "???";
                }

                var res = Resources.Load<Sprite>($"Images/npc/NPC_{_npc}");
                imgCharacter.overrideSprite = res;
                imgCharacter.SetNativeSize();
                imgCharacter.enabled = imgCharacter.overrideSprite != null;
                txtName.text = localizedName;
            }

            bool skipTag = false;
            bool tagClosed = true;
            for (int textIndex = 1; textIndex <= _text.Length; ++textIndex)
            {
                if (_text.Length > textIndex)
                {
                    if (_text[textIndex] == '<')
                    {
                        skipTag = true;
                        tagClosed = false;
                    }
                    else if (skipTag && _text[textIndex] == '>')
                    {
                        skipTag = false;
                        continue;
                    }

                    if (!tagClosed && _text[textIndex] == '/')
                        tagClosed = true;
                }

                if (skipTag)
                    continue;

                if (tagClosed)
                    txtDialog.text = $"{_text.Substring(0, textIndex)}";
                else
                    txtDialog.text = $"{_text.Substring(0, textIndex)}</color>";

                AudioController.instance.PlaySfx(AudioController.SfxCode.Click, 0.1f);

                if (_effects.TryGetValue(textIndex, out var effect))
                {
                    effect.Execute(this);
                }

                yield return new WaitForSeconds(textInterval);
            }

            _coroutine = null;
        }

        private string ParseText(string text)
        {
            var opened = false;
            var openIndex = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var s = text[i];
                switch (s)
                {
                    case '[':
                        if (opened)
                        {
                            continue;
                        }

                        opened = true;
                        openIndex = i;
                        break;
                    case ']':
                        if (!opened)
                        {
                            continue;
                        }

                        opened = false;
                        string left = text.Substring(0, openIndex);
                        string right = text.Substring(i + 1);
                        string[] pair = text.Substring(openIndex + 1, i - openIndex - 1).Split(':');
                        string pairKey = pair[0].ToLower();
                        int.TryParse(pair[1], out int pairValue);
                        switch (pairKey)
                        {
                            case "character":
                                _characterId = pairValue;

                                break;
                            case "npc":
                                _npc = pair[1];

                                break;
                            case "item":
                                if (Game.Game.instance.TableSheets.ItemSheet.TryGetValue(pairValue, out var itemData))
                                {
                                    var localizedItemName = itemData.GetLocalizedName();
                                    left = $"{left}<color={_itemTextColor}>{localizedItemName}</color>";
                                }

                                break;
                            case "shake_vertical":
                                // TODO: 좀더 좋은 방법
                                var values = pair[1].Split('|');
                                _effects.Add(left.Length - 1, new DialogEffectShake()
                                {
                                    value = new Vector3(0.0f, -int.Parse(values[0])),
                                    duration = int.Parse(values[1]),
                                    loops = int.Parse(values[2]),
                                });

                                break;
                        }

                        text = $"{left}{right}";
                        i = left.Length - 1;

                        break;
                }
            }

            return text;
        }
    }
}
