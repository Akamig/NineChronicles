using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Shop : Widget
    {
        public GameObject btnBuy;
        public GameObject btnSell;
        public GameObject btnConfirm;
        public ScrollRect cart;
        public Button sword;
        public List<CartItem> items;
        public GameObject itemBase;
        public Text totalPrice;

        private void Awake()
        {
            items = new List<CartItem>();
        }

        public void SwordClick()
        {
            GameObject newItem = Instantiate(itemBase, cart.content);
            CartItem item = newItem.GetComponent<CartItem>();
            var itemInfo = sword.GetComponent<Item>();
            item.itemName.text = itemInfo.itemName.text;
            item.price.text = itemInfo.price.text;
            item.info.text = itemInfo.info.text;
            item.icon.sprite = sword.GetComponent<Image>().sprite;
            item.gameObject.SetActive(true);
            items.Add(item);
            CalcTotalPrice();
        }

        public void CalcTotalPrice()
        {
            var total = 0;
            foreach (var item in items)
            {
                int price;
                int.TryParse(item.price.text, out price);
                total += price;
            }
            totalPrice.text = total.ToString();
        }
    }
}
