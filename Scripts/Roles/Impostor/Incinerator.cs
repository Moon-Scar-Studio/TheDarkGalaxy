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

    static readonly BoolConfiguration CanHoldBody = NebulaAPI.Configurations.Configuration("options.role.incinerator.CanHoldBody",true);
    static readonly FloatConfiguration ExplosionRadius = NebulaAPI.Configurations.Configuration("options.role.incinerator.ExplosionRadius",(1f,10f,0.5f),3f);
    static readonly BoolConfiguration EnableWarning = NebulaAPI.Configurations.Configuration("options.role.incinerator.EnableWarning",true);
    static readonly FloatConfiguration WarningDuration = NebulaAPI.Configurations.Configuration("options.role.incinerator.WarningDuration",(0f,10f,0.5f),2f,FloatConfigurationDecorator.Second);
    public Citation? Citation => Core.Citations.TheDarkGalaxy_GoodEndGambler;

    public Incinerator() : base("incinerator", Virial.Color.ImpostorColor, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [RigCoolDown,RigMaxUses,CanBoomOther,NeedBodyPos,DetonateCoolDown,CantMove,RigTime, CanHoldBody, ExplosionRadius, EnableWarning, WarningDuration])
    {

    }
    public RuntimeRole CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public static readonly Incinerator MyRole = new();
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
        Virial.Game.DeadBody? DeadBody;
        public void OnActivated()
        {
            if (!AmOwner) return;

            rigLeftUses = RigMaxUses;
            canDetonate = false;

            rigImage = DarkUtils.AsImage("Buttons/Impostor/IncineratorRig.png",115);
            detonateImage = DarkUtils.AsImage("Buttons/Impostor/IncineratorDetonate.png", 115);
            dragImage = DarkUtils.AsImage("Buttons/Impostor/IncineratorDrag.png", 115);

            var tracker = ObjectTrackers.ForDeadBody(
                this,null,MyPlayer,body=>body.IsActive && body.CurrentHolder == null && !body.IsFullyDissolved,color:UnityEngine.Color.red
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
                    DeadBody = target;
                    canDetonate = true;
                    rigLeftUses--;
                }
                btn.UpdateUsesIcon(rigLeftUses.ToString());
                btn.StartCoolDown();
            };
            rigButton.OnUpdate = button =>
            {
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
            var detonateButton = NebulaAPI.Modules.AbilityButton(
                this, MyPlayer, VirtualKeyInput.SecondaryAbility, DetonateCoolDown, "DETONATE", detonateImage, (button) => canDetonate && DeadBody != null && DeadBody.IsActive && InBodyPos()
                );
            detonateButton.OnClick = (btn) =>
            {
                if (!canDetonate || DeadBody == null || !DeadBody.IsActive || !InBodyPos()) return;

                bool self = CanBoomOther.GetValue() is 0 or 1;
                bool mate = CanBoomOther.GetValue() is 0 or 2;
                bool warning = EnableWarning.GetValue();
                float warningDuration = WarningDuration.GetValue();
                var body = DeadBody;
                var pos = body.Position;
                var radius = ExplosionRadius.GetValue();

                Rpc炸炸炸.Invoke((pos, MyPlayer, self, mate, radius, warning, warningDuration));

                if (corpseArrow != null)
                    corpseArrow.IsActive = false;

                if (body.IsActive)
                {
                    if (warning && warningDuration > 0f)
                    {
                        NebulaManager.Instance.StartDelayAction(warningDuration, () =>
                        {
                            if (body.IsActive)
                                AmongUsUtil.RpcCleanDeadBody(body, MyPlayer.PlayerId, null);
                        });
                    }
                    else
                    {
                        AmongUsUtil.RpcCleanDeadBody(body, MyPlayer.PlayerId, null);
                    }
                }

                canDetonate = false;
                DeadBody = null;

                btn.StartCoolDown();
            };

            var dragButton = NebulaAPI.Modules.AbilityButton(
                this,
                MyPlayer,
                VirtualKeyInput.SidekickAction,
                0f,
                "DRAG",
                dragImage,
                _ => MyPlayer.HoldingAnyDeadBody || CanHoldBody.GetValue() && tracker.CurrentTarget != null);

            dragButton.OnClick = _ =>
            {
                if (MyPlayer.HoldingAnyDeadBody)
                {
                    MyPlayer.ReleaseDeadBody();
                    return;
                }

                var body = tracker.CurrentTarget;
                if (body != null && body.IsActive)
                    MyPlayer.HoldDeadBody(body);
            };
        }
        static readonly RemoteProcess<(VVector2 pos, GamePlayer bomber, bool self, bool mate, float radius, bool warning, float warningDuration)> Rpc炸炸炸 = new(
            "tdgRpcBoom", (msg, _) =>
            {
                NebulaManager.Instance.StartCoroutine(
                    CoExplode(msg.pos, msg.bomber, msg.self, msg.mate, msg.radius, msg.warning, msg.warningDuration)
                        .WrapToIl2Cpp());
            }
        );

        static System.Collections.IEnumerator CoExplode(VVector2 pos, GamePlayer bomber, bool self, bool mate, float radius, bool warning, float warningDuration)
        {
            if (MeetingHud.Instance != null) yield break;

            var l = GamePlayer.LocalPlayer;

            if (warning && warningDuration > 0f)
            {
                float remain = warningDuration;
                bool flashed = false;
                while (remain > 0f)
                {
                    if (MeetingHud.Instance != null) yield break;

                    if (!flashed && l != null && !l.IsDead
                        && l.TruePosition.Distance(pos) <= radius)
                    {
                        AmongUsUtil.PlayQuickFlash(Virial.Color.Red);
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

            if (MeetingHud.Instance != null) yield break;

            NebulaManager.Instance.StartCoroutine(CoPlayExplosion(pos).WrapToIl2Cpp());

            if (l == null || l.IsDead) yield break;
            if (l.VanillaPlayer.inVent || l.IsDived) yield break;
            if (l.TruePosition.Distance(pos) > radius) yield break;

            if (l == bomber)
            {
                if (!self) yield break;
                l.Suicide(Core.State.Boom, null, KillParameter.WithDeadBody);
                yield break;
            }

            if (l.IsImpostor && !mate) yield break;

            bomber.MurderPlayer(
                l,
                Core.State.Boom,
                null,
                KillParameter.WithAssigningGhostRole | KillParameter.WithoutSelfSE | KillParameter.WithDeadBody,
                KillCondition.TargetAlive | KillCondition.InTaskPhase);
        }
        bool InBodyPos()
        {
            if (!NeedBodyPos.GetValue()) return true;
            return DeadBody != null && DeadBody.IsActive && MyPlayer.Position.Distance(DeadBody.Position) <= 3f;
        }
        public static IDividedSpriteLoader ExplosionSprite = DividedSpriteLoader.FromResource("Nebula.Resources.ExplosionAnim.png", 120f, 4, 2);
        private static System.Collections.IEnumerator CoPlayExplosion(VVector2 pos)
        {
            NebulaAsset.PlaySE(NebulaAudioClip.ExplosionNear, pos, 5f, 5f);
            var explosion = UnityHelper.CreateObject<SpriteRenderer>("Explosion", null, pos.AsVector3(-10f));
            for (int i = 0; i < 8; i++)
            {
                explosion.sprite = ExplosionSprite.GetSprite(i);
                yield return Effects.Wait(0.12f);
            }
            UnityEngine.Object.Destroy(explosion.gameObject);
        }
    }

}
