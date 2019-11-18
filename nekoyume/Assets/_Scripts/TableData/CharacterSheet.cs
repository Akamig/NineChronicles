using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Game;

namespace Nekoyume.TableData
{
    [Serializable]
    public class CharacterSheet : Sheet<int, CharacterSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public SizeType SizeType { get; private set; }
            public ElementalType ElementalType { get; private set; }
            public int HP { get; private set; }
            public int ATK { get; private set; }
            public int DEF { get; private set; }
            public decimal CRI { get; private set; }
            public decimal DOG { get; private set; }
            public decimal SPD { get; private set; }
            public int LvHP { get; private set; }
            public int LvATK { get; private set; }
            public int LvDEF { get; private set; }
            public decimal LvCRI { get; private set; }
            public decimal LvDOG { get; private set; }
            public decimal LvSPD { get; private set; }
            public float AttackRange { get; private set; }
            public float RunSpeed { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                SizeType = (SizeType) Enum.Parse(typeof(SizeType), fields[1]);
                ElementalType = Enum.TryParse<ElementalType>(fields[2], out var elementalType)
                    ? elementalType
                    : ElementalType.Normal;
                HP = int.TryParse(fields[3], out var hp) ? hp : 0;
                ATK = int.TryParse(fields[4], out var damage) ? damage : 0;
                DEF = int.TryParse(fields[5], out var defense) ? defense : 0;
                CRI = decimal.TryParse(fields[6], out var cri) ? cri : 0m;
                DOG = decimal.TryParse(fields[7], out var dog) ? dog : 0m;
                SPD = decimal.TryParse(fields[8], out var spd) ? spd : 0m;
                LvHP = int.TryParse(fields[9], out var lvHP) ? lvHP : 0;
                LvATK = int.TryParse(fields[10], out var lvDamage) ? lvDamage : 0;
                LvDEF = int.TryParse(fields[11], out var lvDefense) ? lvDefense : 0;
                LvCRI = decimal.TryParse(fields[12], out var lvCri) ? lvCri : 0m;
                LvDOG = decimal.TryParse(fields[13], out var lvDog) ? lvDog : 0m;
                LvSPD = decimal.TryParse(fields[14], out var lvSpd) ? lvSpd : 0m;
                AttackRange = float.TryParse(fields[15], out var attackRange) ? attackRange : 1f;
                RunSpeed = int.TryParse(fields[16], out var runSpeed) ? runSpeed : 1f;
            }
        }
        
        public CharacterSheet() : base(nameof(CharacterSheet))
        {
        }
    }

    public static class CharacterSheetExtension
    {
        public static StatsMap ToStats(this CharacterSheet.Row row, int level)
        {
            var hp = row.HP;
            var atk = row.ATK;
            var def = row.DEF;
            var cri = row.CRI;
            var dog = row.DOG;
            var spd = row.SPD;
            if (level > 1)
            {
                var multiplier = level - 1;
                hp += row.LvHP * multiplier;
                atk += row.LvATK * multiplier;
                def += row.LvDEF * multiplier;
                cri += row.LvCRI * multiplier;
                dog += row.LvDOG * multiplier;
                spd += row.LvSPD * multiplier;
            }

            var stats = new StatsMap();
            stats.AddStatValue(StatType.HP, hp);
            stats.AddStatValue(StatType.ATK, atk);
            stats.AddStatValue(StatType.DEF, def);
            stats.AddStatValue(StatType.CRI, cri);
            stats.AddStatValue(StatType.DOG, dog);
            stats.AddStatValue(StatType.SPD, spd);

            return stats;
        }
    }
}
