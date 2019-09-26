using System;
using Nekoyume.TableData;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Belt : Equipment
    {
        public Belt(EquipmentItemSheet.Row data, Guid id) : base(data, id)
        {
        }
    }
}
