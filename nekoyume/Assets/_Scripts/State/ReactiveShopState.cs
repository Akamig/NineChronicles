using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using ShopItem = Nekoyume.Model.Item.ShopItem;

namespace Nekoyume.State
{
    /// <summary>
    /// ShopState가 포함하는 값의 변화를 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// </summary>
    public static class ReactiveShopState
    {
        private enum SortType
        {
            None = 0,
            Grade = 1,
            Cp = 2,
        }

        public static readonly ReactiveProperty<Dictionary<
                Address, Dictionary<ItemSubTypeFilter, Dictionary<
                        ShopSortFilter, Dictionary<int, List<ShopItem>>>>>>
            AgentProducts =
                new ReactiveProperty<Dictionary<
                    Address, Dictionary<
                        ItemSubTypeFilter,
                        Dictionary<ShopSortFilter, Dictionary<int, List<ShopItem>>>>>>();

        public static readonly ReactiveProperty<IReadOnlyDictionary<
                ItemSubTypeFilter, Dictionary<
                    ShopSortFilter, Dictionary<int, List<ShopItem>>>>>
            ItemSubTypeProducts = new ReactiveProperty<IReadOnlyDictionary<
                ItemSubTypeFilter, Dictionary<
                    ShopSortFilter, Dictionary<int, List<ShopItem>>>>>();

        private static int _shopItemsPerPage = 24;

        // FIXME: Manage in the other class
        public static readonly Dictionary<Guid, List<Nekoyume.UI.Model.ShopItem>> PurchaseHistory =
            new Dictionary<Guid, List<Nekoyume.UI.Model.ShopItem>>();

        private static List<ShopItem> _products = new List<ShopItem>();

        public static void Initialize(ShopState state, IEnumerable<ShopItem> shardedProducts, int shopItemsPerPage)
        {
            if (state is null)
            {
                return;
            }

            // It uses shaded shop state with the old store state.
            // Later, only shaded shop state will be used.
            _products = state.Products.Values.ToList();
            _products.AddRange(shardedProducts);

            Update(shopItemsPerPage);
        }

        public static void RemoveShopItem(Guid productId, int shopItemsPerPage)
        {

            var item = _products.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                _products.Remove(item);
            }

            Update(shopItemsPerPage);
        }

        public static void Update(int shopItemsPerPage)
        {
            _shopItemsPerPage = shopItemsPerPage;
            // AgentProducts.
            {
                var agentProducts = new Dictionary<Address, List<ShopItem>>();
                foreach (var product in _products)
                {
                    var agentAddress = product.SellerAgentAddress;
                    if (!agentProducts.ContainsKey(agentAddress))
                    {
                        agentProducts.Add(agentAddress, new List<ShopItem>());
                    }

                    if (Game.Game.instance.Agent.Address == agentAddress)
                    {
                        if (product.SellerAvatarAddress == States.Instance.CurrentAvatarState.address)
                        {
                            agentProducts[agentAddress].Add(product);
                        }
                    }
                    else
                    {
                        agentProducts[agentAddress].Add(product);
                    }
                }

                var filteredAgentProducts = new Dictionary<
                    Address,
                    Dictionary<ItemSubTypeFilter, Dictionary<ShopSortFilter, Dictionary<int, List<ShopItem>>>>>();
                foreach (var pair in agentProducts)
                {
                    filteredAgentProducts.Add(pair.Key, GetGroupedShopItemsByItemSubTypeFilter(pair.Value));
                }

                AgentProducts.Value = filteredAgentProducts;
            }

            // ItemSubTypeProducts.
            {
                var agentAddress = States.Instance.AgentState.address;
                ItemSubTypeProducts.Value = GetGroupedShopItemsByItemSubTypeFilter(_products
                    .Where(product => !product.SellerAgentAddress.Equals(agentAddress)).ToList());
            }
        }

