using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State.Modifiers;
using Nekoyume.State.Subjects;
using Nekoyume.TableData;
using Nekoyume.UI;
using Nekoyume.UI.Module;

namespace Nekoyume.State
{
    /// <summary>
    /// This is a static class that collects the patterns of using the `Add` and `Remove` functions of `LocalStateSettings`.
    /// </summary>
    public static class LocalLayerModifier
    {
        #region Agent, Avatar / Currency

        /// <summary>
        /// Modify the agent's gold.
        /// </summary>
        /// <param name="agentAddress"></param>
        /// <param name="gold"></param>
        public static void ModifyAgentGold(Address agentAddress, FungibleAssetValue gold)
        {
            if (gold.Sign == 0)
            {
                return;
            }

            var modifier = new AgentGoldModifier(gold);
            LocalLayer.Instance.Add(agentAddress, modifier);

            //FIXME Avoid LocalLayer duplicate modify gold.
            var state = new GoldBalanceState(agentAddress, Game.Game.instance.Agent.GetBalance(agentAddress, gold.Currency));
            if (!state.address.Equals(agentAddress))
            {
                return;
            }

            States.Instance.SetGoldBalanceState(state);
        }

        public static void ModifyAgentGold(Address agentAddress, BigInteger gold)
        {
            if (gold == 0)
            {
                return;
            }

            ModifyAgentGold(agentAddress, new FungibleAssetValue(
                States.Instance.GoldBalanceState.Gold.Currency,
                gold,
                0));
        }

        /// <summary>
        /// Modify the avatar's action point.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="actionPoint"></param>
        public static void ModifyAvatarActionPoint(Address avatarAddress, int actionPoint)
        {
            if (actionPoint is 0)
            {
                return;
            }

            var modifier = new AvatarActionPointModifier(actionPoint);
            LocalLayer.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
            {
                return;
            }

            ReactiveAvatarState.ActionPoint.SetValueAndForceNotify(outAvatarState.actionPoint);
        }

        #endregion

        #region Avatar / AddItem

        public static void AddItem(Address avatarAddress, Guid guid, bool resetState = true)
        {
            var modifier = new AvatarInventoryNonFungibleItemRemover(guid);
            LocalLayer.Instance.Remove(avatarAddress, modifier);

            if (!resetState)
            {
                return;
            }

            TryResetLoadedAvatarState(avatarAddress, out _, out _);
        }

        public static void AddItem(
            Address avatarAddress,
            HashDigest<SHA256> id,
            int count,
            bool resetState = true)
        {
            if (count is 0)
            {
                return;
            }

            var modifier = new AvatarInventoryFungibleItemRemover(id, count);
            LocalLayer.Instance.Remove(avatarAddress, modifier);

            if (!resetState)
            {
                return;
            }

            TryResetLoadedAvatarState(avatarAddress, out _, out _);
        }

        public static void AddItem(
            Address avatarAddress,
            Dictionary<HashDigest<SHA256>, int> idAndCountDictionary,
            bool resetState = true)
        {
            var modifier = new AvatarInventoryFungibleItemRemover(idAndCountDictionary);
            LocalLayer.Instance.Remove(avatarAddress, modifier);

            if (!resetState)
            {
                return;
            }

            TryResetLoadedAvatarState(avatarAddress, out _, out _);
        }

        #endregion

        #region Avatar / RemoveItem

        public static void RemoveItem(Address avatarAddress, Guid guid)
        {
            var modifier = new AvatarInventoryNonFungibleItemRemover(guid);
            LocalLayer.Instance.Add(avatarAddress, modifier);
            RemoveItemInternal(avatarAddress, modifier);
        }

        public static void RemoveItem(Address avatarAddress, HashDigest<SHA256> id, int count)
        {
            if (count is 0)
            {
                return;
            }

            var modifier = new AvatarInventoryFungibleItemRemover(id, count);
            LocalLayer.Instance.Add(avatarAddress, modifier);
            RemoveItemInternal(avatarAddress, modifier);
        }

        public static void RemoveItem(
            Address avatarAddress,
            Dictionary<HashDigest<SHA256>, int> idAndCountDictionary)
        {
            var modifier = new AvatarInventoryFungibleItemRemover(idAndCountDictionary);
            LocalLayer.Instance.Add(avatarAddress, modifier);
            RemoveItemInternal(avatarAddress, modifier);
        }

        private static void RemoveItemInternal(Address avatarAddress, AvatarStateModifier modifier)
        {
            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
            {
                return;
            }

            ReactiveAvatarState.Inventory.SetValueAndForceNotify(outAvatarState.inventory);
        }

