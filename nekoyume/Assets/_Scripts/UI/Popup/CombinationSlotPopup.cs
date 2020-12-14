using System;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Action;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI
{
    public class CombinationSlotPopup : PopupWidget
    {
        public RecipeCellView recipeCellView;
        public CombinationMaterialPanel materialPanel;
        public EquipmentOptionRecipeView optionView;
        public SubmitWithCostButton submitButton;
        public TouchHandler touchHandler;

        private int _slotIndex;
        private int _cost;
        private CombinationSelectSmallFrontVFX _frontVFX;
        private MaterialItemSheet.Row _row;

        protected override void Awake()
        {
            base.Awake();

            submitButton.SetSubmitText(
                L10nManager.Localize("UI_COMBINATION_WAITING"),
                L10nManager.Localize("UI_RAPID_COMBINATION")
            );

            submitButton.OnSubmitClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                RapidCombination();
                Close();
            }).AddTo(gameObject);
            touchHandler.OnClick.Subscribe(pointerEventData =>
            {
                if (pointerEventData.pointerCurrentRaycast.gameObject.Equals(gameObject))
                {
                    AudioController.PlayClick();
                    Close();
                }
            }).AddTo(gameObject);

            CloseWidget = null;
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            if (_frontVFX)
            {
                _frontVFX.Stop();
            }
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
            _frontVFX =
                VFXController.instance.CreateAndChase<CombinationSelectSmallFrontVFX>(
                    recipeCellView.transform,
                    new Vector3(0.53f, -0.5f));
        }

        public void Pop(CombinationSlotState state, int slotIndex)
        {
            _slotIndex = slotIndex;
            CombinationConsumable.ResultModel result;
            CombinationConsumable.ResultModel chainResult;
            try
            {
                result = (CombinationConsumable.ResultModel) state.Result;
                var chainState = new CombinationSlotState((Dictionary)Game.Game.instance.Agent.GetState(state.address));
                chainResult = (CombinationConsumable.ResultModel) chainState.Result;
            }
            catch (InvalidCastException)
            {
                return;
            }
            var subRecipeEnabled = result.subRecipeId.HasValue;
            materialPanel.gameObject.SetActive(false);
            optionView.gameObject.SetActive(false);
            switch (result.itemType)
            {
                case ItemType.Equipment:
                {
                    var recipeRow =
                        Game.Game.instance.TableSheets.EquipmentItemRecipeSheet.Values.First(r =>
                            r.Id == result.recipeId);

                    recipeCellView.Set(recipeRow);
                    if (subRecipeEnabled)
                    {
                        optionView.Show(
                            result.itemUsable.GetLocalizedName(),
                            (int) result.subRecipeId,
                            new EquipmentItemSubRecipeSheet.MaterialInfo(
                                recipeRow.MaterialId,
                                recipeRow.MaterialCount),
                            false
                        );
                    }
                    else
                    {
                        materialPanel.SetData(recipeRow, null, false);
                        materialPanel.gameObject.SetActive(true);
                    }

                    break;
                }
                case ItemType.Consumable:
                {
                    var recipeRow =
                        Game.Game.instance.TableSheets.ConsumableItemRecipeSheet.Values.First(r =>
                            r.Id == result.recipeId);

                    recipeCellView.Set(recipeRow);
                    materialPanel.SetData(recipeRow, false, true);
                    materialPanel.gameObject.SetActive(true);
                    break;
                }
            }

            submitButton.HideAP();
            submitButton.HideNCG();
            var diff = result.itemUsable.RequiredBlockIndex - Game.Game.instance.Agent.BlockIndex;
            if (diff < 0)
            {
                submitButton.SetSubmitText(
                    L10nManager.Localize("UI_COMBINATION_WAITING"),
                    L10nManager.Localize("UI_RAPID_COMBINATION")
                );
                submitButton.SetSubmittable(result.id == chainResult?.id);
                submitButton.HideHourglass();
            }
            else
            {
                _cost = Action.RapidCombination.CalculateHourglassCount(
                    States.Instance.GameConfigState,
                    diff);
                _row = Game.Game.instance.TableSheets.MaterialItemSheet.Values
                    .First(r => r.ItemSubType == ItemSubType.Hourglass);
                var isEnough =
                    States.Instance.CurrentAvatarState.inventory.HasItem(_row.ItemId, _cost);

                var count = States.Instance.CurrentAvatarState.inventory
                    .TryGetMaterial(_row.ItemId, out var glass) ? glass.count : 0;

                if (result.id == chainResult?.id)
                {
                    submitButton.SetSubmitText(
                        L10nManager.Localize("UI_RAPID_COMBINATION"));
                    submitButton.SetSubmittable(isEnough);
                }
                else
                {
                    submitButton.SetSubmitText(
                        L10nManager.Localize("UI_COMBINATION_WAITING"));
                    submitButton.SetSubmittable(false);
                }

                submitButton.ShowHourglass(_cost, count);
            }

            base.Show();
        }

        private void RapidCombination()
        {
            LocalLayerModifier.RemoveItem(States.Instance.CurrentAvatarState.address, _row.ItemId,
                _cost);
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            LocalLayerModifier.UnlockCombinationSlot(_slotIndex, blockIndex);
            var slotAddress = States.Instance.CurrentAvatarState.address.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    _slotIndex
                )
            );
            var slotState = States.Instance.CombinationSlotStates[slotAddress];
            var result = (CombinationConsumable.ResultModel) slotState.Result;
            LocalLayerModifier.AddNewResultAttachmentMail(
                States.Instance.CurrentAvatarState.address, result.id, blockIndex);
            var format = L10nManager.Localize("NOTIFICATION_COMBINATION_COMPLETE");
            Notification.Push(
                MailType.Workshop,
                string.Format(CultureInfo.InvariantCulture, format,
                    result.itemUsable.GetLocalizedName())
            );
            Notification.CancelReserve(result.itemUsable.ItemId);
            Game.Game.instance.ActionManager.RapidCombination(_slotIndex);
        }
    }
}
