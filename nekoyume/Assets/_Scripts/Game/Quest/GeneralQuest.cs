using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.EnumType;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class GeneralQuest : Quest
    {
        public readonly QuestEventType Event;

        public GeneralQuest(GeneralQuestSheet.Row data) : base(data)
        {
            Event = data.Event;
        }

        public GeneralQuest(Dictionary serialized) : base(serialized)
        {
            Event = (QuestEventType) (int) ((Integer) serialized["event"]).Value;
        }

        public override QuestType QuestType
        {
            get
            {
                switch (Event)
                {
                    case QuestEventType.Create:
                    case QuestEventType.Level:
                    case QuestEventType.Die:
                    case QuestEventType.Complete:
                        return QuestType.Adventure;
                    case QuestEventType.Enhancement:
                    case QuestEventType.Equipment:
                    case QuestEventType.Consumable:
                        return QuestType.Craft;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override void Check()
        {
            if (Complete)
                return;
            
            Complete = _current >= Goal;
        }

        public override string GetName()
        {
            return LocalizationManager.Localize($"QUEST_GENERAL_{Event}_FORMAT");
        }

        public override string GetProgressText()
        {
            return string.Format(GoalFormat, Math.Min(Goal, _current), Goal);
        }

        protected override string TypeId => "generalQuest";

        public void Update(CollectionMap eventMap)
        {
            if (Complete)
                return;
            
            var key = (int) Event;
            eventMap.TryGetValue(key, out _current);
            Check();
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "event"] = (Integer) (int) Event,
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));

    }
}
