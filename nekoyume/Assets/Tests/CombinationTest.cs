using System.Collections;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Mail;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests
{
    public class CombinationTest : PlayModeTest
    {
        [Test]
        public void GetEquipmentWithSkills()
        {
            var equipmentRow = Game.instance.TableSheets.EquipmentItemSheet.First;
            var equipment = (Equipment) ItemFactory.Create(equipmentRow, default);
            Assert.NotNull(equipment);

            var partRows = Game.instance.TableSheets.MaterialItemSheet.Select(i => i.Value)
                .Where(r => r.SkillId > 0 && r.SkillId < 200000)
                .Take(3)
                .ToList();
            foreach (var partRow in partRows)
            {
                if (Nekoyume.Action.Combination.TryGetSkill(partRow, 0, out var skill))
                    equipment.Skills.Add(skill);
            }

            Assert.AreEqual(partRows.Count, equipment.Skills.Count);

            var skillsAndPartRows = equipment.Skills.Zip(partRows, (skill, partRow) => new {skill, partRow});
            foreach (var skillAndPartRow in skillsAndPartRows)
            {
                var skill = skillAndPartRow.skill;
                var partRow = skillAndPartRow.partRow;
                Assert.AreEqual(partRow.SkillChanceMin, skill.chance);
                Assert.AreEqual(partRow.ElementalType, skill.skillRow.ElementalType);
                Assert.AreEqual(partRow.SkillDamageMin, skill.power);
            }
        }

        [UnityTest]
        public IEnumerator CombinationSuccess()
        {
            miner = new MinerFixture("combination");

            // CreateAvatar
            Widget.Find<Title>().OnClick();
            Widget.Find<Synopsis>().End();
            yield return new WaitUntil(() => Widget.Find<Login>().ready);
            Widget.Find<Login>().SlotClick(2);
            var loginDetail = Widget.Find<LoginDetail>();
            loginDetail.nameField.text = "combination";
            loginDetail.CreateClick();
            yield return new WaitUntil(() => Game.instance.agent.StagedTransactions.Any());
            var createAvatarTx = Game.instance.agent.StagedTransactions.First();
            yield return miner.CoMine(createAvatarTx);
            yield return new WaitWhile(() => States.Instance.CurrentAvatarState.Value is null);
            yield return new WaitUntil(() => Widget.Find<Login>().ready);

            // Login
            Widget.Find<Login>().SlotClick(2);
            Widget.Find<LoginDetail>().LoginClick();
            yield return new WaitUntil(() => GameObject.Find("room"));

            //Combine Consumable
            var w = Widget.Find<Combination>();
            w.Show();
            yield return new WaitUntil(() => w.isActiveAndEnabled);
            w.State.Value = Combination.StateType.CombineConsumable;
            var row = Game.instance.TableSheets.ConsumableItemRecipeSheet.Values.First();
            var rect = w.inventory.scrollerController.GetComponentInChildren<ScrollRect>();
            foreach (var material in row.MaterialItemIds)
            {
                var index = States.Instance.CurrentAvatarState.Value.inventory.Items.ToList()
                    .FindIndex(i => i.item.Data.Id == material);
                InventoryItemView item;
                while (true)
                {
                    item = w.inventory.scrollerController.GetByIndex(index);
                    if (item is null)
                    {
                        rect.GetComponent<ScrollRect>().verticalNormalizedPosition -= 0.1f;
                        yield return null;
                    }
                    else
                        break;
                }

                item.GetComponent<Button>().onClick.Invoke();
                w.inventory.Tooltip.submitButton.onClick.Invoke();
            }

            w.combineConsumable.submitButton.button.onClick.Invoke();
            yield return new WaitUntil(() => Game.instance.agent.StagedTransactions.Any());
            var tx = Game.instance.agent.StagedTransactions.First();
            yield return miner.CoMine(tx);
            Assert.AreEqual(1, States.Instance.CurrentAvatarState.Value.mailBox.OfType<CombinationMail>().Count());
        }
    }
}
