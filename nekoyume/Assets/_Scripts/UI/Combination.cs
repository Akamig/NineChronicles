using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Game.Mail;
using Nekoyume.Model;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UniRx;
using UnityEngine;
using Material = Nekoyume.Game.Item.Material;

namespace Nekoyume.UI
{
    public class Combination : Widget, RecipeCellView.IEventListener
    {
        public enum StateType
        {
            CombineConsumable,
            CombineEquipment,
            EnhanceEquipment
        }

        public readonly ReactiveProperty<StateType> State =
            new ReactiveProperty<StateType>(StateType.CombineEquipment);

        private const int NpcId = 300001;
        private static readonly UnityEngine.Vector3 NpcPosition = new UnityEngine.Vector3(2.28f, -2f);

        private ToggleGroup _toggleGroup;
        public CategoryButton combineEquipmentCategoryButton;
        public CategoryButton combineConsumableCategoryButton;
        public CategoryButton enhanceEquipmentCategoryButton;

        public Module.Inventory inventory;

        public CombineEquipment combineEquipment;
        public CombineConsumable combineConsumable;
        public EnhanceEquipment enhanceEquipment;
        public SpeechBubble speechBubble;

        private Npc _npc;

        public Recipe recipe;

        #region Override

        public override void Initialize()
        {
            base.Initialize();
            
            _toggleGroup = new ToggleGroup();
            _toggleGroup.OnToggledOn.Subscribe(SubscribeOnToggledOn).AddTo(gameObject);
            _toggleGroup.RegisterToggleable(combineEquipmentCategoryButton);
            _toggleGroup.RegisterToggleable(combineConsumableCategoryButton);
            _toggleGroup.RegisterToggleable(enhanceEquipmentCategoryButton);

            State.Subscribe(SubscribeState).AddTo(gameObject);

            inventory.SharedModel.SelectedItemView.Subscribe(ShowTooltip).AddTo(gameObject);
            inventory.SharedModel.OnDoubleClickItemView.Subscribe(StageMaterial).AddTo(gameObject);

            combineEquipment.RemoveMaterialsAll();
            combineEquipment.OnMaterialChange.Subscribe(SubscribeOnMaterialChange).AddTo(gameObject);
            combineEquipment.submitButton.OnSubmitClick.Subscribe(_ => ActionCombineEquipment()).AddTo(gameObject);
            
            combineConsumable.RemoveMaterialsAll();
            combineConsumable.OnMaterialChange.Subscribe(SubscribeOnMaterialChange).AddTo(gameObject);
            combineConsumable.submitButton.OnSubmitClick.Subscribe(_ => ActionCombineConsumable()).AddTo(gameObject);
            combineConsumable.recipeButton.OnClickAsObservable().Subscribe(_ =>
            {
                combineConsumable.submitButton.gameObject.SetActive(false);
                recipe.Show();
            }).AddTo(gameObject);

            enhanceEquipment.RemoveMaterialsAll();
            enhanceEquipment.OnMaterialChange.Subscribe(SubscribeOnMaterialChange).AddTo(gameObject);
            enhanceEquipment.submitButton.OnSubmitClick.Subscribe(_ => ActionEnhanceEquipment()).AddTo(gameObject);

            recipe.RegisterListener(this);
            recipe.closeButton.OnClickAsObservable()
                .Subscribe(_ => combineConsumable.submitButton.gameObject.SetActive(true)).AddTo(gameObject);
        }

        public override void Show()
        {
            base.Show();

            var stage = Game.Game.instance.stage;
            stage.LoadBackground("combination");
            var player = stage.GetPlayer();
            player.gameObject.SetActive(false);

            State.SetValueAndForceNotify(StateType.CombineEquipment);

            Find<BottomMenu>().Show(
                UINavigator.NavigationType.Back,
                SubscribeBackButtonClick,
                true,
                BottomMenu.ToggleableType.Mail,
                BottomMenu.ToggleableType.Quest,
                BottomMenu.ToggleableType.Chat,
                BottomMenu.ToggleableType.IllustratedBook);

            var go = Game.Game.instance.stage.npcFactory.Create(NpcId, NpcPosition);
            _npc = go.GetComponent<Npc>();
            go.SetActive(true);

            ShowSpeech("SPEECH_COMBINE_GREETING_", CharacterAnimation.Type.Greeting);
            AudioController.instance.PlayMusic(AudioController.MusicCode.Combination);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<BottomMenu>().Close(ignoreCloseAnimation);

            combineEquipment.RemoveMaterialsAll();
            combineConsumable.RemoveMaterialsAll();
            enhanceEquipment.RemoveMaterialsAll();

            base.Close(ignoreCloseAnimation);

            _npc.gameObject.SetActive(false);
            speechBubble.gameObject.SetActive(false);
        }

        #endregion

        public void OnRecipeCellViewStarClick(RecipeCellView recipeCellView)
        {
            Debug.LogWarning($"Recipe Star Clicked. {recipeCellView.Model.Row.Id}");
            // 즐겨찾기 등록.

            // 레시피 재정렬.
        }

