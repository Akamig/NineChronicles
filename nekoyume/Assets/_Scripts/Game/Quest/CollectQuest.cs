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
    public class CollectQuest : Quest
    {
        public override QuestType QuestType => QuestType.Obtain;

        private int _current;

        private readonly int _itemId;

        public CollectQuest(CollectQuestSheet.Row data) : base(data)
        {
            _itemId = data.ItemId;
        }

        public CollectQuest(Dictionary serialized) : base(serialized)
        {
            _itemId = (int) ((Integer) serialized["itemId"]).Value;
            _current = (int) ((Integer) serialized["current"]).Value;
        }

        public override void Check()
        {
            if (Complete)
                return;
            
            Complete = _current >= Goal;
        }

        public override string ToInfo()
        {
            return string.Format(GoalFormat, GetName(), Math.Min(Goal, _current), Goal);
        }

        public override string GetName()
        {
            var format = LocalizationManager.Localize("QUEST_COLLECT_CURRENT_INFO_FORMAT");
            var itemName = LocalizationManager.LocalizeItemName(_itemId);
            return string.Format(format, itemName);
        }

        protected override string TypeId => "collectQuest";

        public void Update(CollectionMap itemMap)
        {
            if (Complete)
                return;
            
            itemMap.TryGetValue(_itemId, out _current);
            Check();
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "current"] = (Integer) _current,
                [(Text) "itemId"] = (Integer) _itemId,
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));

    }
}