        private static Dictionary<
                ItemSubTypeFilter, Dictionary<
                    ShopSortFilter, Dictionary<int, List<ShopItem>>>>
            GetGroupedShopItemsByItemSubTypeFilter(IReadOnlyCollection<ShopItem> shopItems)
        {
            var equipment = new List<ShopItem>();
            var food = new List<ShopItem>();
            var costume = new List<ShopItem>();
            var weapons = new List<ShopItem>();
            var armors = new List<ShopItem>();
            var belts = new List<ShopItem>();
            var necklaces = new List<ShopItem>();
            var rings = new List<ShopItem>();
            var foodsHp = new List<ShopItem>();
            var foodsAtk = new List<ShopItem>();
            var foodsDef = new List<ShopItem>();
            var foodsCri = new List<ShopItem>();
            var foodsHit = new List<ShopItem>();
            var fullCostumes = new List<ShopItem>();
            var hairCostumes = new List<ShopItem>();
            var earCostumes = new List<ShopItem>();
            var eyeCostumes = new List<ShopItem>();
            var tailCostumes = new List<ShopItem>();
            var titles = new List<ShopItem>();
            var materials = new List<ShopItem>();

            foreach (var shopItem in shopItems)
            {
                if (shopItem.ItemUsable != null)
                {
                    if (shopItem.ItemUsable.ItemSubType == ItemSubType.Food)
                    {
                        food.Add(shopItem);
                        var state = shopItem.ItemUsable.StatsMap.GetStats().First();
                        switch (state.StatType)
                        {
                            case StatType.HP:
                                foodsHp.Add(shopItem);
                                break;
                            case StatType.ATK:
                                foodsAtk.Add(shopItem);
                                break;
                            case StatType.DEF:
                                foodsDef.Add(shopItem);
                                break;
                            case StatType.CRI:
                                foodsCri.Add(shopItem);
                                break;
                            case StatType.HIT:
                                foodsHit.Add(shopItem);
                                break;
                        }
                    }
                    else
                    {
                        equipment.Add(shopItem);
                        switch (shopItem.ItemUsable.ItemSubType)
                        {
                            case ItemSubType.Weapon:
                                weapons.Add(shopItem);
                                break;
                            case ItemSubType.Armor:
                                armors.Add(shopItem);
                                break;
                            case ItemSubType.Belt:
                                belts.Add(shopItem);
                                break;
                            case ItemSubType.Necklace:
                                necklaces.Add(shopItem);
                                break;
                            case ItemSubType.Ring:
                                rings.Add(shopItem);
                                break;
                        }
                    }
                }
                else if (shopItem.Costume != null)
                {
                    costume.Add(shopItem);
                    switch (shopItem.Costume.ItemSubType)
                    {
                        case ItemSubType.FullCostume:
                            fullCostumes.Add(shopItem);
                            break;
                        case ItemSubType.HairCostume:
                            hairCostumes.Add(shopItem);
                            break;
                        case ItemSubType.EarCostume:
                            earCostumes.Add(shopItem);
                            break;
                        case ItemSubType.EyeCostume:
                            eyeCostumes.Add(shopItem);
                            break;
                        case ItemSubType.TailCostume:
                            tailCostumes.Add(shopItem);
                            break;
                        case ItemSubType.Title:
                            titles.Add(shopItem);
                            break;
                    }
                }
                else
                {
                    // 현재는 모래시계랑 AP포션 뿐이 없음.
                    materials.Add(shopItem);
                }
            }

            var groupedShopItems = new Dictionary<
                ItemSubTypeFilter, Dictionary<ShopSortFilter, Dictionary<int, List<ShopItem>>>>
            {
                {ItemSubTypeFilter.All, GetGroupedShopItemsBySortFilter(shopItems)},
                {ItemSubTypeFilter.Weapon, GetGroupedShopItemsBySortFilter(weapons)},
                {ItemSubTypeFilter.Armor, GetGroupedShopItemsBySortFilter(armors)},
                {ItemSubTypeFilter.Belt, GetGroupedShopItemsBySortFilter(belts)},
                {ItemSubTypeFilter.Necklace, GetGroupedShopItemsBySortFilter(necklaces)},
                {ItemSubTypeFilter.Ring, GetGroupedShopItemsBySortFilter(rings)},
                {ItemSubTypeFilter.Food_HP, GetGroupedShopItemsBySortFilter(foodsHp)},
                {ItemSubTypeFilter.Food_ATK, GetGroupedShopItemsBySortFilter(foodsAtk)},
                {ItemSubTypeFilter.Food_DEF, GetGroupedShopItemsBySortFilter(foodsDef)},
                {ItemSubTypeFilter.Food_CRI, GetGroupedShopItemsBySortFilter(foodsCri)},
                {ItemSubTypeFilter.Food_HIT, GetGroupedShopItemsBySortFilter(foodsHit)},
                {ItemSubTypeFilter.FullCostume, GetGroupedShopItemsBySortFilter(fullCostumes)},
                {ItemSubTypeFilter.HairCostume, GetGroupedShopItemsBySortFilter(hairCostumes)},
                {ItemSubTypeFilter.EarCostume, GetGroupedShopItemsBySortFilter(earCostumes)},
                {ItemSubTypeFilter.EyeCostume, GetGroupedShopItemsBySortFilter(eyeCostumes)},
                {ItemSubTypeFilter.TailCostume, GetGroupedShopItemsBySortFilter(tailCostumes)},
                {ItemSubTypeFilter.Title, GetGroupedShopItemsBySortFilter(titles)},
                {ItemSubTypeFilter.Materials, GetGroupedShopItemsBySortFilter(materials)},
            };
            return groupedShopItems;
        }

