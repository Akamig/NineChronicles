using System;
using System.Collections.Generic;
using Bencodex.Types;
using Nekoyume.State;

namespace Nekoyume.TableData
{
    [Serializable]
    public class QuestSheet : Sheet<int, QuestSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>, IState
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public int Goal { get; private set; }
            public decimal Reward { get; private set; }
            
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                Goal = int.Parse(fields[1]);
                Reward = decimal.Parse(fields[2]);
            }

            public IValue Serialize() =>
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "key"] = (Integer) Key,
                });

            public static Row Deserialize(Bencodex.Types.Dictionary serialized)
            {
                var key = (int) ((Integer) serialized[(Text) "key"]).Value;
                return Game.Game.instance.TableSheets.QuestSheet[key];
            }
        }
        
        public QuestSheet() : base(nameof(QuestSheet))
        {
        }
    }
}
