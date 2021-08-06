﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Nekoyume.UI
{
    using UniRx;

    public class CombinationResult : Widget
    {
        [Serializable]
        public struct ResultItem
        {
            public TextMeshProUGUI itemNameText;
            public SimpleItemView itemView;
            public TextMeshProUGUI mainStatText;
            public TextMeshProUGUI cpText;
        }

#if UNITY_EDITOR
        [Serializable]
        public enum EquipmentOrFood
        {
            Equipment,
            Food
        }

        [Serializable]
        public class EditorStatOption
        {
            public StatType statType;
            public int value;
        }

        [Serializable]
        public class EditorSkillOption
        {
            public int chance;
            public int power;
        }
#endif

        [SerializeField]
        private Image _iconImage;

        [SerializeField]
        private GameObject _titleSuccessObject;

        [SerializeField]
        private GameObject _titleGreatSuccessObject;

        [SerializeField]
        private GameObject _titleFoodSuccessObject;

        [SerializeField]
        private Sprite _equipmentIconSprite;

        [SerializeField]
        private Sprite _consumableIconSprite;

        [SerializeField]
        private ResultItem _resultItem;

        [SerializeField]
        private List<ItemOptionView> _itemStatOptionViews;

        [SerializeField]
        private List<ItemOptionView> _itemSkillOptionViews;

        [SerializeField]
        private List<ItemOptionIconView> _itemOptionIconViews;

        [SerializeField]
        private float _delayTimeOfShowOptions;

        [SerializeField]
        private float _intervalTimeOfShowOptions;

        [SerializeField]
        private float _delayTimeOfIncreaseCPAnimation;

        [SerializeField]
        private float _dueTimeOfIncreaseCPAnimation;

#if UNITY_EDITOR
        [Space(10)]
        [Header("Editor Properties For Test")]
        [Space(10)]
        [SerializeField]
        private bool _editorForEquipment;

        [SerializeField]
        private List<EditorStatOption> _editorStatOptions;

        [SerializeField]
        private List<EditorSkillOption> _editorSkillOptions;
#endif

        private static readonly int AnimatorHashGreatSuccess = Animator.StringToHash("GreatSuccess");
        private static readonly int AnimatorHashSuccess = Animator.StringToHash("Success");
        private static readonly int AnimatorHashLoop = Animator.StringToHash("Loop");
        private static readonly int AnimatorHashClose = Animator.StringToHash("Close");

        private ItemOptionInfo _itemOptionInfo;
        private readonly List<decimal> _cpListForAnimationSteps = new List<decimal>();
        private IDisposable _disposableOfSkip;
        private IDisposable _disposableOfCPAnimation;
        private Coroutine _coroutineOfPlayOptionAnimation;

        public override WidgetType WidgetType => WidgetType.Popup;

        protected override void OnDisable()
        {
            _disposableOfSkip?.Dispose();
            _disposableOfSkip = null;

            _disposableOfCPAnimation?.Dispose();
            _disposableOfCPAnimation = null;

            base.OnDisable();
        }

#if UNITY_EDITOR
        public void ShowWithEditorProperty()
        {
            ItemUsable itemUsable;
            var tableSheets = Game.Game.instance.TableSheets;
            if (_editorForEquipment)
            {
                var equipmentList = !_editorStatOptions.Any() || _editorStatOptions[0].statType == StatType.NONE
                    ? tableSheets.EquipmentItemSheet.OrderedList
                    : tableSheets.EquipmentItemSheet.OrderedList.Where(e =>
                        e.Stat.Type == _editorStatOptions[0].statType).ToList();
                if (!equipmentList.Any())
                {
                    Debug.LogError($"{_editorStatOptions[0].statType} cannot be main stat type");
                    return;
                }

                var equipmentRow = equipmentList[Random.Range(0, equipmentList.Count)];
                var equipment = (Equipment)ItemFactory.CreateItemUsable(equipmentRow, Guid.NewGuid(), 0);
                foreach (var statOption in _editorStatOptions)
                {
                    equipment.StatsMap.AddStatAdditionalValue(statOption.statType, statOption.value);
                    equipment.optionCountFromCombination++;
                }

                var skillList = tableSheets.SkillSheet.OrderedList;
                foreach (var skillOption in _editorSkillOptions)
                {
                    var row = skillList[Random.Range(0, skillList.Count)];
                    var skill = SkillFactory.Get(row, skillOption.power, skillOption.chance);
                    equipment.Skills.Add(skill);
                    equipment.optionCountFromCombination++;
                }

                itemUsable = equipment;
            }
            else
            {
                var consumableList = !_editorStatOptions.Any() || _editorStatOptions[0].statType == StatType.NONE
                    ? tableSheets.ConsumableItemSheet.OrderedList
                    : tableSheets.ConsumableItemSheet.OrderedList.Where(e =>
                        e.Stats[0].StatType == _editorStatOptions[0].statType).ToList();
                if (!consumableList.Any())
                {
                    Debug.LogError($"{_editorStatOptions[0].statType} cannot be main stat type");
                    return;
                }

                var consumableRow = consumableList[Random.Range(0, consumableList.Count)];
                var consumable = (Consumable)ItemFactory.CreateItemUsable(consumableRow, Guid.NewGuid(), 0);
                foreach (var statOption in _editorStatOptions)
                {
                    consumable.StatsMap.AddStatValue(statOption.statType, statOption.value);
                }

                itemUsable = consumable;
            }

            Show(itemUsable);
        }
#endif

        [Obsolete("Use `Show(ItemUsable itemUsable)` instead.")]
        public override void Show(bool ignoreShowAnimation = false)
        {
            // ignore.
        }

        public void Show(ItemUsable itemUsable)
        {
            if (itemUsable is null)
            {
                Debug.LogError($"{nameof(itemUsable)} is null");
                return;
            }

            for (var i = 0; i < _itemOptionIconViews.Count; i++)
            {
                _itemOptionIconViews[i].Hide(true);
            }

            _cpListForAnimationSteps.Clear();
            _resultItem.itemNameText.text = itemUsable.GetLocalizedName(useElementalIcon: false);
            _resultItem.itemView.SetData(
                Game.Game.instance.TableSheets.ItemSheet.OrderedList.First(e => e.Id == itemUsable.Id));
            _resultItem.mainStatText.text = string.Empty;
            _resultItem.cpText.text = string.Empty;

            _itemOptionInfo = itemUsable is Equipment equipment
                ? new ItemOptionInfo(equipment)
                : new ItemOptionInfo(itemUsable);
            var statOptions = _itemOptionInfo.StatOptions;
            var statOptionsCount = statOptions.Count;
            for (var i = 0; i < _itemStatOptionViews.Count; i++)
            {
                var optionView = _itemStatOptionViews[i];
                optionView.Hide(true);
                if (i >= statOptionsCount)
                {
                    optionView.UpdateAsEmpty();
                    continue;
                }

                _itemOptionIconViews[i].UpdateAsStat();
                optionView.UpdateAsStatTuple(statOptions[i]);
            }

            var skillOptions = _itemOptionInfo.SkillOptions;
            var skillOptionsCount = skillOptions.Count;
            for (var i = 0; i < _itemSkillOptionViews.Count; i++)
            {
                var optionView = _itemSkillOptionViews[i];
                optionView.Hide(true);
                if (i >= skillOptionsCount)
                {
                    optionView.UpdateAsEmpty();
                    continue;
                }

                _itemOptionIconViews[i + statOptionsCount].UpdateAsSkill();
                optionView.UpdateAsSkillTuple(skillOptions[i]);
            }

            if (itemUsable.ItemType == ItemType.Equipment)
            {
                PostShowAsEquipment(itemUsable);
            }
            else
            {
                PostShowAsConsumable();
            }
        }

        private void PostShowAsConsumable()
        {
            _iconImage.overrideSprite = _consumableIconSprite;
            _titleSuccessObject.SetActive(false);
            _titleGreatSuccessObject.SetActive(false);
            _titleFoodSuccessObject.SetActive(true);

            // NOTE: Ignore Show Animation
            base.Show(true);
            Animator.SetTrigger(AnimatorHashSuccess);
        }

        private void PostShowAsEquipment(ItemUsable itemUsable)
        {
            _iconImage.overrideSprite = _equipmentIconSprite;
            _titleSuccessObject.SetActive(_itemOptionInfo.OptionCountFromCombination != 4);
            _titleGreatSuccessObject.SetActive(_itemOptionInfo.OptionCountFromCombination == 4);
            _titleFoodSuccessObject.SetActive(false);

            var (mainStatType, mainStatValue) = _itemOptionInfo.MainStat;
            _resultItem.mainStatText.text = $"{mainStatType.ToString()} {mainStatValue}";

            var statsCP = CPHelper.GetStatCP(mainStatType, mainStatValue);
            _cpListForAnimationSteps.Add(statsCP);
            _resultItem.cpText.text = CPHelper.DecimalToInt(statsCP).ToString();

            var statOptions = _itemOptionInfo.StatOptions;
            foreach (var (type, value, _) in statOptions)
            {
                statsCP += CPHelper.GetStatCP(type, value);
                _cpListForAnimationSteps.Add(statsCP);
            }

            var skillOptions = _itemOptionInfo.SkillOptions;
            for (var i = 0; i < skillOptions.Count; i++)
            {
                var multipliedCP = statsCP * CPHelper.GetSkillsMultiplier(i + 1);
                _cpListForAnimationSteps.Add(multipliedCP);
            }

            if (_itemOptionInfo.CP !=
                CPHelper.DecimalToInt(_cpListForAnimationSteps[_cpListForAnimationSteps.Count - 1]))
            {
                Debug.LogError(
                    $"Wrong CP!!!! {_itemOptionInfo.CP} != {_cpListForAnimationSteps[_cpListForAnimationSteps.Count - 1]}");
            }

            // NOTE: Ignore Show Animation
            base.Show(true);
            Animator.SetTrigger(_itemOptionInfo.OptionCountFromCombination == 4
                ? AnimatorHashGreatSuccess
                : AnimatorHashSuccess);
        }

        #region Invoke from Animation

        public void OnAnimatorStateBeginning(string stateName)
        {
            switch (stateName)
            {
                case "Show":
                    _disposableOfSkip = Observable.EveryUpdate()
                        .Where(_ => Input.GetMouseButtonDown(0) ||
                                    Input.GetKeyDown(KeyCode.Return) ||
                                    Input.GetKeyDown(KeyCode.KeypadEnter) ||
                                    Input.GetKeyDown(KeyCode.Escape))
                        .Take(1)
                        .DoOnCompleted(() => _disposableOfSkip = null)
                        .Subscribe(_ =>
                        {
                            AudioController.PlayClick();
                            SkipAnimation();
                        });
                    break;
            }
        }

        public void OnAnimatorStateEnd(string stateName)
        {
            switch (stateName)
            {
                case "Close":
                    base.Close(true);
                    break;
            }
        }

        public void OnRequestPlaySFX(string sfxCode) =>
            AudioController.instance.PlaySfx(sfxCode);

        public void ShowOptionIcon(int index)
        {
            if (index < 0 || index >= _itemOptionIconViews.Count)
            {
                Debug.LogError($"Invalid argument: {nameof(index)}({index})");
            }

            if (index >= _itemOptionInfo.OptionCountFromCombination)
            {
                return;
            }

            _itemOptionIconViews[index].Show();
        }

        public void HideOptionIcon(int index)
        {
            if (index < 0 || index >= _itemOptionIconViews.Count)
            {
                Debug.LogError($"Invalid argument: {nameof(index)}({index})");
            }

            if (index >= _itemOptionInfo.OptionCountFromCombination)
            {
                return;
            }

            _itemOptionIconViews[index].Hide();
        }

        public void PlayOptionAnimation()
        {
            if (_coroutineOfPlayOptionAnimation != null)
            {
                StopCoroutine(_coroutineOfPlayOptionAnimation);
            }

            _coroutineOfPlayOptionAnimation = StartCoroutine(CoPlayOptionAnimation());
        }

        #endregion

        private void SkipAnimation()
        {
            if (_disposableOfSkip != null)
            {
                _disposableOfSkip.Dispose();
                _disposableOfSkip = null;
            }

            if (_disposableOfCPAnimation != null)
            {
                _disposableOfCPAnimation.Dispose();
                _disposableOfCPAnimation = null;
            }

            if (_coroutineOfPlayOptionAnimation != null)
            {
                StopCoroutine(_coroutineOfPlayOptionAnimation);
                _coroutineOfPlayOptionAnimation = null;
            }

            Animator.Play(AnimatorHashLoop, 0, 0);

            for (var i = 0; i < _itemOptionIconViews.Count; i++)
            {
                _itemOptionIconViews[i].Hide(true);
            }

            for (var i = 0; i < _itemStatOptionViews.Count; i++)
            {
                var optionView = _itemStatOptionViews[i];
                if (optionView.IsEmpty)
                {
                    continue;
                }

                optionView.Discover(true);
            }

            for (var i = 0; i < _itemSkillOptionViews.Count; i++)
            {
                var optionView = _itemSkillOptionViews[i];
                if (optionView.IsEmpty)
                {
                    continue;
                }

                optionView.Discover(true);
            }

            if (_cpListForAnimationSteps.Any())
            {
                _resultItem.cpText.text = CPHelper
                    .DecimalToInt(_cpListForAnimationSteps[_cpListForAnimationSteps.Count - 1])
                    .ToString();
            }

            PressToContinue();
        }

        private IEnumerator CoPlayOptionAnimation()
        {
            yield return new WaitForSeconds(_delayTimeOfShowOptions);

            for (var i = 0; i < _itemStatOptionViews.Count; i++)
            {
                var optionView = _itemStatOptionViews[i];
                if (optionView.IsEmpty)
                {
                    continue;
                }

                optionView.Show();
            }

            for (var i = 0; i < _itemSkillOptionViews.Count; i++)
            {
                var optionView = _itemSkillOptionViews[i];
                if (optionView.IsEmpty)
                {
                    continue;
                }

                optionView.Show();
            }

            var step = 0;
            for (var i = 0; i < _itemStatOptionViews.Count; i++)
            {
                var optionView = _itemStatOptionViews[i];
                if (optionView.IsEmpty)
                {
                    continue;
                }

                yield return new WaitForSeconds(_intervalTimeOfShowOptions);
                optionView.Discover();
                yield return new WaitForSeconds(_delayTimeOfIncreaseCPAnimation);
                PlayCPAnimation(step++);
            }

            for (var i = 0; i < _itemSkillOptionViews.Count; i++)
            {
                var optionView = _itemSkillOptionViews[i];
                if (optionView.IsEmpty)
                {
                    continue;
                }

                yield return new WaitForSeconds(_intervalTimeOfShowOptions);
                optionView.Discover();
                yield return new WaitForSeconds(_delayTimeOfIncreaseCPAnimation);
                PlayCPAnimation(step++);
            }

            yield return null;

            _coroutineOfPlayOptionAnimation = null;

            if (_disposableOfSkip != null)
            {
                _disposableOfSkip.Dispose();
                _disposableOfSkip = null;
            }

            PressToContinue();
        }

        private void PlayCPAnimation(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= _cpListForAnimationSteps.Count - 1)
            {
                Debug.Log($"Argument out of range. {nameof(stepIndex)}({stepIndex})");
                return;
            }

            var from = CPHelper.DecimalToInt(_cpListForAnimationSteps[stepIndex]);
            var to = CPHelper.DecimalToInt(_cpListForAnimationSteps[stepIndex + 1]);

            if (_disposableOfCPAnimation != null)
            {
                _disposableOfCPAnimation.Dispose();
            }

            var deltaCP = to - from;
            var deltaTime = 0f;
            _disposableOfCPAnimation = Observable
                .EveryGameObjectUpdate()
                .Take(TimeSpan.FromSeconds(_dueTimeOfIncreaseCPAnimation))
                .DoOnCompleted(() => _disposableOfCPAnimation = null)
                .Subscribe(_ =>
                {
                    deltaTime += Time.deltaTime;
                    var middleCP = math.min(to, (int)(from + deltaCP * (deltaTime / .3f)));
                    _resultItem.cpText.text = middleCP.ToString();
                });
        }

        private void PressToContinue() => Observable.EveryUpdate()
            .Where(_ => Input.GetMouseButtonDown(0) ||
                        Input.GetKeyDown(KeyCode.Return) ||
                        Input.GetKeyDown(KeyCode.KeypadEnter) ||
                        Input.GetKeyDown(KeyCode.Escape))
            .First()
            .Subscribe(_ =>
            {
                AudioController.PlayClick();
                Animator.SetTrigger(AnimatorHashClose);
            });
    }
}
