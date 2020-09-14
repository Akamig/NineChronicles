using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Tween;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EquipmentOptionRecipeView : EquipmentOptionView
    {
        [SerializeField]
        private RequiredItemRecipeView requiredItemRecipeView = null;

        [SerializeField]
        private Button button = null;

        [SerializeField]
        private GameObject lockParent = null;

        [SerializeField]
        private GameObject header = null;

        [SerializeField]
        private GameObject options = null;

        [SerializeField]
        protected RecipeClickVFX recipeClickVFX = null;

        [SerializeField]
        protected LockChainJitterVFX lockVFX = null;

        public RectTransformShakeTweener shakeTweener = null;
        public TransformLocalScaleTweener scaleTweener = null;

        private bool _tempLocked = false;

        private (int parentItemId, int index) _parentInfo;
        private EquipmentItemSubRecipeSheet.MaterialInfo _baseMaterialInfo;

        public EquipmentItemSubRecipeSheet.Row rowData;

        public readonly Subject<Unit> OnClick = new Subject<Unit>();

        public readonly Subject<EquipmentOptionRecipeView> OnClickVFXCompleted =
            new Subject<EquipmentOptionRecipeView>();

        private bool IsLocked => lockParent.activeSelf;
        private bool NotEnoughMaterials { get; set; } = true;

        private void Awake()
        {
            recipeClickVFX.OnTerminated = () => OnClickVFXCompleted.OnNext(this);

            button.OnClickAsObservable().Subscribe(_ =>
            {
                if (IsLocked && !_tempLocked)
                {
                    return;
                }
                scaleTweener.PlayTween();

                if (_tempLocked)
                {
                    AudioController.instance.PlaySfx(AudioController.SfxCode.UnlockRecipe);
                    var avatarState = Game.Game.instance.States.CurrentAvatarState;
                    var combination = Widget.Find<Combination>();
                    combination.RecipeVFXSkipMap[_parentInfo.parentItemId][_parentInfo.index] = rowData.Id;
                    combination.SaveRecipeVFXSkipMap();
                    Set(avatarState, false);
                    var centerPos = GetComponent<RectTransform>()
                        .GetWorldPositionOfCenter();
                    VFXController.instance.CreateAndChaseCam<ElementalRecipeUnlockVFX>(centerPos);
                    return;
                }

                if (NotEnoughMaterials)
                {
                    return;
                }

                OnClick.OnNext(Unit.Default);
                recipeClickVFX.Play();
            }).AddTo(gameObject);
        }

        private void OnDisable()
        {
            recipeClickVFX.Stop();
        }

        private void OnDestroy()
        {
            OnClickVFXCompleted.Dispose();
        }

        public void Show(
            string recipeName,
            int subRecipeId,
            EquipmentItemSubRecipeSheet.MaterialInfo baseMaterialInfo,
            bool checkInventory,
            (int parentItemId, int index)? parentInfo = null
        )
        {
            if (Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheet.TryGetValue(subRecipeId,
                out rowData))
            {
                _baseMaterialInfo = baseMaterialInfo;
                requiredItemRecipeView.SetData(baseMaterialInfo, rowData.Materials,
                    checkInventory);
            }
            else
            {
                Debug.LogWarning($"SubRecipe ID not found : {subRecipeId}");
                Hide();
                return;
            }

            if (parentInfo.HasValue)
            {
                _parentInfo = parentInfo.Value;
            }

            SetLocked(false);
            Show(recipeName, subRecipeId);
        }

        public void Set(AvatarState avatarState, bool tempLocked = false)
        {
            if (rowData is null)
            {
                return;
            }

            _tempLocked = tempLocked;
            SetLocked(tempLocked);

            if (tempLocked)
            {
                lockVFX?.Play();
                shakeTweener.PlayLoop();
            }
            else
            {
                lockVFX?.Stop();
                shakeTweener.KillTween();
            }

            if (tempLocked)
                return;

            // 재료 검사.
            var shouldDimmed = false;

            if (!CheckItem(avatarState, _baseMaterialInfo.Id, _baseMaterialInfo.Count))
            {
                shouldDimmed = true;
            }
            else
            {
                foreach (var info in rowData.Materials)
                {
                    if (CheckItem(avatarState, info.Id, info.Count))
                    {
                        continue;
                    }

                    shouldDimmed = true;
                    break;
                }
            }

            SetDimmed(shouldDimmed);
        }

        public void ShowLocked()
        {
            SetLocked(true);
            Show();
        }

        private bool CheckItem(AvatarState avatarState, int itemId, int count = 1)
        {
            var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
            var inventory = avatarState.inventory;

            return materialSheet.TryGetValue(itemId, out var materialRow) &&
                    inventory.TryGetMaterial(materialRow.ItemId, out var fungibleItem) &&
                    fungibleItem.count >= count;
        }

        private void SetLocked(bool value)
        {
            lockParent.SetActive(value);
            header.SetActive(!value);
            options.SetActive(!value);
            requiredItemRecipeView.gameObject.SetActive(!value);
            SetPanelDimmed(value);
        }

        public override void SetDimmed(bool value)
        {
            base.SetDimmed(value);
            NotEnoughMaterials = value;
        }
    }
}
