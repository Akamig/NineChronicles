using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.Game.Mail;
using Nekoyume.Manager;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI;
using UniRx;
using UnityEngine;
using Combination = Nekoyume.Action.Combination;

namespace Nekoyume.BlockChain
{
    /// <summary>
    /// 현상태 : 각 액션의 랜더 단계에서 즉시 게임 정보에 반영시킴. 아바타를 선택하지 않은 상태에서 이전에 성공시키지 못한 액션을 재수행하고
    ///       이를 핸들링하면, 즉시 게임 정보에 반영시길 수 없기 때문에 에러가 발생함.
    /// 참고 : 이후 언랜더 처리를 고려한 해법이 필요함.
    /// 해법 1: 랜더 단계에서 얻는 `eval` 자체 혹은 변경점을 queue에 넣고, 게임의 상태에 따라 꺼내 쓰도록.
    /// </summary>
    public class ActionRenderHandler : ActionHandler
    {
        private static class Singleton
        {
            internal static readonly ActionRenderHandler Value = new ActionRenderHandler();
        }

        public static ActionRenderHandler Instance => Singleton.Value;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private ActionRenderHandler() : base()
        {
        }

        public void Start()
        {
            Shop();
            Ranking();
            RewardGold();
            CreateAvatar();
            DeleteAvatar();
            HackAndSlash();
            Combination();
            Sell();
            SellCancellation();
            Buy();
            RankingReward();
            AddItem();
            AddGold();
            DailyReward();
            ItemEnhancement();
            QuestReward();
            RankingBattle();
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }

        private void Shop()
        {
            ActionBase.EveryRender(ShopState.Address)
                .ObserveOnMainThread()
                .Subscribe(UpdateShopState).AddTo(_disposables);
        }

        private void Ranking()
        {
            ActionBase.EveryRender(RankingState.Address)
                .ObserveOnMainThread()
                .Subscribe(UpdateRankingState).AddTo(_disposables);
        }

        private void RewardGold()
        {
            ActionBase.EveryRender<RewardGold>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(UpdateAgentState).AddTo(_disposables);
        }