        private static Dictionary<ShopSortFilter, Dictionary<int, List<ShopItem>>>
            GetGroupedShopItemsBySortFilter(IReadOnlyCollection<ShopItem> shopItems)
        {
            return new Dictionary<ShopSortFilter, Dictionary<int, List<ShopItem>>>
            {
                {
                    ShopSortFilter.Class,
                    GetGroupedShopItemsByPage(GetSortedShopItems(shopItems, SortType.Grade))
                },
                {
                    ShopSortFilter.CP,
                    GetGroupedShopItemsByPage(GetSortedShopItems(shopItems, SortType.Cp))
                },
                {
                    ShopSortFilter.Price,
                    GetGroupedShopItemsByPage(shopItems
                        .OrderByDescending(shopItem => shopItem.Price)
                        .ToList())
                },
            };
        }

        private static List<ShopItem> GetSortedShopItems(IEnumerable<ShopItem> shopItems, SortType type)
        {
            var result = new List<ShopItem>();
            result.AddRange(shopItems.Where(shopItem => shopItem.Costume != null)
                .OrderByDescending(shopItem => GetTypeValue(shopItem.Costume, type)));
            result.AddRange(shopItems.Where(shopItem => shopItem.ItemUsable != null)
                .OrderByDescending(shopItem => GetTypeValue(shopItem.ItemUsable, type)));
            result.AddRange(shopItems.Where(shopItem => shopItem.TradableFungibleItem != null)
                .OrderByDescending(shopItem => GetTypeValue((ItemBase)shopItem.TradableFungibleItem, type)));
            return result;
        }

        private static int GetTypeValue(ItemBase item, SortType type)
        {
            switch (type)
            {
                case SortType.Grade:
                    return item.Grade;
                case SortType.Cp:
                    switch (item)
                    {
                        case ItemUsable itemUsable:
                            return CPHelper.GetCP(itemUsable);
                        case Costume costume:
                        {
                            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
                            return CPHelper.GetCP(costume, costumeSheet);
                        }
                        default:
                            return 0;
                    }
                    break;
                case SortType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        private static Dictionary<int, List<ShopItem>> GetGroupedShopItemsByPage(List<ShopItem> shopItems)
        {
            var result = new Dictionary<int, List<ShopItem>>();
            var remainCount = shopItems.Count;
            var listIndex = 0;
            var pageIndex = 0;
            while (remainCount > 0)
            {
                var getCount = Math.Min(_shopItemsPerPage, remainCount);
                var getList = shopItems.GetRange(listIndex, getCount);
                result.Add(pageIndex, getList);
                remainCount -= getCount;
                listIndex += getCount;
                pageIndex++;
            }

            return result;
        }
    }
}
