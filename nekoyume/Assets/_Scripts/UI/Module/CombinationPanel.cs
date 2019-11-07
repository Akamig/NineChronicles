using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public interface ICombinationPanel
    {
        int CostNCG { get; }
        int CostAP { get; }
    }

    public abstract class CombinationPanel<TMaterialView> : MonoBehaviour, ICombinationPanel
        where TMaterialView : CombinationMaterialView
    {
        private readonly List<IDisposable> _disposablesAtShow = new List<IDisposable>();

        [CanBeNull] public TMaterialView baseMaterial;
        public TMaterialView[] otherMaterials;
        public SubmitWithCostButton submitButton;

        public readonly Subject<InventoryItem> OnMaterialAdd = new Subject<InventoryItem>();
        public readonly Subject<InventoryItem> OnBaseMaterialAdd = new Subject<InventoryItem>();
        public readonly Subject<InventoryItem> OnOtherMaterialAdd = new Subject<InventoryItem>();
        public readonly Subject<InventoryItem> OnMaterialRemove = new Subject<InventoryItem>();
        public readonly Subject<InventoryItem> OnBaseMaterialRemove = new Subject<InventoryItem>();
        public readonly Subject<InventoryItem> OnOtherMaterialRemove = new Subject<InventoryItem>();

        public readonly Subject<CombinationPanel<TMaterialView>> OnMaterialChange =
            new Subject<CombinationPanel<TMaterialView>>();

        public readonly Subject<int> OnCostNCGChange = new Subject<int>();
        public readonly Subject<int> OnCostAPChange = new Subject<int>();

        public int CostNCG { get; private set; }
        public int CostAP { get; private set; }

        public bool IsThereAnyUnlockedEmptyMaterialView { get; private set; }
        public abstract bool IsSubmittable { get; }

        #region Initialize & Terminate

        protected virtual void Awake()
        {
            if (!(baseMaterial is null))
            {
                InitMaterialView(baseMaterial);
            }

            foreach (var otherMaterial in otherMaterials)
            {
                InitMaterialView(otherMaterial);
            }

            OnMaterialAdd
                .Merge(OnMaterialRemove)
                .Subscribe(_ => OnMaterialChange.OnNext(this))
                .AddTo(gameObject);
        }

        protected virtual void OnDestroy()
        {
            OnMaterialAdd.Dispose();
            OnBaseMaterialAdd.Dispose();
            OnOtherMaterialAdd.Dispose();
            OnMaterialRemove.Dispose();
            OnBaseMaterialRemove.Dispose();
            OnOtherMaterialRemove.Dispose();
            OnMaterialChange.Dispose();
            OnCostNCGChange.Dispose();
            OnCostAPChange.Dispose();
        }

        private void InitMaterialView(TMaterialView view)
        {
            view.OnClick.Subscribe(_ =>
                {
                    var tooltip = Widget.Find<ItemInformationTooltip>();
                    if (tooltip.Target &&
                        tooltip.Target.Equals(_.RectTransform))
                        tooltip.Close();
                    else
                        tooltip.Show(_.RectTransform, _.Model);
                })
                .AddTo(gameObject);
            view.OnDoubleClick.Subscribe(_ =>
            {
                Widget.Find<ItemInformationTooltip>().Close();
                if (_ is TMaterialView materialView)
                {
                    TryRemoveMaterial(materialView);
                }
            }).AddTo(gameObject);
            view.OnCountChange.Subscribe(_ => OnMaterialCountChanged()).AddTo(gameObject);
        }

        #endregion

        public virtual void Show()
        {
            gameObject.SetActive(true);
            OnMaterialAddedOrRemoved();
            OnMaterialCountChanged();
            ReactiveAgentState.Gold.Subscribe(SubscribeNCG).AddTo(_disposablesAtShow);
            ReactiveCurrentAvatarState.ActionPoint.Subscribe(SubscribeActionPoint).AddTo(_disposablesAtShow);
        }

        public virtual void Hide()
        {
            _disposablesAtShow.DisposeAllAndClear();

            RemoveMaterialsAll();

            gameObject.SetActive(false);
        }

        public abstract bool DimFunc(InventoryItem inventoryItem);

        public virtual bool Contains(InventoryItem inventoryItem)
        {
            if (inventoryItem.ItemBase.Value is ItemUsable itemUsable)
            {
                if (!(baseMaterial is null))
                {
                    if (baseMaterial.InventoryItemViewModel?.ItemBase.Value is ItemUsable itemUsable2)
                    {
                        if (itemUsable.ItemId.Equals(itemUsable2.ItemId))
                            return true;
                    }
                }

                foreach (var otherMaterial in otherMaterials)
                {
                    if (otherMaterial.InventoryItemViewModel?.ItemBase.Value is ItemUsable itemUsable2)
                    {
                        if (itemUsable.ItemId.Equals(itemUsable2.ItemId))
                            return true;
                    }
                }

                return false;
            }

            if (!(baseMaterial is null) &&
                !(baseMaterial.InventoryItemViewModel is null))
            {
                if (baseMaterial.InventoryItemViewModel.ItemBase.Value.Data.Id.Equals(
                    inventoryItem.ItemBase.Value.Data.Id))
                    return true;
            }

            foreach (var otherMaterial in otherMaterials)
            {
                if (!(otherMaterial.InventoryItemViewModel is null) &&
                    otherMaterial.InventoryItemViewModel.ItemBase.Value.Data.Id.Equals(
                        inventoryItem.ItemBase.Value.Data.Id))
                    return true;
            }

            return false;
        }

        protected abstract int GetCostNCG();
        protected abstract int GetCostAP();

        #region Add Material

        public bool TryAddMaterial(InventoryItemView view)
        {
            return TryAddMaterial(view, out var materialView);
        }

        public virtual bool TryAddMaterial(InventoryItemView view, out TMaterialView materialView)
        {
            if (view is null ||
                view.Model is null ||
                view.Model.Dimmed.Value)
            {
                materialView = null;
                return false;
            }

            if (TryAddBaseMaterial(view, out materialView))
            {
                OnMaterialAddedOrRemoved();
                OnMaterialCountChanged();
                OnMaterialAdd.OnNext(view.Model);
                OnBaseMaterialAdd.OnNext(view.Model);
                return true;
            }

            if (TryAddOtherMaterial(view, out materialView))
            {
                OnMaterialAddedOrRemoved();
                OnMaterialCountChanged();
                OnMaterialAdd.OnNext(view.Model);
                OnOtherMaterialAdd.OnNext(view.Model);
                return true;
            }

            return false;
        }

        protected virtual bool TryAddBaseMaterial(InventoryItemView view, out TMaterialView materialView)
        {
            if (baseMaterial is null)
            {
                materialView = null;
                return false;
            }

            if (!(baseMaterial.InventoryItemViewModel is null))
            {
                OnMaterialRemove.OnNext(baseMaterial.InventoryItemViewModel);
                OnBaseMaterialRemove.OnNext(baseMaterial.InventoryItemViewModel);
            }

            baseMaterial.Set(view);
            materialView = baseMaterial;
            return true;
        }

        protected virtual bool TryAddOtherMaterial(InventoryItemView view, out TMaterialView materialView)
        {
            var sameMaterial = otherMaterials.FirstOrDefault(e =>
            {
                if (e.Model?.ItemBase.Value is null ||
                    view.Model?.ItemBase.Value is null)
                    return false;

                return e.Model.ItemBase.Value.Data.Id == view.Model.ItemBase.Value.Data.Id;
            });
            if (sameMaterial is null)
            {
                // 새로 더하기.
                var possibleMaterial = otherMaterials.FirstOrDefault(e => !e.IsLocked && e.IsEmpty);
                if (possibleMaterial is null)
                {
                    // 제료가 이미 가득 찼어요!
                    materialView = null;
                    return false;
                }

                possibleMaterial.Set(view);
                materialView = possibleMaterial;
                return true;
            }

            // 하나 증가.
            sameMaterial.TryIncreaseCount();
            materialView = sameMaterial;
            return true;
        }

        #endregion

        #region Remove Material

        public bool TryRemoveMaterial(TMaterialView view)
        {
            return TryRemoveMaterial(view, out var materialView);
        }

        public virtual bool TryRemoveMaterial(TMaterialView view, out TMaterialView materialView)
        {
            if (view is null ||
                view.Model is null)
            {
                materialView = null;
                return false;
            }

            var inventoryItemView = view.InventoryItemViewModel;

            if (TryRemoveBaseMaterial(view, out materialView))
            {
                OnMaterialAddedOrRemoved();
                OnMaterialCountChanged();
                OnMaterialRemove.OnNext(inventoryItemView);
                OnBaseMaterialRemove.OnNext(inventoryItemView);
                return true;
            }

            if (TryRemoveOtherMaterial(view, out materialView))
            {
                OnMaterialAddedOrRemoved();
                OnMaterialCountChanged();
                OnMaterialRemove.OnNext(inventoryItemView);
                OnOtherMaterialRemove.OnNext(inventoryItemView);
                ReorderOtherMaterials();
                return true;
            }

            materialView = null;
            return false;
        }

        protected virtual bool TryRemoveBaseMaterial(TMaterialView view, out TMaterialView materialView)
        {
            if (baseMaterial is null ||
                baseMaterial.Model?.ItemBase.Value is null ||
                view is null ||
                view.Model?.ItemBase.Value is null ||
                baseMaterial.Model.ItemBase.Value.Data.Id != view.Model.ItemBase.Value.Data.Id)
            {
                materialView = null;
                return false;
            }

            if (baseMaterial.Model?.ItemBase.Value is Equipment baseEquipment)
            {
                if (!(view.Model?.ItemBase.Value is Equipment viewEquipment) ||
                    !baseEquipment.ItemId.Equals(viewEquipment.ItemId))
                {
                    materialView = null;
                    return false;
                }
            }

            baseMaterial.Clear();
            materialView = baseMaterial;
            return true;
        }

        protected virtual bool TryRemoveOtherMaterial(TMaterialView view, out TMaterialView materialView)
        {
            var sameMaterial = otherMaterials.FirstOrDefault(e =>
            {
                if (e.Model?.ItemBase.Value is null ||
                    view.Model?.ItemBase.Value is null)
                    return false;

                return e.Model.ItemBase.Value.Data.Id == view.Model.ItemBase.Value.Data.Id;
            });
            if (sameMaterial is null)
            {
                materialView = null;
                return false;
            }

            sameMaterial.Clear();
            materialView = sameMaterial;
            return true;
        }

        public virtual void RemoveMaterialsAll()
        {
            if (!(baseMaterial is null))
            {
                var model = baseMaterial.InventoryItemViewModel;
                baseMaterial.Clear();
                OnMaterialAddedOrRemoved();
                OnMaterialCountChanged();
                OnMaterialRemove.OnNext(model);
                OnBaseMaterialRemove.OnNext(model);
            }

            foreach (var material in otherMaterials)
            {
                var model = material.InventoryItemViewModel;
                material.Clear();
                OnMaterialAddedOrRemoved();
                OnMaterialCountChanged();
                OnMaterialRemove.OnNext(model);
                OnOtherMaterialRemove.OnNext(model);
            }
        }

        #endregion

        private void SubscribeNCG(decimal ncg)
        {
            if (CostNCG > 0)
            {
                submitButton.ShowNCG(CostNCG, ncg >= CostNCG);
            }
            else
            {
                submitButton.HideNCG();
            }
        }

        private void SubscribeActionPoint(int ap)
        {
            if (CostAP > 0)
            {
                submitButton.ShowAP(CostAP, ap >= CostAP);
            }
            else
            {
                submitButton.HideAP();
            }
        }

        private void ReorderOtherMaterials()
        {
            for (var i = 0; i < otherMaterials.Length; i++)
            {
                var dstMaterial = otherMaterials[i];
                if (!dstMaterial.IsEmpty)
                    continue;

                TMaterialView srcMaterial = null;
                for (var j = i + 1; j < otherMaterials.Length; j++)
                {
                    var tempMaterial = otherMaterials[j];
                    if (tempMaterial.IsEmpty)
                        continue;

                    srcMaterial = tempMaterial;
                    break;
                }

                if (srcMaterial is null)
                    break;

                var inventoryItemView = srcMaterial.InventoryItemViewModel.View;
                if (!TryRemoveOtherMaterial(srcMaterial, out srcMaterial) ||
                    !TryAddOtherMaterial(inventoryItemView, out dstMaterial))
                    break;
            }
        }

        private void OnMaterialAddedOrRemoved()
        {
            if (!(baseMaterial is null) &&
                baseMaterial.IsEmpty)
            {
                IsThereAnyUnlockedEmptyMaterialView = true;
                return;
            }

            IsThereAnyUnlockedEmptyMaterialView =
                otherMaterials.Any(otherMaterial => !otherMaterial.IsLocked && otherMaterial.IsEmpty);
        }

        protected virtual void OnMaterialCountChanged()
        {
            CostNCG = GetCostNCG();
            SubscribeNCG(ReactiveAgentState.Gold.Value);
            CostAP = GetCostAP();
            SubscribeActionPoint(ReactiveCurrentAvatarState.ActionPoint.Value);
            UpdateSubmittable();
            OnCostNCGChange.OnNext(CostNCG);
            OnCostAPChange.OnNext(CostAP);
            OnMaterialChange.OnNext(this);
        }

        private void UpdateSubmittable()
        {
            submitButton.SetSubmittable(IsSubmittable);
        }
    }
}
