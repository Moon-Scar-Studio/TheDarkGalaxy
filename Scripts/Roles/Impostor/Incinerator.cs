using Dark.Scripts.Roles.Crewmates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Helpers;

namespace Dark.Scripts.Roles.Impostor;

[NebulaRPCHolder]
public class Incinerator : DefinedRoleTemplate, HasCitation, RuntimeAssignableGenerator<RuntimeRole>, IAssignableDocument, DefinedRole
{
    static readonly FloatConfiguration RigCoolDown = NebulaAPI.Configurations.Configuration("options.role.incinerator.RigCoolDown", (10,60,2.5f),30,FloatConfigurationDecorator.Second);
    static readonly IntegerConfiguration RigMaxUses = NebulaAPI.Configurations.Configuration("options.role.incinerator.RigMaxUses",(1,10,1),2);
    
    /// <summary>
    /// 找人选择器，0炸所有人，1可以炸自己，2可以炸队友，3可以只不炸自己和队友
    /// </summary>
    static readonly ValueConfiguration<int> CanBoomOther = NebulaAPI.Configurations.Configuration("options.role.incinerator.CanBoomOther", ["options.role.incinerator.CanBoomSelf.AnyOne", "options.role.incinerator.CanBoomSelf.CanSelf", "options.role.incinerator.CanBoomSelf.CanMate", "options.role.incinerator.CanBoomSelf.ExcludeSelfAndMate"],3);
    
    /// <summary>
    /// 是否需要在尸体交互范围内，为false则可以在任何地方引爆
    /// </summary>
    static readonly BoolConfiguration NeedBodyPos = NebulaAPI.Configurations.Configuration("options.role.incinerator.NeedBodyPos",false);
    
    /// <summary>
    /// 引爆冷却
    /// </summary>
    static readonly FloatConfiguration DetonateCoolDown = NebulaAPI.Configurations.Configuration("options.role.incinerator.Detonate",(0,60,2.5f),5,FloatConfigurationDecorator.Second);

    static readonly BoolConfiguration CantMove = NebulaAPI.Configurations.Configuration("options.role.incinerator.CantMove",true);
    /// <summary>
    /// 处理所需时间
    /// </summary>
    static readonly FloatConfiguration RigTime = NebulaAPI.Configurations.Configuration("options.role.incinerator.RigTime",(0,60,2.5f),5);

    static readonly BoolConfiguration CanDragBody = NebulaAPI.Configurations.Configuration("options.role.incinerator.CanDragBody",true);
    static readonly FloatConfiguration ExplosionRadius = NebulaAPI.Configurations.Configuration("options.role.incinerator.ExplosionRadius",(1f,10f,0.5f),3f,FloatConfigurationDecorator.Ratio);
    static readonly BoolConfiguration EnableWarning = NebulaAPI.Configurations.Configuration("options.role.incinerator.EnableWarning",true);
    static readonly FloatConfiguration WarningDuration = NebulaAPI.Configurations.Configuration("options.role.incinerator.WarningDuration",(0f,10f,0.5f),2f,FloatConfigurationDecorator.Second);
    static readonly BoolConfiguration LeaveBody = NebulaAPI.Configurations.Configuration("options.role.incinerator.LeaveBody",false);
    static readonly FloatConfiguration DragDuration = NebulaAPI.Configurations.Configuration("options.role.incinerator.DragDuration",(1f,30f,0.5f),5f,FloatConfigurationDecorator.Second);
    static readonly FloatConfiguration DragCoolDown = NebulaAPI.Configurations.Configuration("options.role.incinerator.DragCoolDown",(0f,60f,2.5f),10f,FloatConfigurationDecorator.Second);
    public Citation? Citation => Core.Citations.TheDarkGalaxy_GoodEndGambler;

