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
    /// `LocalStateSettings`의 `Add`와 `Remove`함수를 사용하는 정적 클래스이다.
    /// `States.AgentState`와 `States.AvatarStates`, `States.CurrentAvatarState`를 업데이트 한다.
    /// `ReactiveAgentState`와 `ReactiveAvatarState`를 업데이트 한다.
    /// 반복되는 로직을 모아둔다.
    /// </summary>
    public static class LocalStateModifier
    {
        #region Agent, Avatar / Currency

        /// <summary>
        /// 에이전트의 골드를 증가시킨다.(휘발성)
        /// </summary>
        /// <param name="agentAddress"></param>
        /// <param name="gold">더할 NCG. 음수일 경우 감소시킨다.</param>
        // FIXME: 이름이 헷갈리니 IncrementAgentGold() 정도로 이름을 바꾸는 게 좋겠습니다.
        // (현재는 이름만 보면 더하는 게 아니라 그냥 덮어씌우는 것처럼 여겨짐.)
        public static void ModifyAgentGold(Address agentAddress, FungibleAssetValue gold)
        {
            if (gold.Sign == 0)
            {
                return;
            }

            var modifier = new AgentGoldModifier(gold);
            LocalStateSettings.Instance.Add(agentAddress, modifier);

            var state = States.Instance.GoldBalanceState;
            if (state is null || !state.address.Equals(agentAddress))
            {
                return;
            }

            // NOTE: Reassignment is not required yet.
            state = modifier.Modify(state);
            ReactiveAgentState.Gold.SetValueAndForceNotify(state.Gold);
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
        /// 아바타의 행동력을 증가시킨다.(휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="actionPoint">더할 행동력. 음수일 경우 감소시킨다.</param>
        // FIXME: 이름이 헷갈리니 IncrementAvatarActionPoint() 정도로 이름을 바꾸는 게 좋겠습니다.
        // (현재는 이름만 보면 더하는 게 아니라 그냥 덮어씌우는 것처럼 여겨짐.)
        public static void ModifyAvatarActionPoint(Address avatarAddress, int actionPoint)
        {
            if (actionPoint is 0)
            {
                return;
            }

            var modifier = new AvatarActionPointModifier(actionPoint);
            LocalStateSettings.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            // NOTE: Reassignment is not required yet.
            outAvatarState = modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
            {
                return;
            }

            ReactiveAvatarState.ActionPoint.SetValueAndForceNotify(outAvatarState.actionPoint);
        }

        #endregion

        #region Avatar / AddItem

        /// <summary>
        /// (휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="guid"></param>
        /// <param name="resetState"></param>
        public static void AddItem(Address avatarAddress, Guid guid, bool resetState = true)
        {
            var modifier = new AvatarInventoryNonFungibleItemRemover(guid);
            LocalStateSettings.Instance.Remove(avatarAddress, modifier);

            if (!resetState)
            {
                return;
            }

            TryResetLoadedAvatarState(avatarAddress, out _, out _);
        }

        /// <summary>
        /// (휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="id"></param>
        /// <param name="count"></param>
        /// <param name="resetState"></param>
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
            LocalStateSettings.Instance.Remove(avatarAddress, modifier);

            if (!resetState)
            {
                return;
            }

            TryResetLoadedAvatarState(avatarAddress, out _, out _);
        }

        /// <summary>
        /// (휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="idAndCountDictionary"></param>
        /// <param name="resetState"></param>
        public static void AddItem(
            Address avatarAddress,
            Dictionary<HashDigest<SHA256>, int> idAndCountDictionary,
            bool resetState = true)
        {
            var modifier = new AvatarInventoryFungibleItemRemover(idAndCountDictionary);
            LocalStateSettings.Instance.Remove(avatarAddress, modifier);

            if (!resetState)
            {
                return;
            }

            TryResetLoadedAvatarState(avatarAddress, out _, out _);
        }

        #endregion

        #region Avatar / RemoveItem

        /// <summary>
        /// (휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="guid"></param>
        public static void RemoveItem(Address avatarAddress, Guid guid)
        {
            var modifier = new AvatarInventoryNonFungibleItemRemover(guid);
            LocalStateSettings.Instance.Add(avatarAddress, modifier);
            RemoveItemInternal(avatarAddress, modifier);
        }

        /// <summary>
        /// (휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="id"></param>
        /// <param name="count"></param>
        public static void RemoveItem(Address avatarAddress, HashDigest<SHA256> id, int count)
        {
            if (count is 0)
            {
                return;
            }

            var modifier = new AvatarInventoryFungibleItemRemover(id, count);
            LocalStateSettings.Instance.Add(avatarAddress, modifier);
            RemoveItemInternal(avatarAddress, modifier);
        }

        /// <summary>
        /// (휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="idAndCountDictionary"></param>
        public static void RemoveItem(
            Address avatarAddress,
            Dictionary<HashDigest<SHA256>, int> idAndCountDictionary)
        {
            var modifier = new AvatarInventoryFungibleItemRemover(idAndCountDictionary);
            LocalStateSettings.Instance.Add(avatarAddress, modifier);
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

            // NOTE: Reassignment is not required yet.
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
        /// `avatarAddress`에 해당하는 아바타 상태의 `MailBox` 안에 `AttachmentMail` 리스트 중, `guid`를 보상으로 갖고 있는 메일을 신규 처리한다.(비휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="mailId"></param>
        public static void AddNewAttachmentMail(Address avatarAddress, Guid mailId)
        {
            var modifier = new AvatarAttachmentMailNewSetter(mailId);
            LocalStateSettings.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            // NOTE: Reassignment is not required yet.
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
            LocalStateSettings.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            // NOTE: Reassignment is not required yet.
            outAvatarState = modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
            {
                return;
            }

            AddNewAttachmentMail(avatarAddress, mailId);
        }

        /// <summary>
        /// `AddNewAttachmentMail()` 메서드 로직을 회귀한다.(비휘발성)
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
            LocalStateSettings.Instance.Remove(avatarAddress, modifier);

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
            LocalStateSettings.Instance.Remove(avatarAddress, resultModifier);

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
        /// `avatarAddress`에 해당하는 아바타 상태의 `QuestList` 안의 퀘스트 중, 매개변수의 `id`를 가진 퀘스트를 신규 처리한다.(비휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="id"></param>
        public static void AddReceivableQuest(Address avatarAddress, int id)
        {
            var modifier = new AvatarQuestIsReceivableSetter(id);
            LocalStateSettings.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            // NOTE: Reassignment is not required yet.
            outAvatarState = modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
            {
                return;
            }

            ReactiveAvatarState.QuestList.SetValueAndForceNotify(outAvatarState.questList);
        }

        /// <summary>
        /// `avatarAddress`에 해당하는 아바타 상태의 `QuestList` 안의 퀘스트 중, 매개변수의 `id`를 가진 퀘스트의 신규 처리를 회귀한다.(비휘발성)
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
            LocalStateSettings.Instance.Remove(avatarAddress, modifier);

            if (!resetState)
            {
                return;
            }

            TryResetLoadedAvatarState(avatarAddress, out _, out _);
        }

        #endregion

        #region Avatar

        /// <summary>
        /// `avatarAddress`에 해당하는 아바타 상태의 `Inventory` 안의 `Costume` 중,
        /// 매개변수의 `id`를 가진 `Costume`의 `equipped`를 매개변수 `equip`으로 설정한다.(비휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="id"></param>
        /// <param name="equip"></param>
        /// <param name="resetState"></param>
        public static void SetCostumeEquip(
            Address avatarAddress,
            int id,
            bool equip,
            bool resetState = true)
        {
            var modifier = new AvatarInventoryCostumeEquippedModifier(id, equip);
            LocalStateSettings.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            // NOTE: Reassignment is not required yet.
            outAvatarState = modifier.Modify(outAvatarState);

            if (!resetState ||
                !isCurrentAvatarState)
            {
                return;
            }

            ReactiveAvatarState.Inventory.SetValueAndForceNotify(outAvatarState.inventory);
        }

        /// <summary>
        /// `avatarAddress`에 해당하는 아바타 상태의 `Inventory` 안의 `Equipment` 중,
        /// 매개변수의 `itemId`를 가진 `Equipment`의 `equipped`를 매개변수 `equip`으로 설정한다.(비휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="itemId"></param>
        /// <param name="equip"></param>
        /// <param name="resetState"></param>
        public static void SetEquipmentEquip(
            Address avatarAddress,
            Guid itemId,
            bool equip,
            bool resetState = true)
        {
            var modifier = new AvatarInventoryEquipmentEquippedModifier(itemId, equip);
            LocalStateSettings.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            // NOTE: Reassignment is not required yet.
            outAvatarState = modifier.Modify(outAvatarState);

            if (!resetState ||
                !isCurrentAvatarState)
            {
                return;
            }

            ReactiveAvatarState.Inventory.SetValueAndForceNotify(outAvatarState.inventory);
        }

        /// <summary>
        /// 아바타의 데일리 리워드 획득 블록 인덱스를 변경한다.(휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="blockCount"></param>
        public static void IncreaseAvatarDailyRewardReceivedIndex(Address avatarAddress, long blockCount)
        {
            var modifier = new AvatarDailyRewardReceivedIndexModifier(blockCount);
            LocalStateSettings.Instance.Add(avatarAddress, modifier);

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

            // NOTE: Reassignment is not required yet.
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
            LocalStateSettings.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            // NOTE: Reassignment is not required yet.
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
            LocalStateSettings.Instance.Remove(avatarAddress, modifier);
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

            LocalStateSettings.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            // NOTE: Reassignment is not required yet.
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

            LocalStateSettings.Instance.Remove(avatarAddress, modifier);

            if (!resetState)
            {
                return;
            }

            TryResetLoadedAvatarState(avatarAddress, out _, out _);
        }

        #endregion

        #region WeeklyArena

        /// <summary>
        /// 현재 바라보고 있는 주간 아레나 상태가 포함하고 있는 `ArenaInfo` 중 현재 아바타 상태의 주소에 해당하는 것을 활성화 시킨다.(휘발)
        /// </summary>
        /// <param name="characterSheet"></param>
        /// <param name="addArenaInfoIfNotContained">주간 아레나 상태에 현재 아바타 정보가 없으면 넣어준다.</param>
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
            LocalStateSettings.Instance.Add(weeklyArenaAddress, modifier);
            // NOTE: Reassignment is not required yet.
            weeklyArenaState = modifier.Modify(weeklyArenaState);
            WeeklyArenaStateSubject.WeeklyArenaState.OnNext(weeklyArenaState);
        }

        /// <summary>
        /// `AddWeeklyArenaInfoActivator()` 메서드 로직을 회귀한다.(휘발)
        /// </summary>
        /// <param name="weeklyArenaAddress"></param>
        /// <param name="avatarAddress"></param>
        public static void RemoveWeeklyArenaInfoActivator(
            Address weeklyArenaAddress,
            Address avatarAddress)
        {
            var modifier = new WeeklyArenaInfoActivator(avatarAddress);
            LocalStateSettings.Instance.Remove(weeklyArenaAddress, modifier);

            var state = States.Instance.WeeklyArenaState;
            if (!state.address.Equals(weeklyArenaAddress))
            {
                return;
            }

            // NOTE: Reassignment is not required yet.
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
            // 레이어가 씌워진 상태에선 실제 상태가 들어오기전까지 상태업데이트를 막아두기 위해 블록높이를 추가로 설정
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
                // id: 처음 로컬 레이어를 적용할 때 id가 default면 노티가 적용되지 않기 때문에 임시로 넣습니다.
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
            LocalStateSettings.Instance.Set(slotState.address, modifier);
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
            // 레이어가 씌워진 상태에선 실제 상태가 들어오기전까지 상태업데이트를 막아두기 위해 블록높이를 추가로 설정
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
            LocalStateSettings.Instance.Set(slotState.address, modifier);
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
            
            // 레이어가 씌워진 상태에선 실제 상태가 들어오기전까지 상태업데이트를 막아두기 위해 블록높이를 추가로 설정
            var blockIndex = Game.Game.instance.Agent.BlockIndex + 100;
            var requiredBlockIndex = blockIndex + 1;

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var avatarState = new AvatarState(
                (Bencodex.Types.Dictionary) Game.Game.instance.Agent.GetState(avatarAddress));

            if (avatarState.inventory.TryGetNonFungibleItem(baseMaterialGuid, out ItemUsable item))
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
                // id: 처음 로컬 레이어를 적용할 때 id가 default면 노티가 적용되지 않기 때문에 임시로 넣습니다.
                id = Guid.NewGuid(),
                actionPoint = 0,
                gold = enhancementRow.Cost,
                materialItemIdList = new[] { otherMaterialGuid },
                itemUsable = equipment,
            };
            var modifier = new CombinationSlotBlockIndexAndResultModifier(result, blockIndex, requiredBlockIndex);
            var slotState = States.Instance.CombinationSlotStates[slotAddress];
            LocalStateSettings.Instance.Set(slotState.address, modifier);
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
            LocalStateSettings.Instance.Set(slotState.address, modifier);
            // NOTE: Reassignment is not required yet.
            States.Instance.CombinationSlotStates[slotAddress] = modifier.Modify(slotState);
            CombinationSlotStateSubject.OnNext(slotState);
        }

        public static void ResetCombinationSlot(CombinationSlotState slot)
        {
            LocalStateSettings.Instance
                .ResetCombinationSlotModifiers<CombinationSlotBlockIndexModifier>(
                    slot.address);
            LocalStateSettings.Instance
                .ResetCombinationSlotModifiers<CombinationSlotBlockIndexAndResultModifier>(
                    slot.address);
        }

        #endregion

        /// <summary>
        /// `States.AvatarStates`가 포함하고 있는 아바타 상태 중에 `avatarAddress`와 같은 객체와 그 키를 반환한다.
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
        /// `States.AddOrReplaceAvatarState(address, key, initializeReactiveState)`함수를 사용해서
        /// 이미 로드되어 있는 아바타 상태를 새로 할당한다.
        /// 따라서 이 함수를 사용한 후에 `ReactiveAvatarState`를 추가로 갱신할 필요가 없다.
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
