using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using Material = Nekoyume.Game.Item.Material;

namespace Nekoyume.UI.Model
{
    public class Inventory : IDisposable
    {
        public readonly ReactiveProperty<ItemType> State = new ReactiveProperty<ItemType>(ItemType.Equipment);
        public readonly ReactiveCollection<InventoryItem> Consumables = new ReactiveCollection<InventoryItem>();
        public readonly ReactiveCollection<InventoryItem> Equipments = new ReactiveCollection<InventoryItem>();
        public readonly ReactiveCollection<InventoryItem> Materials = new ReactiveCollection<InventoryItem>();

        public readonly ReactiveProperty<InventoryItemView> SelectedItemView =
            new ReactiveProperty<InventoryItemView>();

        public readonly ReactiveProperty<InventoryItem> SelectedItemViewModel =
            new ReactiveProperty<InventoryItem>();

        public readonly ReactiveProperty<Func<InventoryItem, bool>> DimmedFunc =
            new ReactiveProperty<Func<InventoryItem, bool>>();

        public readonly ReactiveProperty<Func<InventoryItem, bool>> EffectEnabledFunc =
            new ReactiveProperty<Func<InventoryItem, bool>>();
        
        public readonly ReactiveProperty<Func<InventoryItem, bool>> EquippedEnabledFunc =
            new ReactiveProperty<Func<InventoryItem, bool>>();

        public readonly Subject<InventoryItemView> OnRightClickItemView = new Subject<InventoryItemView>();

        public Inventory(ItemType stateType = ItemType.Equipment)
        {
            State.Value = stateType;
            DimmedFunc.Value = DefaultDimmedFunc;

            State.Subscribe(SubscribeState);
            DimmedFunc.Subscribe(SubscribeDimmedFunc);
            EffectEnabledFunc.Subscribe(SubscribeEffectEnabledFunc);
            EquippedEnabledFunc.Subscribe(SubscribeEquippedEnabledFunc);
        }

        public void Dispose()
        {
            State.Dispose();
            Consumables.Dispose();
            Equipments.Dispose();
            Materials.Dispose();
            SelectedItemView.Dispose();
            SelectedItemViewModel.Dispose();
            DimmedFunc.Dispose();
            EquippedEnabledFunc.Dispose();
            OnRightClickItemView.Dispose();
        }

        public void ResetItems(Game.Item.Inventory inventory)
        {
            RemoveItemsAll();

            if (inventory is null)
                return;

            foreach (var item in inventory.Items)
            {
                AddItem(item.item, item.count);
            }

            State.SetValueAndForceNotify(State.Value);
        }

        #region Inventory Item Pool

        private InventoryItem CreateInventoryItem(ItemBase itemBase, int count)
        {
            var item = new InventoryItem(itemBase, count);
            item.Dimmed.Value = DimmedFunc.Value(item);
            item.OnClick.Subscribe(model =>
            {
                if (!(model is InventoryItem inventoryItem))
                    return;

                SubscribeItemOnClick(inventoryItem.View);
            });
            item.OnRightClick.Subscribe(model =>
            {
                if (!(model is InventoryItem inventoryItem))
                    return;

                OnRightClickItemView.OnNext(inventoryItem.View);
            });

            return item;
        }

        #endregion

        #region Add Item

