using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet;
using Nekoyume.EnumType;

namespace Nekoyume.TableData
{
    [Serializable]
    public class MaterialItemSheet : Sheet<int, MaterialItemSheet.Row>
    {
        [Serializable]
        public class Row : ItemSheet.Row
        {
            public HashDigest<SHA256> ItemId { get; private set; }
            public override ItemType ItemType => ItemType.Material;
            public StatType? StatType { get; private set; }
            public int StatMin { get; private set; }
            public int StatMax { get; private set; }
            public int SkillId { get; private set; }
            public int SkillDamageMin { get; private set; }
            public int SkillDamageMax { get; private set; }
            public int SkillChanceMin { get; private set; }
            public int SkillChanceMax { get; private set; }

            public Row() {}

            public Row(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                ItemId = Hashcash.Hash(serialized.EncodeIntoChunks().SelectMany(b => b).ToArray());
                StatType = StatTypeExtension.Deserialize((Binary) serialized["stat_type"]);
                StatMin = (Integer) serialized["stat_min"];
                StatMax = (Integer) serialized["stat_max"];
                SkillId = (Integer) serialized["skill_id"];
                SkillDamageMin = (Integer) serialized["skill_damage_min"];
                SkillDamageMax = (Integer) serialized["skill_damage_max"];
                SkillChanceMin = (Integer) serialized["skill_chance_min"];
                SkillChanceMax = (Integer) serialized["skill_chance_max"];
            }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                StatType = string.IsNullOrEmpty(fields[4])
                    ? EnumType.StatType.NONE
                    : (StatType) Enum.Parse(typeof(StatType), fields[4]);
                StatMin = string.IsNullOrEmpty(fields[5]) ? 0 : int.Parse(fields[5]);
                StatMax = string.IsNullOrEmpty(fields[6]) ? 0 : int.Parse(fields[6]);
                SkillId = string.IsNullOrEmpty(fields[7]) ? 0 : int.Parse(fields[7]);
                SkillDamageMin = string.IsNullOrEmpty(fields[8]) ? 0 : int.Parse(fields[8]);
                SkillDamageMax = string.IsNullOrEmpty(fields[9]) ? 0 : int.Parse(fields[9]);
                SkillChanceMin = string.IsNullOrEmpty(fields[10]) ? 0 : int.Parse(fields[10]);
                SkillChanceMax = string.IsNullOrEmpty(fields[11]) ? 0 : int.Parse(fields[11]);
                ItemId = Hashcash.Hash(Serialize().EncodeIntoChunks().SelectMany(b => b).ToArray());
            }

            public override IValue Serialize() => new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "stat_type"] = (StatType ?? EnumType.StatType.NONE).Serialize(),
                [(Text) "stat_min"] = (Integer) StatMin,
                [(Text) "stat_max"] = (Integer) StatMax,
                [(Text) "skill_id"] = (Integer) SkillId,
                [(Text) "skill_damage_min"] = (Integer) SkillDamageMin,
                [(Text) "skill_damage_max"] = (Integer) SkillDamageMax,
                [(Text) "skill_chance_min"] = (Integer) SkillChanceMin,
                [(Text) "skill_chance_max"] = (Integer) SkillChanceMax,
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
        }
        
        public MaterialItemSheet() : base(nameof(MaterialItemSheet))
        {
        }
    }
}
