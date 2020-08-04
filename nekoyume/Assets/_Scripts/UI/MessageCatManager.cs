﻿using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.UI.AnimatedGraphics;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI
{
    public class MessageCatManager : Widget
    {
        private readonly List<MessageCat> _pool = new List<MessageCat>();

        [SerializeField]
        private GameObject messageCatPrefab = null;

        public override WidgetType WidgetType => WidgetType.Tooltip;

        protected override void Awake()
        {
            base.Awake();

            OnDisableStaticObservable
                .Subscribe(_ => HideAll(false))
                .AddTo(gameObject);

            CloseWidget = null;
        }

        public MessageCat Show(Vector3 position, string message, bool reverseDirection = false)
        {
            var cat = Pick();
            cat.Show(position, message, reverseDirection);
            return cat;
        }

        public MessageCat Show(bool followMouse, string message, bool reverseDirection = false)
        {
            var cat = Pick();
            cat.Show(followMouse, message, reverseDirection);
            return cat;
        }

        public void HideAll(bool lazyHide = true)
        {
            foreach (var messageCat in _pool.Where(messageCat => messageCat.IsShown))
            {
                messageCat.Hide(lazyHide);
            }
        }

        private MessageCat Pick()
        {
            foreach (var messageCat in _pool.Where(messageCat => !messageCat.IsShown))
            {
                return messageCat;
            }

            var newOne = Create();
            _pool.Add(newOne);
            return newOne;
        }

        private MessageCat Create()
        {
            return Instantiate(messageCatPrefab, transform).GetComponent<MessageCat>();
        }
    }
}
