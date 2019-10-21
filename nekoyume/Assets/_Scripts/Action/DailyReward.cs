using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("daily_reward")]
    public class DailyReward : GameAction
    {
        public Address avatarAddress;
        public int refillPoint;

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.SetState(avatarAddress, MarkChanged);
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out AgentState agentState, out AvatarState avatarState))
            {
                return states;
            }

            if (!states.TryGetState(DailyBlockState.Address, out Bencodex.Types.Dictionary d))
            {
                return states;
            }
            var dailyBlockState = new DailyBlockState(d);

            if (avatarState.nextDailyRewardIndex <= dailyBlockState.nextBlockIndex)
            {
                avatarState.nextDailyRewardIndex = dailyBlockState.nextBlockIndex;
                avatarState.actionPoint = refillPoint;
            }

            return states.SetState(avatarAddress, avatarState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["avatarAddress"] = avatarAddress.Serialize(),
            ["refillPoint"] = (Integer) refillPoint,
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            refillPoint = (int) ((Integer) plainValue["refillPoint"]).Value;
        }
    }
}
