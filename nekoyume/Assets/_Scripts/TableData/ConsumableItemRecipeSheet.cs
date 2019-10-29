using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.TableData
{
    public class ConsumableItemRecipeSheet : Sheet<int, ConsumableItemRecipeSheet.Row> 
    {
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public List<int> MaterialItemIds { get; private set; }
            public int ResultConsumableItemId { get; private set; }
            
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                MaterialItemIds = new List<int>();
                for (var i = 1; i < 5; i++)
                {
                    if (string.IsNullOrEmpty(fields[i]))
                        break;
                    
                    MaterialItemIds.Add(int.Parse(fields[i]));
                }
                MaterialItemIds.Sort((left, right) => left - right);
                
                ResultConsumableItemId = int.Parse(fields[5]);
            }

            public bool Equals(IEnumerable<int> materialItemIds)
            {
                return materialItemIds.All(materialItemId => MaterialItemIds.Contains(materialItemId));
            }
        }

        public ConsumableItemRecipeSheet() : base(nameof(ConsumableItemRecipeSheet))
        {
        }

        public bool TryGetValue(IEnumerable<int> materialItemIds, out Row row, bool throwException = false)
        {
            foreach (var value in Values.Where(value => value.Equals(materialItemIds)))
            {
                row = value;
                return true;
            }

            row = null;
            return false;
        }

        public bool TryGetValue(IEnumerable<MaterialItemSheet.Row> materialItemRows, out Row row, bool throwException = false)
        {
            return TryGetValue(materialItemRows.Select(materialItemRow => materialItemRow.Id), out row, throwException);
        }
    }
}
