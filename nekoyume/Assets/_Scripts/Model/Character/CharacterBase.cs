using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BTAI;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.TableData;

namespace Nekoyume.Model
{
    [Serializable]
    public abstract class CharacterBase : ICloneable
    {
        public const decimal CriticalMultiplier = 1.5m;

        [NonSerialized] private Root _root;
        private Game.Skill _selectedSkill;
        private Skill _usedSkill;

        public readonly Guid Id = Guid.NewGuid();
        [NonSerialized] public readonly Simulator Simulator;

        public ElementalType atkElementType;
        public float attackRange = 1.0f;
        public ElementalType defElementType;

        public readonly Skills Skills = new Skills();
        public readonly Dictionary<int, Game.Buff> Buffs = new Dictionary<int, Game.Buff>();
        public readonly List<CharacterBase> Targets = new List<CharacterBase>();

        public CharacterSheet.Row RowData { get; }
        public SizeType SizeType => RowData?.SizeType ?? SizeType.S;
        public float RunSpeed => RowData?.RunSpeed ?? 1f;
        public CharacterStats Stats { get; }

        public int Level
        {
            get => Stats.Level;
            set => Stats.SetLevel(value);
        }

        public int HP => Stats.HP;
        public int ATK => Stats.ATK;
        public int DEF => Stats.DEF;
        public int CRI => Stats.CRI;
        public int DOG => Stats.DOG;
        public int SPD => Stats.SPD;

        public int CurrentHP
        {
            get => Stats.CurrentHP;
            set => Stats.CurrentHP = value;
        }

        private bool IsDead => CurrentHP <= 0;

        protected CharacterBase(Simulator simulator)
        {
            Simulator = simulator;
            Stats = new CharacterStats();
        }

        protected CharacterBase(Simulator simulator, int characterId, int level)
        {
            Simulator = simulator;

            if (!Game.Game.instance.TableSheets.CharacterSheet.TryGetValue(characterId, out var row))
                throw new SheetRowNotFoundException("CharacterSheet", characterId.ToString());

            RowData = row;
            Stats = new CharacterStats(RowData, level);

            atkElementType = RowData.ElementalType;
            attackRange = RowData.AttackRange;
            defElementType = RowData.ElementalType;
            CurrentHP = HP;
        }

        protected CharacterBase(CharacterBase value)
        {
            _root = value._root;
            _selectedSkill = value._selectedSkill;
            Id = value.Id;
            Simulator = value.Simulator;
            atkElementType = value.atkElementType;
            attackRange = value.attackRange;
            defElementType = value.defElementType;
            // 스킬은 변하지 않는다는 가정 하에 얕은 복사.
            Skills = value.Skills;
            // 버프는 컨테이너도 옮기고,
            Buffs = new Dictionary<int, Game.Buff>();
            foreach (var pair in value.Buffs)
            {
                // 깊은 복사까지 꼭.
                Buffs.Add(pair.Key, (Game.Buff) pair.Value.Clone());
            }

            // 타갯은 컨테이너만 옮기기.
            Targets = new List<CharacterBase>(value.Targets);
            // 캐릭터 테이블 데이타는 변하지 않는다는 가정 하에 얕은 복사.
            RowData = value.RowData;
            Stats = (CharacterStats) value.Stats.Clone();
        }

        public abstract object Clone();

        #region Behaviour Tree

        public void InitAI()
        {
            SetSkill();

            _root = new Root();
            _root.OpenBranch(
                BT.Selector().OpenBranch(
                    BT.If(IsAlive).OpenBranch(
                        BT.Sequence().OpenBranch(
                            BT.Call(BeginningOfTurn),
                            BT.Call(ReduceDurationOfBuffs),
                            BT.Call(SelectSkill),
                            BT.Call(UseSkill),
                            BT.Call(RemoveBuffs)
                        )
                    ),
                    BT.Terminate()
                )
            );
        }

        public void Tick()
        {
            _root.Tick();
        }

        private void BeginningOfTurn()
        {
            _selectedSkill = null;
            _usedSkill = null;
        }

        private void ReduceDurationOfBuffs()
        {
            // 자신의 기존 버프 턴 조절.
            foreach (var pair in Buffs)
            {
                pair.Value.remainedDuration--;
            }
        }

        private void SelectSkill()
        {
            _selectedSkill = Skills.Select(Simulator.Random);
        }

        private void UseSkill()
        {
            // 스킬 사용.
            _usedSkill = _selectedSkill.Use(this);
            Simulator.Log.Add(_usedSkill);

            foreach (var info in _usedSkill.SkillInfos)
            {
                if (info.Target.IsDead)
                {
                    var target = Targets.First(i => i.Id == info.Target.Id);
                    target.Die();
                }
            }
        }

        private void RemoveBuffs()
        {
            var isDirtyMySelf = false;

            // 자신의 버프 제거.
            var keyList = Buffs.Keys.ToList();
            foreach (var key in keyList)
            {
                var buff = Buffs[key];
                if (buff.remainedDuration > 0)
                    continue;

                Buffs.Remove(key);
                isDirtyMySelf = true;
            }

            if (!isDirtyMySelf)
                return;

            // 버프를 상태에 반영.
            Stats.SetBuffs(Buffs.Values, true);
            Simulator.Log.Add(new RemoveBuffs((CharacterBase) Clone()));
        }

        #endregion

        #region Buff

        public void AddBuff(Game.Buff buff, bool updateImmediate = false)
        {
            if (Buffs.TryGetValue(buff.RowData.GroupId, out var outBuff) &&
                outBuff.RowData.Id > buff.RowData.Id)
                return;

            var clone = (Game.Buff) buff.Clone();
            Buffs[buff.RowData.GroupId] = clone;
            Stats.AddBuff(clone, updateImmediate);
        }

        #endregion

        public bool IsCritical()
        {
            var chance = Simulator.Random.Next(0, 100);
            return chance < CRI;
        }

        private bool IsAlive()
        {
            return !IsDead;
        }

        public void Die()
        {
            OnDead();
        }

        protected virtual void OnDead()
        {
            var dead = new Dead((CharacterBase) Clone());
            Simulator.Log.Add(dead);
        }

        public void Heal(int heal)
        {
            var current = CurrentHP;
            CurrentHP = Math.Min(heal + current, HP);
        }

        protected virtual void SetSkill()
        {
            if (!Game.Game.instance.TableSheets.SkillSheet.TryGetValue(100000, out var skillRow))
            {
                throw new KeyNotFoundException("100000");
            }

            var attack = SkillFactory.Get(skillRow, ATK, 1m);
            Skills.Add(attack);
        }

        public bool GetChance(int chance)
        {
            return chance > Simulator.Random.Next(0, 100);
        }
    }

    public class InformationFieldAttribute : Attribute
    {
    }

    [Serializable]
    public class Skills : IEnumerable<Game.Skill>
    {
        private readonly List<Game.Skill> _skills = new List<Game.Skill>();

        public void Add(Game.Skill s)
        {
            if (s is null)
            {
                return;
            }

            _skills.Add(s);
        }

        public void Clear()
        {
            _skills.Clear();
        }

        public IEnumerator<Game.Skill> GetEnumerator()
        {
            return _skills.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Game.Skill Select(IRandom random)
        {
            var selected = _skills
                .Select(skill => new {skill, chance = random.Next(0, 100000) * 0.00001m})
                .Where(t => t.skill.chance > t.chance)
                .Select(t => t.skill)
                .OrderBy(s => s.chance)
                .ThenBy(s => s.effect.id)
                .ToList();

            return selected[random.Next(selected.Count)];
        }
    }
}