        #endregion

        #region Avatar / Mail

        /// <summary>
        /// Turns into a state where you can receive specific mail.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="mailId"></param>
        public static void AddNewAttachmentMail(Address avatarAddress, Guid mailId)
        {
            var modifier = new AvatarAttachmentMailNewSetter(mailId);
            LocalLayer.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
            {
                return;
            }

            ReactiveAvatarState.MailBox.SetValueAndForceNotify(outAvatarState.mailBox);
        }

        public static void AddNewResultAttachmentMail(
            Address avatarAddress,
            Guid mailId,
            long blockIndex
        )
        {
            var modifier = new AvatarAttachmentMailResultSetter(blockIndex, mailId);
            LocalLayer.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
            {
                return;
            }

            AddNewAttachmentMail(avatarAddress, mailId);
        }

        /// <summary>
        /// Regress the logic of the `AddNewAttachmentMail()` method.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="mailId"></param>
        /// <param name="resetState"></param>
        public static void RemoveNewAttachmentMail(
            Address avatarAddress,
            Guid mailId,
            bool resetState = true)
        {
            var modifier = new AvatarAttachmentMailNewSetter(mailId);
            LocalLayer.Instance.Remove(avatarAddress, modifier);

            if (!resetState)
            {
                return;
            }

            TryResetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out var isCurrentAvatarState);
        }

