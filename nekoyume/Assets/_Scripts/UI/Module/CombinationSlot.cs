using System;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class CombinationSlot : MonoBehaviour
    {
        public UnityEngine.UI.Slider progressBar;
        public SimpleItemView resultView;
        public TextMeshProUGUI unlockText;
        public TextMeshProUGUI progressText;
        public TextMeshProUGUI lockText;
        public TextMeshProUGUI sliderText;
        public TouchHandler touchHandler;

        private CombinationSlotState _data;
        private int _slotIndex;

        private void Awake()
        {
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread().Subscribe(UpdateProgressBar).AddTo(gameObject);
            touchHandler.OnClick.Subscribe(pointerEventData =>
            {
                AudioController.PlayClick();
                ShowPopup();
            }).AddTo(gameObject);
        }

        public void SetData(CombinationSlotState state, long blockIndex, int slotIndex)
        {
            _data = state;
            _slotIndex = slotIndex;
            var unlock = States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(state.UnlockStage);
            lockText.gameObject.SetActive(!unlock);
            resultView.gameObject.SetActive(false);
            if (unlock)
            {
                var canUse = state.Validate(States.Instance.CurrentAvatarState, blockIndex);
                if (!(state.Result is null))
                {
                    canUse = canUse && state.Result.itemUsable.RequiredBlockIndex <= blockIndex;
                    resultView.SetData(new Item(state.Result.itemUsable));
                    resultView.gameObject.SetActive(!canUse);
                }
                unlockText.gameObject.SetActive(canUse);
                progressText.gameObject.SetActive(!canUse);
                progressBar.gameObject.SetActive(!canUse);
            }

            progressBar.maxValue = state.UnlockBlockIndex;
            sliderText.text = $"({progressBar.value} / {progressBar.maxValue})";
        }

        private void UpdateProgressBar(long index)
        {
            var value = Math.Min(index, progressBar.maxValue);
            progressBar.value = value;
            sliderText.text = $"({value} / {progressBar.maxValue})";
        }

        private void ShowPopup()
        {
            if (_data?.Result is null)
            {
                return;
            }

            if (_data.Result.itemUsable.RequiredBlockIndex > Game.Game.instance.Agent.BlockIndex)
            {
                Widget.Find<CombinationSlotPopup>().Pop(_data, _slotIndex);
            }
        }
    }
}
