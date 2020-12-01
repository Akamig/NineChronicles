using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItems : IDisposable
    {
        public readonly ReactiveProperty<UI.Shop.StateType> State =
            new ReactiveProperty<UI.Shop.StateType>();

        public readonly ReactiveProperty<Dictionary<int, List<ShopItem>>> AgentProducts =
            new ReactiveProperty<Dictionary<int, List<ShopItem>>>();

        public readonly ReactiveProperty<Dictionary<int, List<ShopItem>>> ItemSubTypeProducts
            = new ReactiveProperty<Dictionary<int, List<ShopItem>>>();

        public readonly ReactiveProperty<ShopItemView> SelectedItemView =
            new ReactiveProperty<ShopItemView>();

        public readonly ReactiveProperty<ShopItem> SelectedItemViewModel =
            new ReactiveProperty<ShopItem>();

        public readonly Subject<ShopItemView> OnDoubleClickItemView = new Subject<ShopItemView>();

        public Module.ShopItems.ItemSubTypeFilter itemSubTypeFilter
            = Module.ShopItems.ItemSubTypeFilter.All;

        public Module.ShopItems.SortFilter sortFilter
            = Module.ShopItems.SortFilter.Class;

        private IReadOnlyDictionary<
            Address, Dictionary<
                Module.ShopItems.ItemSubTypeFilter, Dictionary<
                    Module.ShopItems.SortFilter, Dictionary<int, List<ShopItem>>>>> _agentProducts;

        private IReadOnlyDictionary<
                Module.ShopItems.ItemSubTypeFilter, Dictionary<
                    Module.ShopItems.SortFilter, Dictionary<int, List<ShopItem>>>>
            _itemSubTypeProducts;

        public void Dispose()
        {
            State.Dispose();
            AgentProducts.Dispose();
            ItemSubTypeProducts.Dispose();
            SelectedItemView.Dispose();
            SelectedItemViewModel.Dispose();
            OnDoubleClickItemView.Dispose();
        }

        public void ResetAgentProducts(IReadOnlyDictionary<
            Address, Dictionary<
                Module.ShopItems.ItemSubTypeFilter, Dictionary<
                    Module.ShopItems.SortFilter, Dictionary<
                        int, List<Nekoyume.Model.Item.ShopItem>>>>> products)
        {
            _agentProducts = products is null
                ? new Dictionary<
                    Address, Dictionary<
                        Module.ShopItems.ItemSubTypeFilter, Dictionary<
                            Module.ShopItems.SortFilter, Dictionary<int, List<ShopItem>>>>>()
                : products.ToDictionary(
                    pair => pair.Key,
                    pair => ModelToViewModel(pair.Value));

            ResetAgentProducts();
        }

        public void ResetItemSubTypeProducts(IReadOnlyDictionary<
                Module.ShopItems.ItemSubTypeFilter, Dictionary<
                    Module.ShopItems.SortFilter, Dictionary<int, List<Nekoyume.Model.Item.ShopItem>>
                >>
            products)
        {
            _itemSubTypeProducts = products is null
                ? new Dictionary<
                    Module.ShopItems.ItemSubTypeFilter, Dictionary<
                        Module.ShopItems.SortFilter, Dictionary<int, List<ShopItem>>>>()
                : ModelToViewModel(products);


            ResetItemSubTypeProducts();
        }

        private Dictionary<
                Module.ShopItems.ItemSubTypeFilter, Dictionary<
                    Module.ShopItems.SortFilter, Dictionary<int, List<ShopItem>>>>
            ModelToViewModel(IReadOnlyDictionary<
                Module.ShopItems.ItemSubTypeFilter, Dictionary<
                    Module.ShopItems.SortFilter, Dictionary<
                        int, List<Nekoyume.Model.Item.ShopItem>>>> shopItems)
        {
            return shopItems.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToDictionary(
                    pair2 => pair2.Key,
                    pair2 => pair2.Value.ToDictionary(
                        pair3 => pair3.Key,
                        pair3 => pair3.Value.Select(CreateShopItem).ToList())));
        }

        private void SubscribeItemOnClick(ShopItemView view)
        {
            if (view is null ||
                view == SelectedItemView.Value)
            {
                DeselectItemView();
                return;
            }

            SelectItemView(view);
        }

        public void SelectItemView(ShopItemView view)
        {
            if (view is null ||
                view.Model is null)
                return;

            DeselectItemView();

            SelectedItemView.Value = view;
            SelectedItemViewModel.Value = view.Model;
            SelectedItemViewModel.Value.Selected.Value = true;
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

        #region Shop Item

        public void RemoveAgentProduct(Guid productId)
        {
            var agentAddress = States.Instance.AgentState.address;
            if (!_agentProducts.ContainsKey(agentAddress))
            {
                return;
            }

            RemoveProduct(productId, _agentProducts[agentAddress], AgentProducts.Value);
            AgentProducts.SetValueAndForceNotify(AgentProducts.Value);
        }

        public void RemoveItemSubTypeProduct(Guid productId)
        {
            RemoveProduct(productId, _itemSubTypeProducts, ItemSubTypeProducts.Value);
            ItemSubTypeProducts.SetValueAndForceNotify(ItemSubTypeProducts.Value);
        }

        private static void RemoveProduct(
            Guid productId,
            IReadOnlyDictionary<
                Module.ShopItems.ItemSubTypeFilter, Dictionary<
                    Module.ShopItems.SortFilter, Dictionary<int, List<ShopItem>>>> origin,
            Dictionary<int, List<ShopItem>> reactivePropertyValue)
        {
            foreach (var pair in origin)
            {
                var removed = false;
                foreach (var pair2 in pair.Value)
                {
                    foreach (var pair3 in pair2.Value)
                    {
                        var target = pair3.Value.FirstOrDefault(item =>
                            item.ProductId.Value.Equals(productId));
                        if (target is null)
                        {
                            continue;
                        }

                        target.Dispose();
                        pair3.Value.Remove(target);
                        removed = true;
                    }
                }

                if (removed)
                {
                    break;
                }
            }

            foreach (var pair in reactivePropertyValue)
            {
                var target = pair.Value.FirstOrDefault(item =>
                    item.ProductId.Value.Equals(productId));
                if (target is null)
                {
                    continue;
                }

                target.Dispose();
                pair.Value.Remove(target);
            }
        }

        #endregion

        #region Reset

        public void ResetAgentProducts()
        {
            var agentAddress = States.Instance.AgentState.address;
            if (_agentProducts is null ||
                !_agentProducts.ContainsKey(agentAddress))
            {
                AgentProducts.Value = new Dictionary<int, List<ShopItem>>();
                return;
            }

            AgentProducts.Value = GetFilteredAndSortedProducts(_agentProducts[agentAddress]);
        }

        public void ResetItemSubTypeProducts()
        {
            ItemSubTypeProducts.Value = GetFilteredAndSortedProducts(_itemSubTypeProducts);
        }

        private Dictionary<int, List<ShopItem>> GetFilteredAndSortedProducts(IReadOnlyDictionary<
            Module.ShopItems.ItemSubTypeFilter, Dictionary<
                Module.ShopItems.SortFilter, Dictionary<int, List<ShopItem>>>> products)
        {
            if (products is null)
            {
                return new Dictionary<int, List<ShopItem>>();
            }

            if (!products.ContainsKey(itemSubTypeFilter))
            {
                return new Dictionary<int, List<ShopItem>>();
            }

            var itemSubTypeProducts = products[itemSubTypeFilter];
            if (!itemSubTypeProducts.ContainsKey(sortFilter))
            {
                return new Dictionary<int, List<ShopItem>>();
            }

            var sortProducts = itemSubTypeProducts[sortFilter];
            return sortProducts.Count == 0
                ? new Dictionary<int, List<ShopItem>>()
                : sortProducts;
        }

        private ShopItem CreateShopItem(Nekoyume.Model.Item.ShopItem shopItem)
        {
            var item = new ShopItem(shopItem);
            item.OnClick.Subscribe(model =>
            {
                if (!(model is ShopItem shopItemViewModel))
                {
                    return;
                }

                SubscribeItemOnClick(shopItemViewModel.View);
            });
            item.OnDoubleClick.Subscribe(model =>
            {
                if (!(model is ShopItem shopItemViewModel))
                {
                    return;
                }

                DeselectItemView();
                OnDoubleClickItemView.OnNext(shopItemViewModel.View);
            });

            return item;
        }

        #endregion

        public bool TryGetShopItemFromAgentProducts(Guid itemId, out ShopItem shopItem)
        {
            shopItem = AgentProducts.Value.Values
                .SelectMany(list => list)
                .Where(item => item.ItemBase.Value is INonFungibleItem)
                .FirstOrDefault(item =>
                    ((INonFungibleItem) item.ItemBase.Value).ItemId.Equals(itemId));

            return !(shopItem is null);
        }

        public bool TryGetShopItemFromItemSubTypeProducts(Guid itemId, out ShopItem shopItem)
        {
            shopItem = ItemSubTypeProducts.Value.Values
                .SelectMany(list => list)
                .Where(item => item.ItemBase.Value is INonFungibleItem)
                .FirstOrDefault(item =>
                    ((INonFungibleItem) item.ItemBase.Value).ItemId.Equals(itemId));

            return !(shopItem is null);
        }
    }
}
