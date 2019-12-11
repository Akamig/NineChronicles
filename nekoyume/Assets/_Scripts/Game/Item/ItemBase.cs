using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public abstract class ItemBase : IState
    {
        public ItemSheet.Row Data { get; }

        protected ItemBase(ItemSheet.Row data)
        {
            Data = data;
        }

        protected bool Equals(ItemBase other)
        {
            return Data.Id == other.Data.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ItemBase) obj);
        }

        public override int GetHashCode()
        {
            return (Data != null ? Data.GetHashCode() : 0);
        }

        public virtual string GetLocalizedName()
        {
            var name = Data.GetLocalizedName();
            return $"<color=#{GetColorHexByGrade()}>{name}</color>";
        }

        public string GetLocalizedDescription()
        {
            return Data.GetLocalizedDescription();
        }

        public Sprite GetIconSprite()
        {
            return SpriteHelper.GetItemIcon(Data.Id);
        }

        public Sprite GetBackgroundSprite()
        {
            return SpriteHelper.GetItemBackground(Data.Grade);
        }

        public virtual IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "data"] = Data.Serialize(),
            });
        
        protected string GetColorHexByGrade()
        {
            switch (Data.Grade)
            {
                case 1:
                    return GameConfig.ColorHexForGrade1;
                case 2:
                    return GameConfig.ColorHexForGrade2;
                case 3:
                    return GameConfig.ColorHexForGrade3;
                case 4:
                    return GameConfig.ColorHexForGrade4;
                case 5:
                    return GameConfig.ColorHexForGrade5;
                default:
                    return GameConfig.ColorHexForGrade1;
            }
        }
    }
}
