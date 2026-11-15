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
此时，进入第一阶段，神谕者在你眼中名字后缀有金色的“O”。  ok 
如果神谕者进行了击杀、而且他不是船员，那么你会失去奇迹创造能力、成为船员。  ok
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
    internal static readonly BoolConfiguration HasPrivateChat = NebulaAPI.Configurations.Configuration("options.role.miraclist.HasPrivateChat", true);

    
    static readonly IntegerConfiguration NeedMeetingCount = NebulaAPI.Configurations.Configuration("options.role.miraclist.NeedMeetingCount", (1, 10, 1), 1);
    static readonly BoolConfiguration CantBeGuess = NebulaAPI.Configurations.Configuration("options.role.miraclist.CantBeGuess", true);
    static readonly BoolConfiguration SholdPublicRole = NebulaAPI.Configurations.Configuration("options.role.miraclist.PublicRole", true);
    static readonly FloatConfiguration FateWeaveCoolDown = NebulaAPI.Configurations.Configuration("options.role.miraclist.FateWeaveCD", (0, 60, 3.5f), 15);
    static readonly ValueConfiguration<int> BeMiracleType = NebulaAPI.Configurations.Configuration("options.role.miraclist.BeMiracleType", ["options.role.miraclist.BeMiracleType.DoAllTasks", "options.role.miraclist.BeMiracleType.MeetingCount"], 0);
    public Miraclist() : base("miraclist", Cor.MiraclistGold, RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [HasPrivateChat, CantBeGuess, SholdPublicRole, FateWeaveCoolDown, NeedMeetingCount, BeMiracleType])
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [Miracle.MyRole.ConfigurationHolder!,DivineSeed.MyRole.ConfigurationHolder!]);
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
            MiraclistChatUtil.Register(
                MyPlayer.PlayerId,
                this,
                () => MyPlayer.Role.Role == MyRole);
            DivineSightImg = DarkUtils.AsImage("Crewmates/DivineSightMeeting.png");
            return;
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
                _oraclistPlayer.AddModifier(Miracle.MyRole);
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
            _oraclistPlayer.AddModifier(Miracle.MyRole);
        }
        public void Deco27(PlayerDecorateNameEvent ev)
        {
            if (!AmOwner) return;
            
            if (ev.Player.TryGetModifier<Oraclist.Instance>(out var _))
            {
                // 是神谕，加后缀
                ev.Name += "<color=#D4AF37>O</color>";
            }
            if (ev.Player.TryGetModifier<Miracle.Instance>(out var _)) ev.Name = ColorUtils.CreateGradient(ev.Name, new UnityEngine.Color(0.72f, 0.42f, 0.05f),new UnityEngine.Color(1.00f, 0.93f, 0.55f));
            if (ev.Player.TryGetModifier<DivineSeed.Instance>(out var _)) ev.Name = ColorUtils.CreateGradient(ev.Name, new UnityEngine.Color(0.72f, 0.42f, 0.05f), new UnityEngine.Color(1.00f, 0.93f, 0.55f));
        }
        [Local]
        public void PlayerKillEvent(PlayerKillPlayerEvent ev)
        {
            if (ev.Player.Role.Role.Category == RoleCategory.CrewmateRole) return;
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
        public static bool IsRelated(GamePlayer p) => p.Role.Role == MyRole || p.TryGetModifier<Oraclist.Instance>(out _) || p.TryGetModifier<Miracle.Instance>(out _) || p.TryGetModifier<DivineSeed.Instance>(out _);
        
    }
}

