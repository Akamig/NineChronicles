using Nekoyume.Game.Item;
using Nekoyume.UI.Model;
using TMPro;

namespace Nekoyume.UI.Module
{
    public class EnhancementMaterialView : CombinationMaterialView
    {
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI itemNameText;
        public StatView statView;

        public override void Set(InventoryItem inventoryItemViewModel, int count = 1)
        {
            if (!(inventoryItemViewModel?.ItemBase.Value is Equipment equipment))
            {
                Clear();
                return;
            }

            base.Set(inventoryItemViewModel, count);

            itemNameText.enabled = true;
            itemNameText.text = equipment.GetLocalizedName();
            UpdateStatView();
        }

        public void UpdateStatView(string additionalValueText = null)
        {
            if (!(Model?.ItemBase.Value is Equipment equipment))
                return;

            var statType = equipment.UniqueStatType;
            var statValue = equipment.StatsMap.GetStatValue(equipment.UniqueStatType);
            if (string.IsNullOrEmpty(additionalValueText))
            {
                statView.Show(statType, statValue);
            }
            else
            {
                statView.Show(statType.ToString(), $"{statValue}{additionalValueText}");
            }
        }

        public override void Clear()
        {
            itemNameText.enabled = false;
            statView.Hide();
            base.Clear();
        }
    }
}
