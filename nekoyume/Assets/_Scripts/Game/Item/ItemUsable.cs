using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.Game.Item
{
    // todo: 소모품과 장비가 함께 쓰기에는 장비 위주의 모델이 된 느낌. 아이템 정리하면서 정리를 흐음..
    [Serializable]
    public abstract class ItemUsable : ItemBase
    {
        public new ItemSheet.Row Data { get; }
        public Guid ItemId { get; }
        public StatsMap StatsMap { get; }
        public List<Skill> Skills { get; }
        public List<BuffSkill> BuffSkills { get; }

        protected ItemUsable(ItemSheet.Row data, Guid id) : base(data)
        {
            Data = data;
            ItemId = id;
            StatsMap = new StatsMap();

            switch (data)
            {
                case ConsumableItemSheet.Row consumableItemRow:
                {
                    foreach (var statData in consumableItemRow.Stats)
                    {
                        StatsMap.AddStatValue(statData.StatType, statData.Value);
                    }

                    break;
                }
                case EquipmentItemSheet.Row equipmentItemRow:
                    StatsMap.AddStatValue(equipmentItemRow.Stat.Type, equipmentItemRow.Stat.Value);
                    break;
            }

            Skills = new List<Skill>();
            BuffSkills = new List<BuffSkill>();
        }

        protected bool Equals(ItemUsable other)
        {
            return base.Equals(other) && string.Equals(ItemId, other.ItemId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ItemUsable) obj);
        }

        public override int GetHashCode()
        {
            return (Data != null ? Data.GetHashCode() : 0) ^ ItemId.GetHashCode();
        }

        public int GetOptionCount()
        {
            return StatsMap.GetAdditionalStats().Count()
                   + Skills.Count
                   + BuffSkills.Count;
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "itemId"] = ItemId.Serialize(),
                [(Text) "statsMap"] = StatsMap.Serialize(),
                [(Text) "skills"] = new Bencodex.Types.List(Skills.Select(s => s.Serialize())),
                [(Text) "buffSkills"] = new Bencodex.Types.List(BuffSkills.Select(s => s.Serialize())),
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
    }
}
