using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class WorldQuest : Quest
    {
        private readonly int _goal;
        public WorldQuest(WorldQuestSheet.Row data) : base(data)
        {
            _goal = data.Goal;
        }

        public WorldQuest(Dictionary serialized) : base(serialized)
        {
            _goal = (int) ((Integer) serialized[(Bencodex.Types.Text) "goal"]).Value;
        }

        public override void Check()
        {
        }

        public override string ToInfo()
        {
            if (Game.instance.TableSheets.WorldSheet.TryGetByStageId(_goal, out var worldRow))
            {
                var format = LocalizationManager.Localize("QUEST_WORLD_FORMAT");
                return string.Format(format, worldRow.GetLocalizedName());
            }
            throw new SheetRowNotFoundException("WorldSheet", "TryGetByStageId()", _goal.ToString());
        }

        protected override string TypeId => "worldQuest";

        public void Update(CollectionMap stageMap)
        {
            Complete = stageMap.TryGetValue(_goal, out _);
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "goal"] = (Integer) _goal,
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
    }
}