        public static void RemoveAttachmentResult(
            Address avatarAddress,
            Guid mailId,
            bool resetState = true)
        {
            var resultModifier = new AvatarAttachmentMailResultSetter(mailId);
            LocalLayer.Instance.Remove(avatarAddress, resultModifier);

            if (!resetState)
            {
                return;
            }

            TryResetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out var isCurrentAvatarState);
        }

        #endregion

        #region Avatar / Quest

        /// <summary>
        /// Changes to a state where you can receive quests.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="id"></param>
        public static void AddReceivableQuest(Address avatarAddress, int id)
        {
            var modifier = new AvatarQuestIsReceivableSetter(id);
            LocalLayer.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
            {
                return;
            }

            ReactiveAvatarState.QuestList.SetValueAndForceNotify(outAvatarState.questList);
        }

        /// <summary>
        /// Regress the logic of the `AddReceivableQuest()` method.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="id"></param>
        /// <param name="resetState"></param>
        public static void RemoveReceivableQuest(
            Address avatarAddress,
            int id,
            bool resetState = true)
        {
            var modifier = new AvatarQuestIsReceivableSetter(id);
            LocalLayer.Instance.Remove(avatarAddress, modifier);

            if (!resetState)
            {
                return;
            }

            TryResetLoadedAvatarState(avatarAddress, out _, out _);
        }

        #endregion

        #region Avatar

        /// <summary>
        /// Change the equipment's mounting status.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="itemId"></param>
        /// <param name="equip"></param>
        /// <param name="resetState"></param>
        public static void SetItemEquip(
            Address avatarAddress,
            Guid itemId,
            bool equip,
            bool resetState = true)
        {
            var modifier = new AvatarInventoryItemEquippedModifier(itemId, equip);
            LocalLayer.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);

            if (!resetState ||
                !isCurrentAvatarState)
            {
                return;
            }

            ReactiveAvatarState.Inventory.SetValueAndForceNotify(outAvatarState.inventory);
        }

        /// <summary>
        /// Change the daily reward acquisition block index of the avatar.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="blockCount"></param>
        public static void IncreaseAvatarDailyRewardReceivedIndex(Address avatarAddress, long blockCount)
        {
            var modifier = new AvatarDailyRewardReceivedIndexModifier(blockCount);
            LocalLayer.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            if (!isCurrentAvatarState)
            {
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);
            ReactiveAvatarState.DailyRewardReceivedIndex.SetValueAndForceNotify(
                outAvatarState.dailyRewardReceivedIndex);
        }

        public static void ModifyAvatarItemRequiredIndex(
            Address avatarAddress,
            Guid itemId,
            long blockIndex
        )
        {
            var modifier = new AvatarItemRequiredIndexModifier(blockIndex, itemId);
            LocalLayer.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
            {
                return;
            }

            ReactiveAvatarState.DailyRewardReceivedIndex.SetValueAndForceNotify(
                outAvatarState.dailyRewardReceivedIndex);
        }

        public static void RemoveAvatarItemRequiredIndex(Address avatarAddress, Guid itemId)
        {
            var modifier = new AvatarItemRequiredIndexModifier(itemId);
            LocalLayer.Instance.Remove(avatarAddress, modifier);
        }

        public static void AddMaterial(Address avatarAddress, HashDigest<SHA256> itemId, int count, bool resetState)
        {
            if (count is 0)
            {
                return;
            }

            var modifier = new AvatarInventoryMaterialModifier(
                new Dictionary<HashDigest<SHA256>, int>
                {
                    [itemId] = count,
                }
            );

            LocalLayer.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
            {
                return;
            }

            if (!resetState)
            {
                return;
            }

            TryResetLoadedAvatarState(avatarAddress, out _, out _);

        }

        public static void RemoveMaterial(Address avatarAddress, HashDigest<SHA256> itemId, int count, bool resetState)
        {
            if (count is 0)
            {
                return;
            }

            var modifier = new AvatarInventoryMaterialModifier(
                new Dictionary<HashDigest<SHA256>, int>
                {
                    [itemId] = count,
                }
            );

            LocalLayer.Instance.Remove(avatarAddress, modifier);

            if (!resetState)
            {
                return;
            }

            TryResetLoadedAvatarState(avatarAddress, out _, out _);
        }

        public static void AddWorld(Address avatarAddress, int worldId)
        {
            var modifier = new AvatarWorldInformationAddWorldModifier(worldId);
            if (avatarAddress.Equals(States.Instance.CurrentAvatarState.address))
            {
                modifier.Modify(States.Instance.CurrentAvatarState);
            }

            LocalLayer.Instance.Add(avatarAddress, modifier);
        }

        #endregion

        #region WeeklyArena

        /// <summary>
        /// Activates the one corresponding to the address of the current avatar state among the `ArenaInfo` included in the weekly arena state you are viewing.
        /// </summary>
        /// <param name="characterSheet"></param>
        /// <param name="addArenaInfoIfNotContained"></param>
        public static void AddWeeklyArenaInfoActivator(
            CharacterSheet characterSheet,
            bool addArenaInfoIfNotContained = true)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var avatarAddress = avatarState.address;
            var weeklyArenaState = States.Instance.WeeklyArenaState;
            var weeklyArenaAddress = weeklyArenaState.address;

            if (addArenaInfoIfNotContained &&
                !weeklyArenaState.ContainsKey(avatarAddress))
            {
                weeklyArenaState.Set(avatarState, characterSheet);
            }

            var modifier = new WeeklyArenaInfoActivator(avatarAddress);
            LocalLayer.Instance.Add(weeklyArenaAddress, modifier);
            weeklyArenaState = modifier.Modify(weeklyArenaState);
            WeeklyArenaStateSubject.WeeklyArenaState.OnNext(weeklyArenaState);
        }

        /// <summary>
        /// Regress the logic of the `AddWeeklyArenaInfoActivator()` method.
        /// </summary>
        /// <param name="weeklyArenaAddress"></param>
        /// <param name="avatarAddress"></param>
        public static void RemoveWeeklyArenaInfoActivator(
            Address weeklyArenaAddress,
            Address avatarAddress)
        {
            var modifier = new WeeklyArenaInfoActivator(avatarAddress);
            LocalLayer.Instance.Remove(weeklyArenaAddress, modifier);

            var state = States.Instance.WeeklyArenaState;
            if (!state.address.Equals(weeklyArenaAddress))
            {
                return;
            }

            state = modifier.Modify(state);
            WeeklyArenaStateSubject.WeeklyArenaState.OnNext(state);
        }

        #endregion

        #region Workshop

        public static void ModifyCombinationSlotEquipment(
            TableSheets tableSheets,
            EquipmentItemRecipeSheet.Row row,
            CombinationPanel panel,
            int slotIndex,
            int? subRecipeId
        )
        {
            var slotAddress = States.Instance.CurrentAvatarState.address.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    slotIndex
                )
            );

            ModifyCombinationSlotEquipment(tableSheets, row, panel, slotAddress, subRecipeId);
        }

        public static void ModifyCombinationSlotEquipment(
            TableSheets tableSheets,
            EquipmentItemRecipeSheet.Row row,
            CombinationPanel panel,
            Address slotAddress,
            int? subRecipeId
        )
        {
            // When the layer is covered, additionally set the block height to prevent state updates until the actual state comes in.
            var blockIndex = Game.Game.instance.Agent.BlockIndex + 100;
            var requiredBlockIndex = row.RequiredBlockIndex + blockIndex;
            if (subRecipeId.HasValue)
            {
                var subRow =
                    tableSheets.EquipmentItemSubRecipeSheet.Values.First(r => r.Id == subRecipeId);
                requiredBlockIndex += subRow.RequiredBlockIndex;
            }

            var equipRow =
                tableSheets.EquipmentItemSheet.Values.First(i => i.Id == row.ResultEquipmentId);
            var equipment = ItemFactory.CreateItemUsable(equipRow, Guid.Empty, requiredBlockIndex);
            var materials = new Dictionary<Material, int>();
            foreach (var (material, count) in panel.materialPanel.MaterialList)
            {
                materials[material] = count;
            }

            var result = new CombinationConsumable.ResultModel
            {
                // id: When applying the local layer for the first time, if the id is the default, the notification is not applied.
                id = Guid.NewGuid(),
                actionPoint = panel.CostAP,
                gold = panel.CostNCG,
                materials = materials,
                itemUsable = equipment,
                recipeId = row.Id,
                subRecipeId = subRecipeId,
                itemType = ItemType.Equipment,
            };
            var modifier = new CombinationSlotBlockIndexAndResultModifier(result, blockIndex, requiredBlockIndex);
            var slotState = States.Instance.CombinationSlotStates[slotAddress];
            LocalLayer.Instance.Set(slotState.address, modifier);
            States.Instance.CombinationSlotStates[slotAddress] = modifier.Modify(slotState);
            CombinationSlotStateSubject.OnNext(slotState);
        }

        public static void ModifyCombinationSlotConsumable(
            TableSheets tableSheets,
            ICombinationPanel panel,
            ConsumableItemRecipeSheet.Row recipeRow,
            int slotIndex
        )
        {
            var slotAddress = States.Instance.CurrentAvatarState.address.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    slotIndex
                )
            );

            ModifyCombinationSlotConsumable(tableSheets, panel, recipeRow, slotAddress);
        }

        public static void ModifyCombinationSlotConsumable(
            TableSheets tableSheets,
            ICombinationPanel panel,
            ConsumableItemRecipeSheet.Row recipeRow,
            Address slotAddress
        )
        {
            // When the layer is covered, additionally set the block height to prevent state updates until the actual state comes in.
            var blockIndex = Game.Game.instance.Agent.BlockIndex + 100;
            var requiredBlockIndex = blockIndex + recipeRow.RequiredBlockIndex;
            var consumableRow = tableSheets.ConsumableItemSheet.Values.First(i =>
                i.Id == recipeRow.ResultConsumableItemId);
            var consumable = ItemFactory.CreateItemUsable(
                consumableRow,
                Guid.Empty,
                requiredBlockIndex);
            var materials = new Dictionary<Material, int>();
            foreach (var materialInfo in recipeRow.Materials)
            {
                var materialRow = tableSheets.MaterialItemSheet.Values.First(r => r.Id == materialInfo.Id);
                var material = ItemFactory.CreateMaterial(materialRow);
                materials[material] = materialInfo.Count;
            }

            var result = new CombinationConsumable.ResultModel
            {
                actionPoint = panel.CostAP,
                gold = panel.CostNCG,
                materials = materials,
                itemUsable = consumable,
                recipeId = recipeRow.Id,
                itemType = ItemType.Consumable,
            };
            var modifier = new CombinationSlotBlockIndexAndResultModifier(result, blockIndex, requiredBlockIndex);
            var slotState = States.Instance.CombinationSlotStates[slotAddress];
            LocalLayer.Instance.Set(slotState.address, modifier);
            States.Instance.CombinationSlotStates[slotAddress] = modifier.Modify(slotState);
            CombinationSlotStateSubject.OnNext(slotState);
        }

        public static void ModifyCombinationSlotItemEnhancement(
            Guid baseMaterialGuid,
            Guid guid,
            int slotIndex
        )
        {
            var slotAddress = States.Instance.CurrentAvatarState.address.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    slotIndex
                )
            );

            ModifyCombinationSlotItemEnhancement(baseMaterialGuid, guid, slotAddress);
        }

        public static void ModifyCombinationSlotItemEnhancement(
            Guid baseMaterialGuid,
            Guid otherMaterialGuid,
            Address slotAddress
        )
        {

            // When the layer is covered, additionally set the block height to prevent state updates until the actual state comes in.
            var blockIndex = Game.Game.instance.Agent.BlockIndex + 100;
            var requiredBlockIndex = blockIndex + 1;

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var avatarState = new AvatarState(
                (Bencodex.Types.Dictionary) Game.Game.instance.Agent.GetState(avatarAddress));

            if (!avatarState.inventory.TryGetNonFungibleItem(baseMaterialGuid, out ItemUsable item))
            {
                return;
            }

            if (!(item is Equipment equipment))
            {
                return;
            }

            equipment.LevelUp();
            equipment.Update(requiredBlockIndex);

            var enhancementRow = Game.Game.instance.TableSheets
                .EnhancementCostSheet.Values
                .FirstOrDefault(x => x.Grade == equipment.Grade && x.Level == equipment.level);

            var result = new ItemEnhancement.ResultModel
            {
                // id: When applying the local layer for the first time, if the id is the default, the notification is not applied.
                id = Guid.NewGuid(),
                actionPoint = 0,
                gold = enhancementRow.Cost,
                materialItemIdList = new[] { otherMaterialGuid },
                itemUsable = equipment,
            };

            var modifier = new CombinationSlotBlockIndexAndResultModifier(result, blockIndex, requiredBlockIndex);
            var slotState = States.Instance.CombinationSlotStates[slotAddress];
            LocalLayer.Instance.Set(slotState.address, modifier);
            States.Instance.CombinationSlotStates[slotAddress] = modifier.Modify(slotState);
            CombinationSlotStateSubject.OnNext(slotState);
        }

        public static void UnlockCombinationSlot(int slotIndex, long blockIndex)
        {
            var slotAddress = States.Instance.CurrentAvatarState.address.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    slotIndex
                )
            );

            UnlockCombinationSlot(slotAddress, blockIndex);
        }

        private static void UnlockCombinationSlot(Address slotAddress, long blockIndex)
        {
            var slotState = States.Instance.CombinationSlotStates[slotAddress];
            var modifier = new CombinationSlotBlockIndexModifier(blockIndex);
            LocalLayer.Instance.Set(slotState.address, modifier);
            States.Instance.CombinationSlotStates[slotAddress] = modifier.Modify(slotState);
            CombinationSlotStateSubject.OnNext(slotState);
        }

        public static void ResetCombinationSlot(CombinationSlotState slot)
        {
            LocalLayer.Instance
                .ResetCombinationSlotModifiers<CombinationSlotBlockIndexModifier>(
                    slot.address);
            LocalLayer.Instance
                .ResetCombinationSlotModifiers<CombinationSlotBlockIndexAndResultModifier>(
                    slot.address);
        }

        #endregion

        /// <summary>
        /// Returns the same object as `avatarAddress` and its key among the avatar states included in `States.AvatarStates`.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="outAvatarState"></param>
        /// <param name="outKey"></param>
        /// <param name="isCurrentAvatarState"></param>
        /// <returns></returns>
        private static bool TryGetLoadedAvatarState(
            Address avatarAddress,
            out AvatarState outAvatarState,
            out int outKey,
            out bool isCurrentAvatarState)
        {
            var agentState = States.Instance.AgentState;
            if (agentState is null ||
                !agentState.avatarAddresses.ContainsValue(avatarAddress))
            {
                outAvatarState = null;
                outKey = -1;
                isCurrentAvatarState = false;
                return false;
            }

            foreach (var pair in States.Instance.AvatarStates)
            {
                if (!pair.Value.address.Equals(avatarAddress))
                {
                    continue;
                }

                outAvatarState = pair.Value;
                outKey = pair.Key;
                isCurrentAvatarState = outKey.Equals(States.Instance.CurrentAvatarKey);
                return true;
            }

            outAvatarState = null;
            outKey = -1;
            isCurrentAvatarState = false;
            return false;
        }

        /// <summary>
        /// Use the `States.AddOrReplaceAvatarState(address, key, initializeReactiveState)` function to newly allocate the already loaded avatar state.
        /// Therefore, there is no need to additionally update `ReactiveAvatarState` after using this function.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="outAvatarState"></param>
        /// <param name="isCurrentAvatarState"></param>
        private static bool TryResetLoadedAvatarState(
            Address avatarAddress,
            out AvatarState outAvatarState,
            out bool isCurrentAvatarState)
        {
            if (!TryGetLoadedAvatarState(avatarAddress, out outAvatarState, out var outKey,
                out isCurrentAvatarState))
            {
                return false;
            }

            outAvatarState = States.Instance.AddOrReplaceAvatarState(
                avatarAddress,
                outKey,
                isCurrentAvatarState);
            return true;
        }
    }
}
