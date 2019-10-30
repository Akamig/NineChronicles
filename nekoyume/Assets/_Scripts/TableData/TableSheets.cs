using System;
using System.Collections;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;

namespace Nekoyume.TableData
{
    public class TableSheets
    {
        public readonly ReactiveProperty<float> loadProgress = new ReactiveProperty<float>();

        public BackgroundSheet BackgroundSheet { get; private set; }
        public WorldSheet WorldSheet { get; private set; }
        public StageSheet StageSheet { get; private set; }
        public StageRewardSheet StageRewardSheet { get; private set; }
        public CharacterSheet CharacterSheet { get; private set; }
        public LevelSheet LevelSheet { get; private set; }
        public SkillSheet SkillSheet { get; private set; }
        public BuffSheet BuffSheet { get; private set; }
        public ItemSheet ItemSheet { get; private set; }
        public MaterialItemSheet MaterialItemSheet { get; private set; }
        public EquipmentItemSheet EquipmentItemSheet { get; private set; }
        public ConsumableItemSheet ConsumableItemSheet { get; private set; }
        public QuestSheet QuestSheet { get; private set; }
        public WorldQuestSheet WorldQuestSheet { get; private set; }
        public CollectQuestSheet CollectQuestSheet { get; private set; }
        public CombinationQuestSheet CombinationQuestSheet { get; private set; }
        public TradeQuestSheet TradeQuestSheet { get; private set; }

        public ItemEnhancementQuestSheet ItemEnhancementQuestSheet { get; private set; }
        public GeneralQuestSheet GeneralQuestSheet { get; private set; }
        public SkillBuffSheet SkillBuffSheet { get; private set; }
        public MonsterQuestSheet MonsterQuestSheet { get; private set; }
        public ItemGradeQuestSheet ItemGradeQuestSheet { get; private set; }
        public ItemTypeCollectQuestSheet ItemTypeCollectQuestSheet { get; private set; }
        public GoldQuestSheet GoldQuestSheet { get; private set; }
        public EquipmentItemSetEffectSheet EquipmentItemSetEffectSheet { get; private set; }
        public EnemySkillSheet EnemySkillSheet { get; private set; }
        public ItemConfigForGradeSheet ItemConfigForGradeSheet { get; private set; }
        public ConsumableItemRecipeSheet ConsumableItemRecipeSheet { get; private set; }

        public QuestRewardSheet QuestRewardSheet { get; private set; }
        public QuestItemRewardSheet QuestItemRewardSheet { get; set; }

        public IEnumerator CoInitialize()
        {
            loadProgress.Value = 0f;

            var request = Resources.LoadAsync<AddressableAssetsContainer>("AddressableAssetsContainer");
            yield return request;
            if (!(request.asset is AddressableAssetsContainer addressableAssetsContainer))
                throw new NullReferenceException(nameof(addressableAssetsContainer));

            var tableCsvAssets = addressableAssetsContainer.tableCsvAssets;
            var loadTaskCount = tableCsvAssets.Count;
            var loadedTaskCount = 0;
            //어드레서블어셋에 새로운 테이블을 추가하면 AddressableAssetsContainer.asset에도 해당 csv파일을 추가해줘야합니다.
            foreach (var textAsset in tableCsvAssets)
            {
                SetToSheet(textAsset.name, textAsset.text);
                loadedTaskCount++;
                loadProgress.Value = (float)loadedTaskCount / loadTaskCount;
            }

            loadProgress.Value = 1f;

            ItemSheetInitialize();
            QuestSheetInitialize();
        }