        public void AddItem(ItemBase itemBase, int count = 1)
        {
            InventoryItem inventoryItem;
            switch (itemBase.Data.ItemType)
            {
                case ItemType.Consumable:
                    inventoryItem = CreateInventoryItem(itemBase, count);
                    Consumables.Add(inventoryItem);
                    break;
                case ItemType.Equipment:
                    inventoryItem = CreateInventoryItem(itemBase, count);
                    inventoryItem.EquippedEnabled.Value = ((Equipment) itemBase).equipped;
                    Equipments.Add(inventoryItem);
                    break;
                case ItemType.Material:
                    var material = (Material) itemBase;
                    if (TryGetMaterial(material, out inventoryItem))
                    {
                        inventoryItem.Count.Value += count;
                        return;
                    }

                    inventoryItem = CreateInventoryItem(itemBase, count);
                    Materials.Add(inventoryItem);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region Remove Item

        public void RemoveItem(ItemBase itemBase, int count = 1)
        {
            InventoryItem inventoryItem;
            switch (itemBase.Data.ItemType)
            {
                case ItemType.Consumable:
                    if (!TryGetConsumable((ItemUsable) itemBase, out inventoryItem))
                        return;

                    Consumables.Remove(inventoryItem);
                    break;
                case ItemType.Equipment:
                    if (!TryGetEquipment((ItemUsable) itemBase, out inventoryItem))
                        return;

                    Equipments.Remove(inventoryItem);
                    break;
                case ItemType.Material:
                    if (!TryGetMaterial((Material) itemBase, out inventoryItem))
                        return;

                    inventoryItem.Count.Value -= count;
                    if (inventoryItem.Count.Value > 0)
                        return;

                    Materials.Remove(inventoryItem);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void RemoveItems(IEnumerable<CountEditableItem> collection)
        {
            foreach (var countEditableItem in collection)
            {
                if (countEditableItem is null)
                    continue;

                RemoveItem(countEditableItem.ItemBase.Value, countEditableItem.Count.Value);
            }
        }

        public void RemoveItemsAll()
        {
            Consumables.DisposeAllAndClear();
            Equipments.DisposeAllAndClear();
            Materials.DisposeAllAndClear();
        }

        #endregion

        #region Try Get

        public bool TryGetItem(Guid guid, out ItemUsable itemUsable)
        {
            foreach (var inventoryItem in Equipments)
            {
                if (!(inventoryItem.ItemBase.Value is Equipment equipment)
                    || !equipment.ItemId.Equals(guid))
                {
                    continue;
                }

                itemUsable = equipment;
                return true;
            }

            itemUsable = null;
            return false;
        }

        public bool TryGetEquipment(ItemUsable itemUsable, out InventoryItem inventoryItem)
        {
            if (itemUsable is null)
            {
                inventoryItem = null;
                return false;
            }

            foreach (var item in Equipments)
            {
                if (!(item.ItemBase.Value is Equipment equipment))
                {
                    continue;
                }

                if (equipment.ItemId != itemUsable.ItemId)
                {
                    continue;
                }

                inventoryItem = item;
                return true;
            }

            inventoryItem = null;
            return false;
        }

        public bool TryGetConsumable(ItemUsable itemUsable, out InventoryItem inventoryItem)
        {
            if (itemUsable is null)
            {
                inventoryItem = null;
                return false;
            }

            foreach (var item in Consumables)
            {
                if (!(item.ItemBase.Value is Consumable food))
                {
                    continue;
                }

                if (food.ItemId != itemUsable.ItemId)
                {
                    continue;
                }

                inventoryItem = item;
                return true;
            }

            inventoryItem = null;
            return false;
        }

        public bool TryGetMaterial(Material material, out InventoryItem inventoryItem)
        {
            foreach (var item in Materials)
            {
                if (item.ItemBase.Value.Data.Id != material.Data.Id)
                {
                    continue;
                }

                inventoryItem = item;
                return true;
            }

            inventoryItem = null;
            return false;
        }

        #endregion

        private void SubscribeItemOnClick(InventoryItemView view)
        {
            if (view != null &&
                view == SelectedItemView.Value)
            {
                DeselectItemView();
                return;
            }

            SelectItemView(view);
        }
        
        public void SelectItemView(InventoryItemView view)
        {
            if (view is null ||
                view.Model is null)
                return;

            DeselectItemView();

            SelectedItemView.Value = view;
            SelectedItemViewModel.Value = view.Model;
            SelectedItemViewModel.Value.Selected.Value = true;
            SetGlowedAll(false);
        }

        public void DeselectItemView()
        {
            if (SelectedItemView.Value is null ||
                SelectedItemViewModel.Value is null)
            {
                return;
            }

            SelectedItemViewModel.Value.Selected.Value = false;
            SelectedItemViewModel.Value = null;
            SelectedItemView.Value = null;
        }

        public void UpdateDimAll()
        {
            SubscribeDimmedFunc(DimmedFunc.Value);
        }
        
        public void UpdateEffectAll()
        {
            SubscribeEffectEnabledFunc(EffectEnabledFunc.Value);
        }

        #region Subscribe

        private void SubscribeState(ItemType state)
        {
            DeselectItemView();
        }

        private void SubscribeDimmedFunc(Func<InventoryItem, bool> func)
        {
            if (DimmedFunc.Value == null)
            {
                DimmedFunc.Value = DefaultDimmedFunc;
            }

            foreach (var item in Equipments)
            {
                item.Dimmed.Value = DimmedFunc.Value(item);
            }

            foreach (var item in Consumables)
            {
                item.Dimmed.Value = DimmedFunc.Value(item);
            }

            foreach (var item in Materials)
            {
                item.Dimmed.Value = DimmedFunc.Value(item);
            }
        }
        
        private void SubscribeEffectEnabledFunc(Func<InventoryItem, bool> func)
        {
            if (EffectEnabledFunc.Value == null)
            {
                EffectEnabledFunc.Value = DefaultCoveredFunc;
            }

            foreach (var item in Equipments)
            {
                item.EffectEnabled.Value = EffectEnabledFunc.Value(item);
            }

            foreach (var item in Consumables)
            {
                item.EffectEnabled.Value = EffectEnabledFunc.Value(item);
            }

            foreach (var item in Materials)
            {
                item.EffectEnabled.Value = EffectEnabledFunc.Value(item);
            }
        }
        
        private void SubscribeEquippedEnabledFunc(Func<InventoryItem, bool> func)
        {
            if (EquippedEnabledFunc.Value == null)
            {
                EquippedEnabledFunc.Value = DefaultEquippedFunc;
            }

            foreach (var item in Equipments)
            {
                item.EquippedEnabled.Value = EquippedEnabledFunc.Value(item);
            }

            foreach (var item in Consumables)
            {
                item.EquippedEnabled.Value = EquippedEnabledFunc.Value(item);
            }

            foreach (var item in Materials)
            {
                item.EquippedEnabled.Value = EquippedEnabledFunc.Value(item);
            }
        }

        #endregion

        private static bool DefaultDimmedFunc(InventoryItem inventoryItem)
        {
            return false;
        }
        
        private static bool DefaultCoveredFunc(InventoryItem inventoryItem)
        {
            return false;
        }
        
        private static bool DefaultEquippedFunc(InventoryItem inventoryItem)
        {
            if (!(inventoryItem.ItemBase.Value is Equipment equipment))
                return false;

            return equipment.equipped;
        }

        private void SetGlowedAll(bool value)
        {
            foreach (var item in Equipments)
            {
                item.GlowEnabled.Value = value;
            }

            foreach (var item in Consumables)
            {
                item.GlowEnabled.Value = value;
            }

            foreach (var item in Materials)
            {
                item.GlowEnabled.Value = value;
            }
        }
    }
}