        public void OnRecipeCellViewSubmitClick(RecipeCellView recipeCellView)
        {
            if (recipeCellView is null ||
                State.Value != StateType.CombineConsumable)
                return;

            Debug.LogWarning($"Recipe Submit Clicked. {recipeCellView.Model.Row.Id}");

            var inventoryItemViewModels = new List<InventoryItem>();
            if (recipeCellView.Model.MaterialInfos
                .Any(e =>
                {
                    if (!inventory.SharedModel.TryGetMaterial(e.Id, out var viewModel))
                        return true;

                    inventoryItemViewModels.Add(viewModel);
                    return false;
                }))
                return;

            recipe.Hide();

            combineConsumable.RemoveMaterialsAll();
            combineConsumable.ResetCount();
            foreach (var inventoryItemViewModel in inventoryItemViewModels)
            {
                combineConsumable.TryAddMaterial(inventoryItemViewModel);
            }
            combineConsumable.submitButton.gameObject.SetActive(true);
        }

        private void SubscribeState(StateType value)
        {
            inventory.Tooltip.Close();
            inventory.SharedModel.DeselectItemView();
            recipe.Hide();

            switch (value)
            {
                case StateType.CombineConsumable:
                    _toggleGroup.SetToggledOn(combineConsumableCategoryButton);

                    inventory.SharedModel.State.Value = ItemType.Material;
                    inventory.SharedModel.DimmedFunc.Value = combineConsumable.DimFunc;
                    inventory.SharedModel.EffectEnabledFunc.Value = combineConsumable.Contains;

                    combineEquipment.Hide();
                    combineConsumable.Show(true);
                    enhanceEquipment.Hide();
                    ShowSpeech("SPEECH_COMBINE_CONSUMABLE_");
                    break;
                case StateType.CombineEquipment:
                    _toggleGroup.SetToggledOn(combineEquipmentCategoryButton);

                    inventory.SharedModel.State.Value = ItemType.Material;
                    inventory.SharedModel.DimmedFunc.Value = combineEquipment.DimFunc;
                    inventory.SharedModel.EffectEnabledFunc.Value = combineEquipment.Contains;

                    combineEquipment.Show(true);
                    combineConsumable.Hide();
                    enhanceEquipment.Hide();
                    ShowSpeech("SPEECH_COMBINE_EQUIPMENT_");
                    break;
                case StateType.EnhanceEquipment:
                    _toggleGroup.SetToggledOn(enhanceEquipmentCategoryButton);

                    inventory.SharedModel.State.Value = ItemType.Equipment;
                    inventory.SharedModel.DimmedFunc.Value = enhanceEquipment.DimFunc;
                    inventory.SharedModel.EffectEnabledFunc.Value = enhanceEquipment.Contains;

                    combineEquipment.Hide();
                    combineConsumable.Hide();
                    enhanceEquipment.Show(true);
                    ShowSpeech("SPEECH_COMBINE_ENHANCE_EQUIPMENT_");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private void ShowTooltip(InventoryItemView view)
        {
            if (view is null ||
                view.RectTransform == inventory.Tooltip.Target)
            {
                inventory.Tooltip.Close();
                return;
            }

            inventory.Tooltip.Show(
                view.RectTransform,
                view.Model,
                value => !view.Model?.Dimmed.Value ?? false,
                LocalizationManager.Localize("UI_COMBINATION_REGISTER_MATERIAL"),
                tooltip => StageMaterial(view),
                tooltip => inventory.SharedModel.DeselectItemView());
        }

        private void StageMaterial(InventoryItemView itemView)
        {
            ShowSpeech("SPEECH_COMBINE_STAGE_MATERIAL_");
            switch (State.Value)
            {
                case StateType.CombineConsumable:
                    combineConsumable.TryAddMaterial(itemView);
                    break;
                case StateType.CombineEquipment:
                    combineEquipment.TryAddMaterial(itemView);
                    break;
                case StateType.EnhanceEquipment:
                    enhanceEquipment.TryAddMaterial(itemView);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SubscribeOnMaterialChange(CombinationPanel<CombinationMaterialView> viewModel)
        {
            inventory.SharedModel.UpdateDimAll();
            inventory.SharedModel.UpdateEffectAll();
        }

        private void SubscribeOnMaterialChange(CombinationPanel<EnhancementMaterialView> viewModel)
        {
            inventory.SharedModel.UpdateDimAll();
            inventory.SharedModel.UpdateEffectAll();
        }

        private void SubscribeOnToggledOn(IToggleable toggleable)
        {
            if (toggleable.Name.Equals(combineConsumableCategoryButton.Name))
            {
                State.Value = StateType.CombineConsumable;
            }
            else if (toggleable.Name.Equals(combineEquipmentCategoryButton.Name))
            {
                State.Value = StateType.CombineEquipment;
            }
            else if (toggleable.Name.Equals(enhanceEquipmentCategoryButton.Name))
            {
                State.Value = StateType.EnhanceEquipment;
            }
        }

        private void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            Close();
            Game.Event.OnRoomEnter.Invoke();
        }

        #region Action

        private void ActionCombineConsumable()
        {
            var materialInfoList = combineConsumable.otherMaterials
                .Where(e => !(e is null) && !e.IsEmpty)
                .Select(e => ((Material) e.Model.ItemBase.Value, e.Model.Count.Value))
                .ToList();

            UpdateCurrentAvatarState(combineConsumable, materialInfoList);
            CreateCombinationAction(materialInfoList);
            combineConsumable.RemoveMaterialsAll();
        }

        private void ActionCombineEquipment()
        {
            var materialInfoList = new List<(Material material, int value)>();
            materialInfoList.Add((
                (Material) combineEquipment.baseMaterial.Model.ItemBase.Value,
                combineEquipment.baseMaterial.Model.Count.Value));
            materialInfoList.AddRange(combineEquipment.otherMaterials
                .Where(e => !(e is null) && !(e.Model is null))
                .Select(e => ((Material)e.Model.ItemBase.Value, e.Model.Count.Value)));

            UpdateCurrentAvatarState(combineEquipment, materialInfoList);
            CreateCombinationAction(materialInfoList);
            combineEquipment.RemoveMaterialsAll();
        }

        private void ActionEnhanceEquipment()
        {
            var baseEquipmentGuid = ((Equipment) enhanceEquipment.baseMaterial.Model.ItemBase.Value).ItemId;
            var otherEquipmentGuidList = enhanceEquipment.otherMaterials
                .Select(e => ((Equipment) e.Model.ItemBase.Value).ItemId)
                .ToList();

            UpdateCurrentAvatarState(enhanceEquipment, baseEquipmentGuid, otherEquipmentGuidList);
            CreateItemEnhancementAction(baseEquipmentGuid, otherEquipmentGuidList);
            enhanceEquipment.RemoveMaterialsAll();
        }

        private void UpdateCurrentAvatarState(ICombinationPanel combinationPanel,
            IEnumerable<(Material material, int count)> materialInfoList)
        {
            States.Instance.AgentState.Value.gold -= combinationPanel.CostNCG;
            States.Instance.CurrentAvatarState.Value.actionPoint -= combinationPanel.CostAP;
            ReactiveCurrentAvatarState.ActionPoint.SetValueAndForceNotify(
                States.Instance.CurrentAvatarState.Value.actionPoint);
            foreach (var (material, count) in materialInfoList)
            {
                States.Instance.CurrentAvatarState.Value.inventory.RemoveFungibleItem(material, count);
            }

            ReactiveCurrentAvatarState.Inventory.SetValueAndForceNotify(
                States.Instance.CurrentAvatarState.Value.inventory);
        }

        private void UpdateCurrentAvatarState(ICombinationPanel combinationPanel, Guid baseItemGuid,
            IEnumerable<Guid> otherItemGuidList)
        {
            States.Instance.AgentState.Value.gold -= combinationPanel.CostNCG;
            States.Instance.CurrentAvatarState.Value.actionPoint -= combinationPanel.CostAP;
            ReactiveCurrentAvatarState.ActionPoint.SetValueAndForceNotify(
                States.Instance.CurrentAvatarState.Value.actionPoint);
            States.Instance.CurrentAvatarState.Value.inventory.RemoveNonFungibleItem(baseItemGuid);
            foreach (var itemGuid in otherItemGuidList)
            {
                States.Instance.CurrentAvatarState.Value.inventory.RemoveNonFungibleItem(itemGuid);
            }

            ReactiveCurrentAvatarState.Inventory.SetValueAndForceNotify(
                States.Instance.CurrentAvatarState.Value.inventory);
        }

        private void CreateCombinationAction(List<(Material material, int count)> materialInfoList)
        {
            var msg = LocalizationManager.Localize("NOTIFICATION_COMBINATION_START");
            Notification.Push(MailType.Workshop, msg);
            ActionManager.instance.Combination(materialInfoList)
                .Subscribe(_ => { }, _ => Find<ActionFailPopup>().Show("Timeout occurred during Combination"));
        }

        private void CreateItemEnhancementAction(Guid baseItemGuid, IEnumerable<Guid> otherItemGuidList)
        {
            var msg = LocalizationManager.Localize("NOTIFICATION_ITEM_ENHANCEMENT_START");
            Notification.Push(MailType.Workshop, msg);
            ActionManager.instance.ItemEnhancement(baseItemGuid, otherItemGuidList)
                .Subscribe(_ => { }, _ => Find<ActionFailPopup>().Show("Timeout occurred during ItemEnhancement"));
        }

        #endregion

        private void ShowSpeech(string key, CharacterAnimation.Type type = CharacterAnimation.Type.Emotion)
        {
            if (_npc)
            {
                if (type == CharacterAnimation.Type.Greeting)
                {
                    _npc.Greeting();
                }
                else
                {
                    _npc.Emotion();
                }
                speechBubble.SetKey(key);
                StartCoroutine(speechBubble.CoShowText());
            }
        }
    }
}
