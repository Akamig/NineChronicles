using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using DG.Tweening;
using Nekoyume.BlockChain;
using Nekoyume.Data;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Entrance;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Trigger;
using Nekoyume.Game.Util;
using Nekoyume.Game.VFX;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using Spine.Unity;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Stage : MonoBehaviour, IStage
    {
        public const float StageStartPosition = -1.2f;
        private const float SkillDelay = 0.1f;
        public ObjectPool objectPool;
        public PlayerFactory playerFactory;
        public EnemyFactory enemyFactory;
        public NpcFactory npcFactory;
        public DropItemFactory dropItemFactory;
        public SkillController skillController;
        public BuffController buffController;

        public MonsterSpawner spawner;

        public GameObject background;

        // dummy for stage background moving.
        public GameObject dummy;
        public ParticleSystem defaultBGVFX;
        public ParticleSystem bosswaveBGVFX;

        public int worldId;
        public int stageId;
        public Character.Player selectedPlayer;
        public readonly Vector2 questPreparationPosition = new Vector2(2.1f, -0.2f);
        public readonly Vector2 roomPosition = new Vector2(-2.808f, -1.519f);
        public bool repeatStage;
        public bool isExitReserved;
        public string zone;

        private Camera _camera;
        private BattleLog _battleLog;
        private BattleResult.Model _battleResultModel;

        public bool IsInStage { get; private set; }
        public Enemy Boss { get; private set; }
        public AvatarState AvatarState { get; set; }
        public Vector3 SelectPositionBegin(int index) => new Vector3(-2.15f + index * 2.22f, -1.79f, 0.0f);
        public Vector3 SelectPositionEnd(int index) => new Vector3(-2.15f + index * 2.22f, -0.25f, 0.0f);

        protected void Awake()
        {
            _camera = Camera.main;
            if (ReferenceEquals(_camera, null))
            {
                throw new NullReferenceException("`Camera.main` can't be null.");
            }

            if (ReferenceEquals(dummy, null))
            {
                throw new NullReferenceException("`Dummy` can't be null.");
            }

            Event.OnNestEnter.AddListener(OnNestEnter);
            Event.OnLoginDetail.AddListener(OnLoginDetail);
            Event.OnRoomEnter.AddListener(OnRoomEnter);
            Event.OnStageStart.AddListener(OnStageStart);
        }

        private void OnStageStart(BattleLog log)
        {
            if (_battleLog?.id != log.id)
            {
                _battleLog = log;
                Play(_battleLog);
            }
            else
            {
                Debug.Log("Skip duplicated battle");
            }
        }

        private void OnNestEnter()
        {
            gameObject.AddComponent<NestEntering>();
        }

        private void OnLoginDetail(int index)
        {
            DOTween.KillAll();
            var players = GetComponentsInChildren<Character.Player>(true);
            for (int i = 0; i < players.Length; ++i)
            {
                GameObject playerObject = players[i].gameObject;
                var anim = players[i].Animator;
                if (index == i)
                {
                    var moveTo = new Vector3(-0.05f, -0.5f);
                    playerObject.transform.DOScale(1.1f, 2.0f).SetDelay(0.2f);
                    playerObject.transform.DOMove(moveTo, 2.4f).SetDelay(0.2f);
                    var seqPos = new Vector3(moveTo.x, moveTo.y - UnityEngine.Random.Range(0.05f, 0.1f), 0.0f);
                    var seq = DOTween.Sequence();
                    seq.Append(playerObject.transform.DOMove(seqPos, UnityEngine.Random.Range(4.0f, 5.0f)));
                    seq.Append(playerObject.transform.DOMove(moveTo, UnityEngine.Random.Range(4.0f, 5.0f)));
                    seq.Play().SetDelay(2.6f).SetLoops(-1);
                    if (!ReferenceEquals(anim, null) && !anim.Target.activeSelf)
                    {
                        anim.Target.SetActive(true);
                        var skeleton = anim.Target.GetComponentInChildren<SkeletonAnimation>().skeleton;
                        skeleton.A = 0.0f;
                        DOTween.To(() => skeleton.A, x => skeleton.A = x, 1.0f, 1.0f);
                        anim.Appear();
                    }

                    selectedPlayer = players[i];
                }
                else
                {
                    playerObject.transform.DOScale(0.9f, 1.0f);
                    playerObject.transform.DOMoveY(-3.6f, 2.0f);

                    if (!ReferenceEquals(anim, null) && anim.Target.activeSelf)
                    {
                        anim.Target.SetActive(true);
                        anim.Disappear();
                    }
                }
            }
        }

        private void OnRoomEnter()
        {
            gameObject.AddComponent<RoomEntering>();
        }

        // todo: 배경 캐싱.
        public void LoadBackground(string prefabName, float fadeTime = 0.0f)
        {
            if (background)
            {
                if (background.name.Equals(prefabName))
                    return;

                if (fadeTime > 0.0f)
                {
                    var sprites = background.GetComponentsInChildren<SpriteRenderer>();
                    foreach (var sprite in sprites)
                    {
                        sprite.sortingOrder += 1;
                        sprite.DOFade(0.0f, fadeTime);
                    }

                    var particles = background.GetComponentsInChildren<ParticleSystem>();
                    foreach (var particle in particles)
                    {
                        particle.Stop();
                    }
                }

                Destroy(background, fadeTime);
                background = null;
            }

            var path = $"Prefab/Background/{prefabName}";
            var prefab = Resources.Load<GameObject>(path);
            if (!prefab)
                throw new FailedToLoadResourceException<GameObject>(path);

            background = Instantiate(prefab, transform);
            background.name = prefabName;
            foreach (Transform child in background.transform)
            {
                var childName = child.name;
                if (!childName.StartsWith("bgvfx"))
                    continue;

                var num = childName.Substring(childName.Length - 2);
                switch (num)
                {
                    case "01":
                        defaultBGVFX = child.GetComponent<ParticleSystem>();
                        break;
                    case "02":
                        bosswaveBGVFX = child.GetComponent<ParticleSystem>();
                        break;
                }
            }
        }

        public void Play(BattleLog log)
        {
            if (log?.Count > 0)
            {
                StartCoroutine(PlayAsync(log));
            }
        }

        private IEnumerator PlayAsync(BattleLog log)
        {
            yield return StartCoroutine(CoStageEnter(log.worldId, log.stageId));
            foreach (EventBase e in log)
            {
                yield return StartCoroutine(e.CoExecute(this));
            }

            yield return StartCoroutine(CoStageEnd(log));
        }

        private static IEnumerator CoDialog(int worldStage)
        {
            var stageDialogs = Tables.instance.StageDialogs.Values
                .Where(i => i.stageId == worldStage)
                .OrderBy(i => i.dialogId)
                .ToArray();
            if (stageDialogs.Any())
            {
                var dialog = Widget.Find<Dialog>();

                foreach (var stageDialog in stageDialogs)
                {
                    dialog.Show(stageDialog.dialogId);
                    yield return new WaitWhile(() => dialog.gameObject.activeSelf);
                }
            }

            yield return null;
        }

        private IEnumerator CoStageEnter(int worldId, int stageId)
        {
            IsInStage = true;
            if (!Game.instance.TableSheets.BackgroundSheet.TryGetValue(stageId, out var data))
            {
                yield break;
            }

            _battleResultModel = new BattleResult.Model();

            this.worldId = worldId;
            this.stageId = stageId;
            zone = data.Background;
            LoadBackground(zone, 3.0f);
            PlayBGVFX(false);
            RunPlayer();

            var title = Widget.Find<StageTitle>();
            title.Show(stageId);

            yield return new WaitForSeconds(2.0f);

            yield return StartCoroutine(title.CoClose());

            AudioController.instance.PlayMusic(data.BGM);
        }

        private IEnumerator CoStageEnd(BattleLog log)
        {
            Boss = null;
            yield return new WaitForSeconds(2.0f);
            Widget.Find<UI.Battle>().bossStatus.Close();
            Widget.Find<UI.Battle>().Close();
            if (log.result == BattleLog.Result.Win)
            {
                yield return new WaitForSeconds(0.75f);
                yield return StartCoroutine(CoDialog(log.stageId));
                var playerCharacter = GetPlayer();
                playerCharacter.Animator.Win();
                playerCharacter.ShowSpeech("PLAYER_WIN");
                yield return new WaitForSeconds(2.2f);
                StartCoroutine(CoSlideBg());
            }
            else
            {
                objectPool.ReleaseAll();
            }

            _battleResultModel.State = log.result;
            _battleResultModel.ShouldExit = isExitReserved;
            _battleResultModel.ShouldRepeat = repeatStage;
            _battleResultModel.ActionPointNotEnough =
                States.Instance.CurrentAvatarState.Value.actionPoint < GameConfig.HackAndSlashCostAP;
            Widget.Find<BattleResult>().Show(_battleResultModel);

            IsInStage = false;
            ActionRenderHandler.Instance.Pending = false;
            yield return null;
        }

        private IEnumerator CoSlideBg()
        {
            RunPlayer();
            while (Widget.Find<BattleResult>().IsActive())
            {
                yield return new WaitForEndOfFrame();
            }
        }

        public IEnumerator CoSpawnPlayer(Player character)
        {
            var playerCharacter = RunPlayer();
            playerCharacter.Set(character, true);
            playerCharacter.ShowSpeech("PLAYER_INIT");
            var player = playerCharacter.gameObject;

            var status = Widget.Find<Status>();
            status.UpdatePlayer(player);
            status.Show();
            status.ShowBattleStatus();

            var battle = Widget.Find<UI.Battle>();
            battle.Show(stageId, repeatStage);
            if (!(AvatarState is null) && !ActionRenderHandler.Instance.Pending)
            {
                ActionRenderHandler.Instance.UpdateCurrentAvatarState(AvatarState);
            }

            ActionRenderHandler.Instance.Pending = true;

            ActionCamera.instance.ChaseX(player.transform);
            yield return null;
        }

        #region Skill

        public IEnumerator CoNormalAttack(CharacterBase caster, IEnumerable<Model.Skill.SkillInfo> skillInfos,
            IEnumerable<Model.Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                yield return StartCoroutine(CoSkill(character, skillInfos, buffInfos, character.CoNormalAttack));
            }
        }

        public IEnumerator CoBlowAttack(CharacterBase caster, IEnumerable<Model.Skill.SkillInfo> skillInfos,
            IEnumerable<Model.Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                yield return StartCoroutine(CoSkill(character, skillInfos, buffInfos, character.CoBlowAttack));
            }
        }

        public IEnumerator CoDoubleAttack(CharacterBase caster, IEnumerable<Model.Skill.SkillInfo> skillInfos,
            IEnumerable<Model.Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                yield return StartCoroutine(CoSkill(character, skillInfos, buffInfos, character.CoDoubleAttack));
            }
        }

        public IEnumerator CoAreaAttack(CharacterBase caster, IEnumerable<Model.Skill.SkillInfo> skillInfos,
            IEnumerable<Model.Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                yield return StartCoroutine(CoSkill(character, skillInfos, buffInfos, character.CoAreaAttack));
            }
        }

        public IEnumerator CoHeal(CharacterBase caster, IEnumerable<Model.Skill.SkillInfo> skillInfos,
            IEnumerable<Model.Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                yield return StartCoroutine(CoSkill(character, skillInfos, buffInfos, character.CoHeal));
            }
        }

        public IEnumerator CoBuff(CharacterBase caster, IEnumerable<Model.Skill.SkillInfo> skillInfos,
            IEnumerable<Model.Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                yield return StartCoroutine(CoSkill(character, skillInfos, buffInfos, character.CoBuff));
            }
        }

        private IEnumerator CoSkill(Character.CharacterBase character, IEnumerable<Model.Skill.SkillInfo> skillInfos,
            IEnumerable<Model.Skill.SkillInfo> buffInfos,
            Func<IReadOnlyList<Model.Skill.SkillInfo>, IEnumerator> func)
        {
            if (!character)
                throw new ArgumentNullException(nameof(character));

            yield return StartCoroutine(CoBeforeSkill(character));

            yield return StartCoroutine(func(skillInfos.ToList()));

            yield return StartCoroutine(CoAfterSkill(character, buffInfos));
        }

        #endregion

        public IEnumerator CoDropBox(List<ItemBase> items)
        {
            if (items.Count > 0)
            {
                var dropItemFactory = GetComponent<DropItemFactory>();
                var player = GetPlayer();
                var position = player.transform.position;
                position.x += 1.0f;
                yield return StartCoroutine(dropItemFactory.CoCreate(items, position));
            }

            yield return null;
        }

        private IEnumerator CoBeforeSkill(Character.CharacterBase character)
        {
            if (!character)
                throw new ArgumentNullException(nameof(character));

            var enemy = GetComponentsInChildren<Character.CharacterBase>()
                .Where(c => c.gameObject.CompareTag(character.TargetTag) && c.IsAlive)
                .OrderBy(c => c.transform.position.x).FirstOrDefault();
            if (!enemy || character.TargetInAttackRange(enemy))
                yield break;

            character.StartRun();
            var time = Time.time;
            yield return new WaitUntil(() => Time.time - time > 2f || character.TargetInAttackRange(enemy));
        }

        private IEnumerator CoAfterSkill(Character.CharacterBase character,
            IEnumerable<Model.Skill.SkillInfo> buffInfos)
        {
            if (!character)
                throw new ArgumentNullException(nameof(character));

            character.UpdateHpBar();

            if (!(buffInfos is null))
            {
                foreach (var buffInfo in buffInfos)
                {
                    var buffCharacter = GetCharacter(buffInfo.Target);
                    if (!buffCharacter)
                        throw new ArgumentNullException(nameof(buffCharacter));
                    buffCharacter.UpdateHpBar();
//                    Debug.LogWarning(
//                        $"{buffCharacter.Animator.Target.name}'s {nameof(CoAfterSkill)} called: {buffCharacter.CurrentHP}({buffCharacter.Model.Stats.CurrentHP}) / {buffCharacter.HP}({buffCharacter.Model.Stats.LevelStats.HP}+{buffCharacter.Model.Stats.BuffStats.HP})");
                }
            }

            yield return new WaitForSeconds(SkillDelay);
            var enemy = GetComponentsInChildren<Character.CharacterBase>()
                .Where(c => c.gameObject.CompareTag(character.TargetTag) && c.IsAlive)
                .OrderBy(c => c.transform.position.x).FirstOrDefault();
            if (enemy && !character.TargetInAttackRange(enemy))
                character.StartRun();
        }

        public IEnumerator CoRemoveBuffs(CharacterBase caster)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                character.UpdateHpBar();
                if (character.HPBar.HpVFX != null)
                {
                    character.HPBar.HpVFX.Stop();
                }
            }

            yield break;
        }

        public IEnumerator CoGetReward(List<ItemBase> rewards)
        {
            foreach (var item in rewards)
            {
                var countableItem = new CountableItem(item, 1);
                _battleResultModel.AddReward(countableItem);
            }

            yield return null;
        }

        public IEnumerator CoSpawnWave(List<Enemy> enemies, bool isBoss)
        {
            Widget.Find<UI.Battle>().bossStatus.Close();
            var playerCharacter = GetPlayer();
            playerCharacter.StartRun();
            var battle = Widget.Find<UI.Battle>();

            if (isBoss)
            {
                yield return new WaitForSeconds(1.5f);
                playerCharacter.ShowSpeech("PLAYER_BOSS_STAGE");
                yield return new WaitForSeconds(1.5f);
                PlayBGVFX(true);
                AudioController.instance.PlayMusic(AudioController.MusicCode.Boss1);
                VFXController.instance.Create<BattleBossTitleVFX>(Vector3.zero);
                StartCoroutine(Widget.Find<Blind>().FadeIn(0.4f, "", 0.2f));
                yield return new WaitForSeconds(2.0f);
                StartCoroutine(Widget.Find<Blind>().FadeOut(0.2f));
                yield return new WaitForSeconds(2.0f);
                var boss = enemies.Last();
                Boss = boss;
                var sprite = SpriteHelper.GetCharacterIcon(boss.RowData.Id);
                battle.bossStatus.Show();
                battle.bossStatus.SetHp(boss.HP, boss.HP);
                battle.bossStatus.SetProfile(boss.Level, LocalizationManager.LocalizeCharacterName(boss.RowData.Id),
                    sprite);
                playerCharacter.ShowSpeech("PLAYER_BOSS_ENCOUNTER");
            }

            yield return StartCoroutine(spawner.CoSetData(stageId, enemies));
        }

        public IEnumerator CoGetExp(long exp)
        {
            _battleResultModel.Exp += exp;
            var player = GetPlayer();
            yield return StartCoroutine(player.CoGetExp(exp));
        }

        public Character.Player GetPlayer()
        {
            var player = GetComponentInChildren<Character.Player>();
            if (!(player is null))
                return player;

            var go = playerFactory.Create(States.Instance.CurrentAvatarState.Value);
            player = go.GetComponent<Character.Player>();

            if (player is null)
                throw new NotFoundComponentException<Character.Player>();

            return player;
        }

        public Character.Player GetPlayer(Vector2 position)
        {
            var player = GetPlayer();
            player.transform.position = position;
            return player;
        }

        public Character.Player RunPlayer()
        {
            var player = GetPlayer();
            var playerTransform = player.transform;
            Vector2 position = playerTransform.position;
            position.y = StageStartPosition;
            playerTransform.position = position;
            player.StartRun();
            return player;
        }

        /// <summary>
        /// 게임 캐릭터를 갖고 올 때 사용함.
        /// 갖고 올 때 매번 모델을 할당해주고 있음.
        /// 모델을 매번 할당하지 않고, 모델이 변경되는 로직 마다 바꿔주게 하는 것이 좋겠음. 물론 연출도 그때에 맞춰서 해주는 식.
        /// </summary>
        /// <param name="caster"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Character.CharacterBase GetCharacter(CharacterBase caster)
        {
            if (caster is null)
                throw new ArgumentNullException(nameof(caster));

            var character = GetComponentsInChildren<Character.CharacterBase>().FirstOrDefault(c => c.Id == caster.Id);
            if (!(character is null))
                character.Set(caster);
            return character;
        }

        private void PlayBGVFX(bool isBoss)
        {
            if (isBoss)
            {
                if (defaultBGVFX)
                    defaultBGVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (bosswaveBGVFX)
                    bosswaveBGVFX.Play(true);
            }
            else
            {
                if (bosswaveBGVFX)
                    bosswaveBGVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (defaultBGVFX)
                    defaultBGVFX.Play(true);
            }
        }
    }
}
