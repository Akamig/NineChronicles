using Nekoyume.Model.State;
using Nekoyume.UI.Module;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.TableData.Crystal;

namespace Nekoyume.UI
{
    using Nekoyume.State;
    using TMPro;
    using UniRx;

    public class BuffBonusResultPopup : PopupWidget
    {
        [SerializeField]
        private BonusBuffViewDataScriptableObject bonusBuffViewData;

        [SerializeField]
        private Button retryButton = null;

        [SerializeField]
        private Image selectedBuffBg = null;

        [SerializeField]
        private Image selectedBuffIcon = null;

        [SerializeField]
        private TextMeshProUGUI selectedBuffText = null;

        [SerializeField]
        private List<GameObject> buffViewParents = null;

        public int? SelectedSkillId { get; set; }

        private int _stageId;

        private readonly Dictionary<GameObject, BonusBuffView> buffViewMap
            = new Dictionary<GameObject, BonusBuffView>();

        public readonly Subject<CrystalRandomBuffSheet.Row> OnBuffSelectedSubject
            = new Subject<CrystalRandomBuffSheet.Row>();

        protected override void Awake()
        {
            base.Awake();

            foreach (var viewParent in buffViewParents)
            {
                var view = viewParent.GetComponentInChildren<BonusBuffView>();
                buffViewMap[viewParent] = view;
                view.BonusBuffViewData = bonusBuffViewData;
            }

            OnBuffSelectedSubject.Subscribe(row =>
            {
                foreach (var viewParent in buffViewParents)
                {
                    var inner = viewParent;
                    if (!inner.activeSelf)
                    {
                        continue;
                    }

                    var view = buffViewMap[inner];
                    if (view.UpdateSelected(row))
                    {
                        selectedBuffText.text = view.CurrentSkillName;
                        selectedBuffIcon.sprite = view.CurrentIcon;
                        selectedBuffBg.sprite = view.CurrentGradeData.BgSprite;
                        SelectedSkillId = row.Id;
                    }
                }
            })
            .AddTo(gameObject);
            retryButton.onClick.AddListener(Retry);
        }

        public void Show(int stageId, CrystalRandomSkillState state)
        {
            _stageId = stageId;
            foreach (var viewParent in buffViewParents)
            {
                viewParent.SetActive(false);
            }

            var buffs = state.SkillIds
                .Select(buffId =>
                {
                    var randomBuffSheet = Game.Game.instance.TableSheets.CrystalRandomBuffSheet;
                    if (!randomBuffSheet.TryGetValue(buffId, out var bonusBuffRow))
                    {
                        return null;
                    }
                    return bonusBuffRow;
                })
                .OrderBy(x => x.Rank)
                .ThenBy(x => x.Id);

            foreach (var buff in buffs)
            {
                var viewParent = buffViewParents.First(x => !x.activeSelf);
                var view = buffViewMap[viewParent];
                view.SetData(buff, OnBuffSelectedSubject.OnNext);
                viewParent.SetActive(true);
            }

            var selectedBuff = SelectedSkillId.HasValue ?
                buffs.First(x => x.Id == SelectedSkillId) : buffs.First();
            SelectedSkillId = selectedBuff.Id;
            OnBuffSelectedSubject.OnNext(selectedBuff);
            base.Show();
        }

        private void Retry()
        {
            Close();
            SelectedSkillId = null;
            var hasEnoughStar =
                Game.Game.instance.TableSheets.CrystalStageBuffGachaSheet.TryGetValue(_stageId, out var row)
                && States.Instance.CrystalRandomSkillState.StarCount >= row.MaxStar;
            Find<BuffBonusPopup>().Show(_stageId, hasEnoughStar);
        }
    }
}
