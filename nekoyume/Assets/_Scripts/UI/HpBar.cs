using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.UI.Module;
using TMPro;
using Unity.Mathematics;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class HpBar : ProgressBar
    {
        public BuffLayout buffLayout;
        public TextMeshProUGUI levelText;
        public Slider additionalSlider;

        public HpBarVFX HpVFX { get; protected set; }

        public void SetBuffs(IReadOnlyDictionary<int, Buff> buffs)
        {
            buffLayout.SetBuff(buffs);

            if (buffLayout.IsBuffAdded(EnumType.StatType.HP))
            {
                HpVFX?.Stop();
                var rectTransform = bar.rectTransform;
                HpVFX = VFXController.instance.CreateAndChaseRectTransform<HpBarVFX>(rectTransform.position, rectTransform);
                HpVFX.Play();
            }
            else if (!buffLayout.HasBuff(EnumType.StatType.HP))
            {
                HpVFX?.Stop();
            }
        }

        public void SetLevel(int value)
        {
            levelText.text = value.ToString();
        }

        public void Set(int current, int additional, int max)
        {
            SetText($"{current} / {max}");
            SetValue((float) math.min(current, max - additional) / max);

            bool isHPBoosted = additional > 0;
            additionalSlider.gameObject.SetActive(isHPBoosted);
            if (isHPBoosted)
                additionalSlider.value = (float) current / max;
        }
    }
}
