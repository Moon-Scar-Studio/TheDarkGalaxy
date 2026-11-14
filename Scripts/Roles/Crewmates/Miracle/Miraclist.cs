using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dark.Scripts.Core;

namespace Dark.Scripts.Roles.Crewmates.Miracle;

/*
 * 「你我所亲手缔造的奇迹」
神迹者 Half sugar's gift

你拥有「神迹」技能，可以在会议上对一人使用，使其成为「神谕者」。

固有能力【神迹创造】
你可以通过神视选择一人标记为「神谕者」。
此时，进入第一阶段，神谕者在你眼中名字后缀有金色的“O”。
如果神谕者进行了击杀、而且他不是船员，那么你会失去奇迹创造能力、成为船员。
当X轮后，且神谕者的阵营为船员，他的职业变更为「奇迹」。这时，他将会得知你的存在。
之后，你可以使用「命途编织」为其重新更改一次职业。更改后，你的职业公开，且不可被赌。
奇迹可以选择一位“神种”，当神种或奇迹死亡，你会根据房主设置获得提示。

神视
在会议上选择一人标记为「神谕者」

*role.miraclist.doc.FateWeave

提示
· 命途只能选择船员职业。
 */
// 神迹者
public class Miraclist : DefinedRoleTemplate, RuntimeAssignableGenerator<RuntimeRole>, HasCitation, IAssignableDocument, DefinedRole
{
    // 有没有私聊频道
    static readonly BoolConfiguration HasPrivateChat = NebulaAPI.Configurations.Configuration("options.role.miraclist.HasPrivateChat", true);

    // 奇迹能不能招募神种
    public static readonly BoolConfiguration CanChooseSeed = NebulaAPI.Configurations.Configuration("options.role.miraclist.CanChooseSeed", true);
    static readonly IntegerConfiguration NeedMeetingCount = NebulaAPI.Configurations.Configuration("options.role.miraclist.NeedMeetingCount", (1, 10, 1), 1);
    static readonly BoolConfiguration CantBeGuess = NebulaAPI.Configurations.Configuration("options.role.miraclist.CantBeGuess", true);
    static readonly BoolConfiguration SholdPublicRole = NebulaAPI.Configurations.Configuration("options.role.miraclist.PublicRole", true);
    static readonly FloatConfiguration FateWeaveCoolDown = NebulaAPI.Configurations.Configuration("options.role.miraclist.FateWeaveCD", (0, 60, 3.5f), 15);
    static readonly ValueConfiguration<int> BeMiracleType = NebulaAPI.Configurations.Configuration("options.role.miraclist.BeMiracleType", ["options.role.miraclist.BeMiracleType.DoAllTasks", "options.role.miraclist.BeMiracleType.MeetingCount"], 0);
    static readonly ValueConfiguration<int> DivineSightType = NebulaAPI.Configurations.Configuration("options.role.miraclist.DivineSightType", ["options.role.miraclist.DivineSightType.Meeting", "options.role.miraclist.DivineSightType.Normal"], 0);
    public Miraclist() : base("miraclist", Cor.Golden, RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [HasPrivateChat, CanChooseSeed, CantBeGuess, SholdPublicRole, FateWeaveCoolDown, NeedMeetingCount, BeMiracleType])
    {

    }
    public Citation? Citation => Core.Citations.TheDarkGalaxy;
    public static readonly Miraclist MyRole = new();
    public RuntimeRole CreateInstance(GamePlayer player, int[] arguments)
    {
        return new Instance(player);
    }
    enum BeMiracleTypes
    {
        DoAllTasks,
        Meeting
    }
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, IGameOperator
    {
        public Instance(GamePlayer player) : base(player)
        {
        }

        public DefinedRole Role => MyRole;
        Image? fateWeave;
        Image? DivineSightImg;
        bool canUseFateWeave;
        GamePlayer _oraclistPlayer;
        int meetingCount;
        public void OnActivated()
        {
            if (!AmOwner) return;
            fateWeave = DarkUtils.AsImage("Crewmates/FateWeave.png");
            meetingCount = 0;
            canUseFateWeave = false;
            _oraclistPlayer = null!;
            var fateWeaveBtn = NebulaAPI.Modules.AbilityButton(this, MyPlayer, VirtualKeyInput.Ability, FateWeaveCoolDown, "FATE", fateWeave, (button) => canUseFateWeave);
            if (DivineSightType.GetValue() != 1) 
            {
                DivineSightImg = DarkUtils.AsImage("Crewmates/DivineSightMeeting.png");
                return;
            }
            DivineSightImg = DarkUtils.AsImage("Crewmates/DivineSightNormal.png");
            var divineSightBtn = NebulaAPI.Modules.EffectButton(this,MyPlayer,VirtualKeyInput.FixedAbility,1,1,"A",DivineSightImg);
            
        }
        [Local]
        public void OnMeetingStart(MeetingStartEvent ev)
        {
            var buttonManager = NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>();
            buttonManager?.RegisterMeetingAction(new(
                DivineSightImg,
                state =>
                {
                    var target = state.MyPlayer;
                    if (target == null || target == MyPlayer) return; 
                    _oraclistPlayer = target;
                    target.AddModifier(Oraclist.MyRole); // 给神谕
                    AmongUsUtil.PlayQuickFlash(Cor.Golden);
                },p => p.MyPlayer != MyPlayer && !p.MyPlayer.IsDead
            ));
        }
        // 按轮次算的
        [Local]
        public void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (!HasOraclist()) return;
            if (r() != BeMiracleTypes.Meeting) return;
            if (meetingCount == NeedMeetingCount) 
            {
                _oraclistPlayer.AddModifier(Oraclist.MyRole);
                return;
            }
            meetingCount++; 
        }
        // 按任务算
        [Local]
        public void DoAllTask(PlayerTaskCompleteEvent ev)
        {
            if (!HasOraclist()) return;
            if (ev.Player != _oraclistPlayer) return;
            if (!ev.Player.Tasks.IsCompletedTotalTasks) return;
            _oraclistPlayer.AddModifier(Oraclist.MyRole);
        }
        public void Deco27(PlayerDecorateNameEvent ev)
        {
            if (!AmOwner) return;
            
            if (ev.Player.TryGetModifier<Oraclist.Instance>(out var _))
            {
                // 是神谕，加后缀
                ev.Name += "<color=#D4AF37>O</color>";
            }
        }
        [Local]
        public void PlayerKillEvent(PlayerKillPlayerEvent ev)
        {
            if(ev.Player == _oraclistPlayer)
            {
                _oraclistPlayer.RemoveModifier(Oraclist.MyRole);
                _oraclistPlayer = null!;
                AmongUsUtil.PlayCustomFlash(Cor.Golden,0.2f,3,0.6f);
                MyPlayer.SetRole(Crewmate.MyRole);
            }
        }
        bool HasOraclist() => _oraclistPlayer != null;
        BeMiracleTypes r() => BeMiracleType.GetValue() == 0 ? BeMiracleTypes.DoAllTasks : BeMiracleTypes.Meeting;
    }
}