        private void CreateAvatar()
        {
            ActionBase.EveryRender<CreateAvatar>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    UpdateAgentState(eval);
                    UpdateAvatarState(eval, eval.Action.index);
                }).AddTo(_disposables);
        }

        private void DeleteAvatar()
        {
            ActionBase.EveryRender<DeleteAvatar>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    UpdateAgentState(eval);
                    UpdateAvatarState(eval, eval.Action.index);
                }).AddTo(_disposables);
        }

        private void HackAndSlash()
        {
            ActionBase.EveryRender<HackAndSlash>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseHackAndSlash).AddTo(_disposables);
        }

        private void Combination()
        {
            ActionBase.EveryRender<Combination>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseCombination).AddTo(_disposables);
        }

        private void Sell()
        {
            ActionBase.EveryRender<Sell>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseSell).AddTo(_disposables);
        }

        private void SellCancellation()
        {
            ActionBase.EveryRender<SellCancellation>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseSellCancellation).AddTo(_disposables);
        }

        private void Buy()
        {
            ActionBase.EveryRender<Buy>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(ResponseBuy).AddTo(_disposables);
        }

        private void RankingReward()
        {
            ActionBase.EveryRender<RankingReward>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(UpdateAgentState).AddTo(_disposables);
        }

        private void AddItem()
        {
            ActionBase.EveryRender<AddItem>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(UpdateCurrentAvatarState).AddTo(_disposables);
        }

        private void AddGold()
        {
            ActionBase.EveryRender<AddGold>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    UpdateAgentState(eval);
                    UpdateCurrentAvatarState(eval);
                }).AddTo(_disposables);
        }

        private void ItemEnhancement()
        {
            ActionBase.EveryRender<ItemEnhancement>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(ResponseItemEnhancement).AddTo(_disposables);
        }

        private void DailyReward()
        {
            ActionBase.EveryRender<DailyReward>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(UpdateCurrentAvatarState).AddTo(_disposables);
        }

        private void QuestReward()
        {
            ActionBase.EveryRender<QuestReward>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseQuestReward).AddTo(_disposables);
        }

        private void RankingBattle()
        {
            ActionBase.EveryRender<RankingBattle>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseRankingBattle).AddTo(_disposables);
        }

        private void ResponseCombination(ActionBase.ActionEvaluation<Combination> evaluation)
        {
            var itemUsable = evaluation.Action.Result.itemUsable;
            var isSuccess = !(itemUsable is null);
            if (isSuccess)
            {
                var format = LocalizationManager.Localize("NOTIFICATION_COMBINATION_COMPLETE");
                UI.Notification.Push(MailType.Workshop, string.Format(format, itemUsable.Data.GetLocalizedName()));
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionCombinationSuccess);
            }
            else
            {
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionCombinationFail);
                var format = LocalizationManager.Localize("NOTIFICATION_COMBINATION_FAIL");
                UI.Notification.Push(MailType.Workshop, format);
            }
            UpdateCurrentAvatarState(evaluation);
        }

        private void ResponseSell(ActionBase.ActionEvaluation<Sell> eval)
        {
            var format = LocalizationManager.Localize("NOTIFICATION_SELL_COMPLETE");
            UI.Notification.Push(MailType.Auction, string.Format(format, eval.Action.itemUsable.GetLocalizedName()));
            UpdateCurrentAvatarState(eval);
        }

        private void ResponseSellCancellation(ActionBase.ActionEvaluation<SellCancellation> eval)
        {
            var format = LocalizationManager.Localize("NOTIFICATION_SELL_CANCEL_COMPLETE");
            UI.Notification.Push(MailType.Auction, string.Format(format, eval.Action.result.itemUsable.GetLocalizedName()));
            UpdateCurrentAvatarState(eval);
        }

        private void ResponseBuy(ActionBase.ActionEvaluation<Buy> eval)
        {
            if (eval.Action.buyerAvatarAddress == States.Instance.CurrentAvatarState.Value.address)
            {
                var format = LocalizationManager.Localize("NOTIFICATION_BUY_BUYER_COMPLETE");
                UI.Notification.Push(MailType.Auction, string.Format(format, eval.Action.buyerResult.itemUsable.GetLocalizedName()));
            }
            else
            {
                var format = LocalizationManager.Localize("NOTIFICATION_BUY_SELLER_COMPLETE");
                var buyerName =
                    new AvatarState(
                            (Bencodex.Types.Dictionary) eval.OutputStates.GetState(eval.Action.buyerAvatarAddress))
                        .NameWithHash;
                var result = eval.Action.sellerResult;
                UI.Notification.Push(MailType.Auction, string.Format(format, buyerName, result.itemUsable.GetLocalizedName()));
            }

            UpdateCurrentAvatarState(eval);
        }
        
        private void ResponseHackAndSlash(ActionBase.ActionEvaluation<HackAndSlash> eval)
        {
            UpdateCurrentAvatarState(eval);

            var actionFailPopup = Widget.Find<ActionFailPopup>();
            actionFailPopup.CloseCallback = null;
            actionFailPopup.Close();

            if (Widget.Find<QuestPreparation>().IsActive() &&
                Widget.Find<LoadingScreen>().IsActive())
            {
                Widget.Find<QuestPreparation>().GoToStage(eval);
            }
            else if (Widget.Find<BattleResult>().IsActive() &&
                Widget.Find<StageLoadingScreen>().IsActive())
            {
                Widget.Find<BattleResult>().NextStage(eval);
            }
        }

        private void ResponseQuestReward(ActionBase.ActionEvaluation<QuestReward> eval)
        {
            UpdateCurrentAvatarState(eval);
            var format = LocalizationManager.Localize("NOTIFICATION_QUEST_REWARD");
            var msg = string.Format(format, eval.Action.Result.GetName());
            UI.Notification.Push(MailType.System, msg);
        }

        private void ResponseItemEnhancement(ActionBase.ActionEvaluation<ItemEnhancement> eval)
        {
            var format = LocalizationManager.Localize("NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE");
            UI.Notification.Push(MailType.Workshop,
                string.Format(format, eval.Action.result.itemUsable.Data.GetLocalizedName()));
            UpdateAgentState(eval);
            UpdateCurrentAvatarState(eval);
        }

        private void ResponseRankingBattle(ActionBase.ActionEvaluation<RankingBattle> eval)
        {
            UpdateCurrentAvatarState(eval);

            var actionFailPopup = Widget.Find<ActionFailPopup>();
            actionFailPopup.CloseCallback = null;
            actionFailPopup.Close();

            Widget.Find<RankingBoard>().GoToStage(eval);
        }
    }
}