        private void SetToSheet(string name, string csv)
        {
            switch (name)
            {
                case nameof(TableData.BackgroundSheet):
                    BackgroundSheet = new BackgroundSheet();
                    BackgroundSheet.Set(csv);
                    break;
                case nameof(TableData.WorldSheet):
                    WorldSheet = new WorldSheet();
                    WorldSheet.Set(csv);
                    break;
                case nameof(TableData.StageSheet):
                    StageSheet = new StageSheet();
                    StageSheet.Set(csv);
                    break;
                case nameof(TableData.StageRewardSheet):
                    StageRewardSheet = new StageRewardSheet();
                    StageRewardSheet.Set(csv);
                    break;
                case nameof(TableData.CharacterSheet):
                    CharacterSheet = new CharacterSheet();
                    CharacterSheet.Set(csv);
                    break;
                case nameof(TableData.LevelSheet):
                    LevelSheet = new LevelSheet();
                    LevelSheet.Set(csv);
                    break;
                case nameof(TableData.SkillSheet):
                    SkillSheet = new SkillSheet();
                    SkillSheet.Set(csv);
                    break;
                case nameof(TableData.BuffSheet):
                    BuffSheet = new BuffSheet();
                    BuffSheet.Set(csv);
                    break;
                case nameof(TableData.MaterialItemSheet):
                    MaterialItemSheet = new MaterialItemSheet();
                    MaterialItemSheet.Set(csv);
                    break;
                case nameof(TableData.EquipmentItemSheet):
                    EquipmentItemSheet = new EquipmentItemSheet();
                    EquipmentItemSheet.Set(csv);
                    break;
                case nameof(TableData.ConsumableItemSheet):
                    ConsumableItemSheet = new ConsumableItemSheet();
                    ConsumableItemSheet.Set(csv);
                    break;
                case nameof(TableData.WorldQuestSheet):
                    WorldQuestSheet = new WorldQuestSheet();
                    WorldQuestSheet.Set(csv);
                    break;
                case nameof(TableData.CollectQuestSheet):
                    CollectQuestSheet = new CollectQuestSheet();
                    CollectQuestSheet.Set(csv);
                    break;
                case nameof(TableData.CombinationQuestSheet):
                    CombinationQuestSheet = new CombinationQuestSheet();
                    CombinationQuestSheet.Set(csv);
                    break;
                case nameof(TableData.TradeQuestSheet):
                    TradeQuestSheet = new TradeQuestSheet();
                    TradeQuestSheet.Set(csv);
                    break;
                case nameof(TableData.MonsterQuestSheet):
                    MonsterQuestSheet = new MonsterQuestSheet();
                    MonsterQuestSheet.Set(csv);
                    break;
                case nameof(TableData.ItemEnhancementQuestSheet):
                    ItemEnhancementQuestSheet = new ItemEnhancementQuestSheet();
                    ItemEnhancementQuestSheet.Set(csv);
                    break;
                case nameof(TableData.GeneralQuestSheet):
                    GeneralQuestSheet = new GeneralQuestSheet();
                    GeneralQuestSheet.Set(csv);
                    break;
                case nameof(TableData.SkillBuffSheet):
                    SkillBuffSheet = new SkillBuffSheet();
                    SkillBuffSheet.Set(csv);
                    break;
                case nameof(TableData.EquipmentItemSetEffectSheet):
                    EquipmentItemSetEffectSheet = new EquipmentItemSetEffectSheet();
                    EquipmentItemSetEffectSheet.Set(csv);
                    break;
                case nameof(TableData.ItemGradeQuestSheet):
                    ItemGradeQuestSheet = new ItemGradeQuestSheet();
                    ItemGradeQuestSheet.Set(csv);
                    break;
                case nameof(TableData.ItemTypeCollectQuestSheet):
                    ItemTypeCollectQuestSheet = new ItemTypeCollectQuestSheet();
                    ItemTypeCollectQuestSheet.Set(csv);
                    break;
                case nameof(TableData.GoldQuestSheet):
                    GoldQuestSheet= new GoldQuestSheet();
                    GoldQuestSheet.Set(csv);
                    break;
                case nameof(TableData.EnemySkillSheet):
                    EnemySkillSheet = new EnemySkillSheet();
                    EnemySkillSheet.Set(csv);
                    break;
                case nameof(TableData.ItemConfigForGradeSheet):
                    ItemConfigForGradeSheet = new ItemConfigForGradeSheet();
                    ItemConfigForGradeSheet.Set(csv);
                    break;
                case nameof(TableData.ConsumableItemRecipeSheet):
                    ConsumableItemRecipeSheet = new ConsumableItemRecipeSheet();
                    ConsumableItemRecipeSheet.Set(csv);
                    break;
                case nameof(TableData.QuestRewardSheet):
                    QuestRewardSheet = new QuestRewardSheet();
                    QuestRewardSheet.Set(csv);
                    break;
                case nameof(TableData.QuestItemRewardSheet):
                    QuestItemRewardSheet = new QuestItemRewardSheet();
                    QuestItemRewardSheet.Set(csv);
                    break;
                default:
                    throw new InvalidDataException($"Not found {name} class in namespace `TableData`");
            }
        }

        private void ItemSheetInitialize()
        {
            ItemSheet = new ItemSheet();
            ItemSheet.Set(ConsumableItemSheet, false);
            ItemSheet.Set(EquipmentItemSheet, false);
            ItemSheet.Set(MaterialItemSheet);
        }

        private void QuestSheetInitialize()
        {
            QuestSheet = new QuestSheet();
            QuestSheet.Set(WorldQuestSheet, false);
            QuestSheet.Set(CollectQuestSheet, false);
            QuestSheet.Set(CombinationQuestSheet, false);
            QuestSheet.Set(TradeQuestSheet, false);
            QuestSheet.Set(MonsterQuestSheet, false);
            QuestSheet.Set(ItemEnhancementQuestSheet, false);
            QuestSheet.Set(GeneralQuestSheet, false);
            QuestSheet.Set(ItemGradeQuestSheet, false);
            QuestSheet.Set(ItemTypeCollectQuestSheet, false);
            QuestSheet.Set(GoldQuestSheet);
        }
    }
}
