using System;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.UI.Model;

namespace Nekoyume.UI.Module
{
    public class CombineEquipment : CombinationPanel<CombinationMaterialView>
    {
        public override bool IsSubmittable =>
            !(States.Instance.AgentState.Value is null) &&
            States.Instance.AgentState.Value.gold >= CostNCG &&
            !(States.Instance.CurrentAvatarState.Value is null) &&
            States.Instance.CurrentAvatarState.Value.actionPoint >= CostAP &&
            !(baseMaterial is null) &&
            !baseMaterial.IsEmpty &&
            otherMaterials.Any(e => !e.IsEmpty);


        protected override void Awake()
        {
            base.Awake();

            if (baseMaterial is null)
                throw new SerializeFieldNullException();

            submitButton.submitText.text = LocalizationManager.Localize("UI_COMBINATION_ITEM");
        }

        public override void Show()
        {
            base.Show();

            baseMaterial.Unlock();

            foreach (var material in otherMaterials)
            {
                material.Lock();
            }
        }

        public override bool DimFunc(InventoryItem inventoryItem)
        {
            var row = inventoryItem.ItemBase.Value.Data;
            if (row.ItemType != ItemType.Material)
                return true;

            if (!IsThereAnyUnlockedEmptyMaterialView)
            {
                if (row.ItemSubType == ItemSubType.EquipmentMaterial)
                    return false;
                
                return !Contains(inventoryItem);
            }
            
            if (baseMaterial.IsEmpty)
            {
                if (row.ItemSubType != ItemSubType.EquipmentMaterial)
                    return true;
            }
            else if (row.ItemSubType != ItemSubType.EquipmentMaterial &&
                     row.ItemSubType != ItemSubType.MonsterPart)
                return true;

            return false;
        }

        protected override int GetCostNCG()
        {
            if (baseMaterial.IsEmpty)
                return 0;

            Game.Game.instance.TableSheets.ItemConfigForGradeSheet.TryGetValue(
                baseMaterial.Model.ItemBase.Value.Data.Grade,
                out var configRow, true);

            var otherMaterialsCount = otherMaterials.Count(e => !e.IsEmpty && !e.IsLocked);
            var ncgCount = Math.Max(0, otherMaterialsCount - configRow.MonsterPartsCountForCombination);
            return ncgCount * GameConfig.CombineEquipmentCostNCG;
        }

        protected override int GetCostAP()
        {
            return baseMaterial.IsEmpty ? 0 : GameConfig.CombineEquipmentCostAP;
        }

        protected override bool TryAddBaseMaterial(InventoryItemView view, out CombinationMaterialView materialView)
        {
            if (view.Model is null ||
                view.Model.ItemBase.Value.Data.ItemType != ItemType.Material ||
                view.Model.ItemBase.Value.Data.ItemSubType != ItemSubType.EquipmentMaterial)
            {
                materialView = null;
                return false;
            }

            if (base.TryAddBaseMaterial(view, out materialView))
            {
                Game.Game.instance.TableSheets.ItemConfigForGradeSheet.TryGetValue(view.Model.ItemBase.Value.Data.Grade,
                    out var configRow, true);

                for (var i = 0; i < otherMaterials.Length; i++)
                {
                    var material = otherMaterials[i];
                    if (i < configRow.MonsterPartsCountForCombination)
                    {
                        material.Unlock();
                    }
                    else if (i < configRow.MonsterPartsCountForCombination +
                             configRow.MonsterPartsCountForCombinationWithNCG)
                    {
                        material.UnlockAsNCG();
                    }
                    else
                    {
                        material.Lock();
                    }
                }

                return true;
            }

            return false;
        }

        protected override bool TryRemoveBaseMaterial(CombinationMaterialView view, out CombinationMaterialView materialView)
        {
            if (!base.TryRemoveBaseMaterial(view, out materialView))
                return false;

            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.Clear();
                otherMaterial.Lock();
            }

            return true;
        }
    }
}