file static class MiraclistChatUtil
{
    public static void Register(int ownerId, ILifespan lifespan, Func<bool> localVisible)
    {
        if (!Miraclist.HasPrivateChat.GetValue() || ownerId is < 0 or > 255)
            return;

        PrivateChatUtil.Register(
            Cor.OtherMiracleGold,
            Cor.MiraclistGold,
            $"miraclist_chat_{ownerId}",
            "miraclist.ChatChannel",
            lifespan,
            () => MeetingHud.Instance != null
                && Miraclist.HasPrivateChat.GetValue()
                && OwnerValid(ownerId)
                && localVisible(),
            (sender, receiver) => CanReceive(ownerId, sender, receiver));
    }

    static bool OwnerValid(int ownerId)
    {
        var owner = GamePlayer.GetPlayer((byte)ownerId);
        return owner != null && owner.Role.Role == Miraclist.MyRole;
    }

    static bool CanReceive(int ownerId, GamePlayer sender, GamePlayer receiver)
    {
        if (!Miraclist.HasPrivateChat.GetValue() || !OwnerValid(ownerId))
            return false;

        return sender != receiver
            && IsParticipant(ownerId, sender)
            && IsParticipant(ownerId, receiver);
    }

    static bool IsParticipant(int ownerId, GamePlayer player)
    {
        if (player.PlayerId == ownerId && player.Role.Role == Miraclist.MyRole)
            return true;

        if (player.TryGetModifier<Miracle.Instance>(out var miracle)
            && miracle.MiraclistId == ownerId)
            return true;

        if (DivineSeed.HasPrivateChat.GetValue()
            && player.TryGetModifier<DivineSeed.Instance>(out var seed)
            && seed.MiraclistId == ownerId)
            return true;

        return false;
    }
}

// 神谕者
public class Oraclist : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier
{
    public Oraclist() : base("oraclist","oraclist", Cor.OtherMiracleGold) { }

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
    public Miracle() : base("miracle", "miracle", Cor.OtherMiracleGold, [CanChooseSeed,TellMiraclist]) 
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [Miraclist.MyRole.ConfigurationHolder!, DivineSeed.MyRole.ConfigurationHolder!]);
    }
    static readonly BoolConfiguration CanChooseSeed = NebulaAPI.Configurations.Configuration("options.role.miraclist.CanChooseSeed", true);
    static readonly BoolConfiguration TellMiraclist = NebulaAPI.Configurations.Configuration("options.Miracle.TellMiraclist", true);
    public Citation? Citation => Core.Citations.TheDarkGalaxy;
    bool ISpawnable.IsSpawnable => false;
    public RuntimeModifier CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments != null && arguments.Length > 0 ? arguments[0] : -1);
    public static Miracle MyRole = new();
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        bool RuntimeAssignable.CanBeAwareAssignment => false;
        public int MiraclistId { get; }
        public Instance(GamePlayer player, int miraclistId) : base(player)
        {
            MiraclistId = miraclistId;
        }

        public DefinedModifier Modifier => MyRole;

        public void OnActivated()
        {
            MiraclistChatUtil.Register(
                MiraclistId,
                this,
                () => MyPlayer.TryGetModifier<Miracle.Instance>(out var miracle)
                    && miracle.MiraclistId == MiraclistId);
        }
    }
}
// 神种
public class DivineSeed: DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier
{
    public DivineSeed() : base("divineseed", "ds", Cor.OtherMiracleGold, [HasPrivateChat, TellMiraclist]) 
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [Miracle.MyRole.ConfigurationHolder!, Miraclist.MyRole.ConfigurationHolder!]);
    }
    public static readonly BoolConfiguration HasPrivateChat = NebulaAPI.Configurations.Configuration("options.DivineSeed.HasPrivateChat",true);
    static readonly BoolConfiguration TellMiraclist = NebulaAPI.Configurations.Configuration("options.DivineSeed.TellMiraclist",true);
    public Citation? Citation => Core.Citations.TheDarkGalaxy;
    bool ISpawnable.IsSpawnable => false;
    public RuntimeModifier CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments != null && arguments.Length > 0 ? arguments[0] : -1);
    public static DivineSeed MyRole = new();
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        bool RuntimeAssignable.CanBeAwareAssignment => false;
        public int MiraclistId { get; }
        public Instance(GamePlayer player, int miraclistId) : base(player)
        {
            MiraclistId = miraclistId;
        }

        public DefinedModifier Modifier => MyRole;

        public void OnActivated()
        {
            if (!DivineSeed.HasPrivateChat.GetValue()) return;

            MiraclistChatUtil.Register(
                MiraclistId,
                this,
                () => DivineSeed.HasPrivateChat.GetValue()
                    && MyPlayer.TryGetModifier<DivineSeed.Instance>(out var seed)
                    && seed.MiraclistId == MiraclistId);
        }
    }
}