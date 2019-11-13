using System;
using System.Collections.Generic;
using Nekoyume.EnumType;

namespace Nekoyume.TableData
{
    [Serializable]
    public class GoldQuestSheet : Sheet<int, GoldQuestSheet.Row>
    {
        public class Row : QuestSheet.Row
        {
            public TradeType Type { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                Type = (TradeType) Enum.Parse(typeof(TradeType), fields[3]);
            }
        }

        public GoldQuestSheet() : base(nameof(GoldQuestSheet))
        {
        }
    }
}