// 神谕者
public class Oraclist : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier
{
    public Oraclist() : base("oraclist","oraclist", Cor.Golden) { }

    public Citation? Citation => Core.Citations.TheDarkGalaxy;
    bool ISpawnable.IsSpawnable => false;
    public RuntimeModifier CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    
    public static Oraclist MyRole = new();
    public class Instance: RuntimeAssignableTemplate, RuntimeModifier
    {
        bool RuntimeAssignable.CanBeAwareAssignment => false;
        public Instance(GamePlayer player) : base(player)
        {
        }

        public DefinedModifier Modifier => MyRole;

        public void OnActivated()
        {
        }
    }
}
// 奇迹
public class Miracle: DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier
{
    public Miracle() : base("miracle","miracle",Cor.Golden) { }

    public Citation? Citation => Core.Citations.TheDarkGalaxy;
    bool ISpawnable.IsSpawnable => false;
    public RuntimeModifier CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public static Miracle MyRole = new();
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        bool RuntimeAssignable.CanBeAwareAssignment => false;
        public Instance(GamePlayer player) : base(player)
        {
        }

        public DefinedModifier Modifier => MyRole;

        public void OnActivated()
        {
        }
    }
}
// 神种
public class DivineSeed: DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier
{
    public DivineSeed() : base("divineseed","ds",Cor.Golden) { }

    public Citation? Citation => Core.Citations.TheDarkGalaxy;
    bool ISpawnable.IsSpawnable => false;
    public RuntimeModifier CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public static DivineSeed MyRole = new();
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        bool RuntimeAssignable.CanBeAwareAssignment => false;
        public Instance(GamePlayer player) : base(player)
        {
        }

        public DefinedModifier Modifier => MyRole;

        public void OnActivated()
        {
        }
    }
}