    public Incinerator() : base("incinerator", Virial.Color.ImpostorColor, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [RigCoolDown,RigMaxUses,CanBoomOther,NeedBodyPos,DetonateCoolDown,CantMove,RigTime, CanDragBody, ExplosionRadius, EnableWarning, WarningDuration, LeaveBody, DragDuration, DragCoolDown])
    {

    }
    public RuntimeRole CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public static readonly Incinerator MyRole = new();
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new AssignableDocumentImage(
            NebulaAPI.AddonAsset.GetResource("Buttons/Impostor/IncineratorRig.png")?.AsImage(100f),

            "role.incinerator.doc.rig");
        yield return new AssignableDocumentImage(
            NebulaAPI.AddonAsset.GetResource("Buttons/Impostor/IncineratorDetonate.png")?.AsImage(100f),
            "role.incinerator.doc.detonate");
        if (CanDragBody)
        {
            yield return new AssignableDocumentImage(
                NebulaAPI.AddonAsset.GetResource("Buttons/Impostor/IncineratorDrag.png")?.AsImage(100f),
                "role.incinerator.doc.drag"
                );
        }
    }
    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new AssignableDocumentReplacement("%RIGMU%", RigMaxUses.GetValue().ToString()); // 处理最大使用次数
        yield return new AssignableDocumentReplacement("%OTHER%", GetOtherDocText()); // 炸人对象
        yield return new AssignableDocumentReplacement("%NEEDPOS%", l2("needpos", NeedBodyPos));
        // 位置
        yield return new AssignableDocumentReplacement("%DETONATECD%", DetonateCoolDown.GetValue().ToString()); // 引爆CD
        yield return new AssignableDocumentReplacement("%CANTMOVE%",l2("cantmove",CantMove)); // 移动
        yield return new AssignableDocumentReplacement("%EXPRAD%",ExplosionRadius.GetValue().ToString());
        yield return new AssignableDocumentReplacement("%RIGTIME%",RigTime.GetValue().ToString());
        yield return new AssignableDocumentReplacement("%ENABLEWAR%",l2("enablewarning",EnableWarning));
        yield return new AssignableDocumentReplacement("%DUR%",WarningDuration.GetValue().ToString());
    }
    static string GetOtherDocText()
    {
        int i = CanBoomOther.GetValue();

        return i switch
        {
            0 => l("all"),
            1 => l("self"),
            2 => l("mate"),
            _ => l("excludeSelfNMate")
        };
    }
    static string l(string content) => Language.Translate($"role.incinerator.doc.canboomother.{content}");
    static string l2(string c,bool a) => Language.Translate($"role.incinerator.doc.{c}.{a}");
    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, IGameOperator
    {
        public Instance(GamePlayer player) : base(player)
        {
        }

        public DefinedRole Role => MyRole;
        int rigLeftUses;
        Image? rigImage;
        Image? detonateImage;
        Image? dragImage;
        bool canDetonate;
        bool movementLocked;
        bool wasMoveable = true;
        Virial.Game.DeadBody? riggingBody;
        Arrow? corpseArrow;
        GameObject? rigIconHolder;
        PoolablePlayer? rigIcon;
        byte rigIconPlayerId = byte.MaxValue;
        Virial.Game.DeadBody? processedBody;
        int activeDetonationId;
        static readonly Image? AshImage = DarkUtils.AsImage("Other/ash.png", 100);
        static readonly HashSet<int> CanceledDetonations = new();
        static int detonationCounter;
        Virial.Game.DeadBody? DeadBody;
        int expKills;
        bool countingExp;
        int impostorsBeforeExplosion;
        static bool firstRound = true;
        static bool usedBombInFirstRound;
        static float lastBombExplosionTime = -999f;
        
        public void OnActivated()
        {
            GameOperatorManager.Instance?.Subscribe<PlayerKillPlayerEvent>(OnPlayerKillPlayer, this);

            if (!AmOwner) return;

            firstRound = true;
            usedBombInFirstRound = false;
            lastBombExplosionTime = -999f;
            GameOperatorManager.Instance?.Subscribe<MeetingStartEvent>(_ => firstRound = false, this);
            GameOperatorManager.Instance?.SubscribeAchievement<GameEndEvent>("incinerator.firstRoundWin",IsFirstRoundBombWin,this);

            rigLeftUses = RigMaxUses;
            canDetonate = false;

            rigImage = DarkUtils.AsImage("Buttons/Impostor/IncineratorRig.png",115);
            detonateImage = DarkUtils.AsImage("Buttons/Impostor/IncineratorDetonate.png", 115);
            dragImage = DarkUtils.AsImage("Buttons/Impostor/IncineratorDrag.png", 115);

            var tracker = ObjectTrackers.ForDeadBody(
                this,null,MyPlayer,body=>body.IsActive && body.CurrentHolder == null && !body.IsFullyDissolved && body != processedBody && body != riggingBody,color:UnityEngine.Color.red
                );
            /*
 焚化工 
内鬼
根据房主设置，可以拖动鸡腿 ok
技能「锅炉准备」
处理鸡腿，处理过程中无法移动 ok，
            使其可以被引爆，炸死附近的所有人，如果处理过的鸡腿被吃掉，引爆则炸死食用者
根据房主设置，可以炸队友或自己
根据房主设置，引爆方式为必须在尸体交互范围内或者可以在任何一地引爆。
 */
            var rigButton = NebulaAPI.Modules.EffectButton(
                this,MyPlayer,VirtualKeyInput.Ability,null,RigCoolDown,RigTime,"RIG",rigImage, (button) => !canDetonate && rigLeftUses > 0 && tracker.CurrentTarget != null
                );
            rigButton.ShowUsesIcon(0, rigLeftUses.ToString());

            if (rigButton is ModAbilityButtonImpl rigButtonImpl && rigButtonImpl.VanillaButton != null)
            {
                rigIconHolder = UnityHelper.CreateObject("IncineratorRigIcon", rigButtonImpl.VanillaButton.transform, new UnityEngine.Vector3(0f, 0.55f, -1f));
            }

            rigButton.Availability = b =>
                b.IsInEffect ||
                (!canDetonate && rigLeftUses > 0 && tracker.CurrentTarget != null && MyPlayer.CanMove);
            rigButton.OnClick = btn =>
            {
                if (btn.IsInEffect)
                {
                    btn.InterruptEffect();
                    return;
                }

                if (!canDetonate && rigLeftUses > 0 && tracker.CurrentTarget != null)
                    btn.StartEffect();
            };

            rigButton.OnEffectStart = btn =>
            {
                riggingBody = tracker.CurrentTarget;
                if (riggingBody == null || !riggingBody.IsActive)
                {
                    btn.InterruptEffect();
                    return;
                }

                if (CantMove.GetValue() && !MyPlayer.IsDead && MeetingHud.Instance == null)
                {
                    wasMoveable = MyPlayer.VanillaPlayer.moveable;
                    MyPlayer.VanillaPlayer.moveable = false;
                    MyPlayer.VanillaPlayer.MyPhysics.body.velocity = UnityEngine.Vector2.zero;
                    movementLocked = true;
                }
                tracker.KeepAsLongAsPossible = true;
            };
            rigButton.OnEffectEnd = btn => 
            {
                if (movementLocked)
                {
                    if (!MyPlayer.IsDead && MeetingHud.Instance == null)
                        MyPlayer.VanillaPlayer.moveable = wasMoveable;
                    movementLocked = false;
                }

                tracker.KeepAsLongAsPossible = false;

                var target = riggingBody;
                riggingBody = null;
                if (target == null || !target.IsActive) return;
                if (MeetingHud.Instance != null) return;
                if (!btn.EffectTimer!.IsProgressing)
                {
                    // 处理完毕
                    DeadBody = target;
                    processedBody = target;
                    canDetonate = true;
                    rigLeftUses--;
                }
                btn.UpdateUsesIcon(rigLeftUses.ToString());
                btn.StartCoolDown();
            };
            rigButton.OnUpdate = button =>
            {
                UpdateRigIcon(DeadBody ?? riggingBody ?? tracker.CurrentTarget);

                if (canDetonate && DeadBody != null && DeadBody.IsActive)
                {
                    corpseArrow ??= new Arrow(null).SetColor(Virial.Color.Red).Register(this);
                    corpseArrow.IsActive = true;
                    corpseArrow.TargetPos = DeadBody.Position;
                }
                else if (corpseArrow != null)
                {
                    corpseArrow.IsActive = false;
                }

                if (canDetonate && (DeadBody == null || !DeadBody.IsActive))
                {
                    canDetonate = false;
                    DeadBody = null;
                }

                if (!button.IsInEffect) return;
                if (riggingBody == null || !riggingBody.IsActive)
                    button.InterruptEffect();
            };
            ModAbilityButton detonateButton;
            if (EnableWarning.GetValue() && WarningDuration.GetValue() > 0f)
            {
                var effectButton = NebulaAPI.Modules.EffectButton(
                    this, MyPlayer, VirtualKeyInput.SecondaryAbility, null,
                    DetonateCoolDown, WarningDuration.GetValue(), "DETONATE", detonateImage,
                    b => b.IsInEffect || CanDetonateNow());

                effectButton.OnClick = btn =>
                {
                    if (btn.IsInEffect)
                    {
                        btn.InterruptEffect();
                        return;
                    }

                    StartDetonation(btn, true);
                };
                effectButton.OnEffectEnd = btn =>
                {
                    if (btn.EffectTimer!.IsProgressing)
                    {
                        countingExp = false;
                        if (activeDetonationId != 0)
                            RpcCancel.Invoke(activeDetonationId);
                        activeDetonationId = 0;
                        btn.StartCoolDown();
                        return;
                    }

                    FinishDetonation(btn);
                };

                detonateButton = effectButton;
            }
            else
            {
                detonateButton = NebulaAPI.Modules.AbilityButton(
                    this, MyPlayer, VirtualKeyInput.SecondaryAbility, DetonateCoolDown, "DETONATE", detonateImage,
                    _ => CanDetonateNow());

                detonateButton.OnClick = btn => StartDetonation(btn, false);
            }

            var dragButton = NebulaAPI.Modules.EffectButton(
                this,
                MyPlayer,
                VirtualKeyInput.SidekickAction,
                null,
                DragCoolDown,
                DragDuration.GetValue(),
                "DRAG",
                dragImage,
                _ => MyPlayer.HoldingAnyDeadBody || (CanDragBody.GetValue() && tracker.CurrentTarget != null));

            dragButton.OnClick = btn =>
            {
                if (MyPlayer.HoldingAnyDeadBody)
                {
                    MyPlayer.ReleaseDeadBody();
                    if (btn.IsInEffect) btn.InterruptEffect();
                    return;
                }

                var body = tracker.CurrentTarget;
                if (body == null || !body.IsActive || body == processedBody) return;

                MyPlayer.HoldDeadBody(body);
                btn.StartEffect();
            };
            dragButton.OnEffectEnd = btn =>
            {
                if (MyPlayer.HoldingAnyDeadBody)
                    MyPlayer.ReleaseDeadBody();

                btn.StartCoolDown();
            };
        }
        bool CanDetonateNow()
        {
            return canDetonate && DeadBody != null && DeadBody.IsActive && InBodyPos();
        }
        int NextDetonationId()
        {
            return (MyPlayer.PlayerId << 24) | (++detonationCounter & 0xFFFFFF);
        }
        void CleanupDetonatedBody(Virial.Game.DeadBody body)
        {
            if (body.IsActive)
                AmongUsUtil.RpcCleanDeadBody(body, MyPlayer.PlayerId, null);
        }
        // 开始执行引爆
        void StartDetonation(ModAbilityButton button, bool useWarning)
        {
            if (!CanDetonateNow()) return;

            var body = DeadBody!;
            bool self = CanBoomOther.GetValue() is 0 or 1;
            bool mate = CanBoomOther.GetValue() is 0 or 2;
            var radius = ExplosionRadius.GetValue();
            float warningDuration = useWarning ? WarningDuration.GetValue() : 0f;
            bool leaveBody = LeaveBody.GetValue();
            int flags = (self ? 1 : 0) | (mate ? 2 : 0) | (useWarning ? 4 : 0) | (leaveBody ? 8 : 0);

            activeDetonationId = NextDetonationId();
            countingExp = true;
            expKills = 0;
            impostorsBeforeExplosion = GamePlayer.AllPlayers.Count(p => p.IsImpostor && !p.IsDead);
            Rpc炸炸炸.Invoke((body.Position, MyPlayer, flags, radius, warningDuration, activeDetonationId));

            NebulaManager.Instance.StartCoroutine(CoFinishExpCount(useWarning ? warningDuration+1.2f:1.2f).WrapToIl2Cpp());

            if (corpseArrow != null)
                corpseArrow.IsActive = false;

            if (useWarning)
            {
                button.StartEffect();
                return;
            }

            CleanupDetonatedBody(body);
            canDetonate = false;
            DeadBody = null;
            activeDetonationId = 0;
            button.StartCoolDown();
        }
        bool IsFirstRoundBombWin(GameEndEvent ev)
        {
            if (!firstRound || !usedBombInFirstRound) return false;
            if (ev.EndState == null) return false;
            if (ev.EndState.EndCondition != NebulaGameEnd.ImpostorWin) return false;
            if (ev.EndState.EndReason != GameEndReason.Situation) return false;
            if (Time.time - lastBombExplosionTime > 2.5f) return false;

            var last = NebulaGameManager.Instance?.LastDead;
            return last != null && (last == MyPlayer || last.MyKiller == MyPlayer);
        }
        System.Collections.IEnumerator CoFinishExpCount(float seconds)
        {
            yield return Effects.Wait(seconds);

            bool wasCounting = countingExp;
            countingExp = false;

            if (!wasCounting) yield break;

            if (MyPlayer.AmOwner && expKills >= 5)
                _ = new StaticAchievementToken("incinerator.kill5");

            int impostorsAfter = GamePlayer.AllPlayers.Count(p => p.IsImpostor && !p.IsDead);

            if (MyPlayer.AmOwner && impostorsBeforeExplosion > 1 && impostorsAfter == 0)
                _ = new StaticAchievementToken("incinerator.killAllImpostors");
        }
        void FinishDetonation(ModAbilityButton button)
        {
            if (DeadBody != null)
                CleanupDetonatedBody(DeadBody);

            canDetonate = false;
            DeadBody = null;
            activeDetonationId = 0;
            button.StartCoolDown();
        }
        void OnPlayerKillPlayer(PlayerKillPlayerEvent ev)
        {
            if (ev.Dead.PlayerState != Core.State.Boom) return;
            if (countingExp && ev.Dead != MyPlayer)
                expKills++;
            if (ev.WithDeadBody) return;

            var image = AshImage;
            if (image == null) return;

            var ash = UnityHelper.CreateObject<SpriteRenderer>("IncineratorAsh", null, ev.Dead.TruePosition.AsVector3(-1f));
            ash.sprite = image.GetSprite();
            ash.transform.localScale = UnityEngine.Vector3.one * 0.5f;
        }
        static readonly RemoteProcess<(VVector2 pos, GamePlayer bomber, int flags, float radius, float warningDuration, int id)> Rpc炸炸炸 = new(
            "tdgRpcBoom", (msg, _) =>
            {
                NebulaManager.Instance.StartCoroutine(
                    CoExplode(msg.pos, msg.bomber, msg.flags, msg.radius, msg.warningDuration, msg.id)
                        .WrapToIl2Cpp());
            }
        );
        static readonly RemoteProcess<int> RpcCancel = new(
            "tdgRpcBoomCancel", (id, _) => CanceledDetonations.Add(id));

        static System.Collections.IEnumerator CoExplode(VVector2 pos, GamePlayer bomber, int flags, float radius, float warningDuration, int id)
        {
            // 本地使用引爆
            bool self = (flags & 1) != 0;
            bool mate = (flags & 2) != 0;
            bool warning = (flags & 4) != 0;
            bool leaveBody = (flags & 8) != 0;

            if (CanceledDetonations.Contains(id))
            {
                CanceledDetonations.Remove(id);
                yield break;
            }

            if (MeetingHud.Instance != null)
            {
                CanceledDetonations.Remove(id);
                yield break;
            }

            var l = GamePlayer.LocalPlayer;

            if (warning && warningDuration > 0f)
            {
                float remain = warningDuration;
                bool flashed = false;
                while (remain > 0f)
                {
                    if (CanceledDetonations.Contains(id))
                    {
                        CanceledDetonations.Remove(id);
                        yield break;
                    }

                    if (MeetingHud.Instance != null)
                    {
                        CanceledDetonations.Remove(id);
                        yield break;
                    }

                    if (!flashed && l != null && !l.IsDead
                        && l.TruePosition.Distance(pos) <= radius)
                    {
                        AmongUsUtil.PlayQuickFlash(Virial.Color.Red);
                        DarkUtils.ShowTitle(Language.Translate("role.incinerator.warningtext"),Virial.Color.Red,warningDuration,true);
                        flashed = true;
                    }

                    remain -= Time.deltaTime;
                    yield return null;
                }
            }
            else if (warning && warningDuration <= 0f)
            {
                if (l != null && !l.IsDead && l.TruePosition.Distance(pos) <= radius)
                    AmongUsUtil.PlayQuickFlash(Virial.Color.Red);
            }

            if (CanceledDetonations.Contains(id) || MeetingHud.Instance != null)
            {
                CanceledDetonations.Remove(id);
                yield break;
            }

            CanceledDetonations.Remove(id);

            if (bomber.AmOwner)
            {
                _ = new StaticAchievementToken("incinerator.detonate1");

                if (firstRound)
                    usedBombInFirstRound = true;

                lastBombExplosionTime = Time.time;

                if (NebulaAPI.AmongUs.MapId == 2 &&
                    ShipStatus.Instance != null &&
                    ShipStatus.Instance.FastRooms.TryGetValue(SystemTypes.BoilerRoom, out var boilerRoom) &&
                    boilerRoom != null &&
                    boilerRoom.roomArea != null &&
                    boilerRoom.roomArea.OverlapPoint(pos))
                {
                    _ = new StaticAchievementToken("incinerator.polusBoiler");
                }
            }

            NebulaManager.Instance.StartCoroutine(CoPlayExplosion(pos, radius).WrapToIl2Cpp());

            var killParam = KillParameter.WithAssigningGhostRole | KillParameter.WithoutSelfSE;
            if (leaveBody)
                killParam |= KillParameter.WithDeadBody;

            if (l == null || l.IsDead) yield break;
            if (l.VanillaPlayer.inVent || l.IsDived) yield break;
            if (l.TruePosition.Distance(pos) > radius) yield break;

            if (l == bomber)
            {
                if (!self) yield break;
                if (bomber.AmOwner)
                    new StaticAchievementToken("incinerator.selfBoom");
                l.Suicide(Core.State.Boom, null, killParam);
                yield break;
            }

            if (l.IsImpostor && !mate) yield break;

            bomber.MurderPlayer(
                l,
                Core.State.Boom,
                null,
                killParam,
                KillCondition.TargetAlive | KillCondition.InTaskPhase);
        }
        bool InBodyPos()
        {
            if (!NeedBodyPos.GetValue()) return true;
            return DeadBody != null && DeadBody.IsActive && MyPlayer.Position.Distance(DeadBody.Position) <= 3f;
        }
        void UpdateRigIcon(Virial.Game.DeadBody? body)
        {
            if (rigIconHolder == null) return;

            var player = body?.Player;
            if (player == null)
            {
                rigIcon?.gameObject.SetActive(false);
                return;
            }

            if (rigIcon != null && rigIconPlayerId == player.PlayerId)
            {
                rigIcon.gameObject.SetActive(true);
                return;
            }

            if (rigIcon != null) UnityEngine.Object.Destroy(rigIcon.gameObject);

            rigIcon = AmongUsUtil.GetPlayerIcon(
                player.DefaultOutfit,
                rigIconHolder.transform,
                UnityEngine.Vector3.zero,
                UnityEngine.Vector3.one * 0.3f,
                false,
                false);
            rigIcon.ToggleName(false);
            rigIcon.SetAlpha(0.5f);
            rigIconPlayerId = player.PlayerId;
        }
        public static IDividedSpriteLoader ExplosionSprite = DividedSpriteLoader.FromResource("Nebula.Resources.ExplosionAnim.png", 120f, 4, 2);
        private static System.Collections.IEnumerator CoPlayExplosion(VVector2 pos, float radius)
        {
            NebulaAsset.PlaySE(NebulaAudioClip.ExplosionNear, pos, 5f, 5f);
            var explosion = UnityHelper.CreateObject<SpriteRenderer>("Explosion", null, pos.AsVector3(-10f));
            for (int i = 0; i < 8; i++)
            {
                var sprite = ExplosionSprite.GetSprite(i);
                explosion.sprite = sprite;
                float size = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                explosion.transform.localScale = UnityEngine.Vector3.one * (size > 0f ? radius * 2f / size : 1f);
                yield return Effects.Wait(0.12f);
            }
            UnityEngine.Object.Destroy(explosion.gameObject);
        }
    }

}
