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

你拥有「神迹」技能，可以在会议上对一人使用，使其成为「奇迹之人」。

固有能力【神迹创造】
你可以通过神视选择一人标记为「奇迹之人」。
此时，进入第一阶段，奇迹之人在你眼中会有M-T的后缀。
如果奇迹之人进行了击杀、而且他不是船员，那么你会失去奇迹创造能力、成为船员。 果然，所谓奇迹......
当奇迹之人任务全部完成后、且他为船员阵营，他的职业变更为「奇迹」。这时，他将会得知你的存在。
之后，你可以使用「命途编织」为其重新更改一次职业。更改后，你的职业公开，且不可被赌。
奇迹可以选择一位“神种”，当神种或奇迹死亡，你会根据房主设置获得提示。

神视
在会议上选择一人标记为「奇迹之人」

*role.miraclist.doc.FateWeave

提示
· 命途只能选择船员职业。
 */
// 神迹者
public class Miraclist : DefinedRoleTemplate, RuntimeAssignableGenerator<RuntimeRole>, HasCitation, IAssignableDocument,DefinedRole
{
    // 有没有私聊频道
    static readonly BoolConfiguration HasPrivateChat = NebulaAPI.Configurations.Configuration("options.role.miraclist.HasPrivateChat",true);

    // 奇迹能不能招募神种
    public static readonly BoolConfiguration CanChooseSeed = NebulaAPI.Configurations.Configuration("options.role.miraclist.CanChooseSeed",true);

    static readonly BoolConfiguration CantBeGuess = NebulaAPI.Configurations.Configuration("options.role.miraclist.CantBeGuess",true);
    static readonly BoolConfiguration SholdPublicRole = NebulaAPI.Configurations.Configuration("options.role.miraclist.PublicRole",true);
    static readonly FloatConfiguration FateWeaveCoolDown = NebulaAPI.Configurations.Configuration("options.role.miraclist.FateWeaveCD",(0,60,3.5f),15);
    public Miraclist() : base("miraclist", Cor.Golden, RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [HasPrivateChat, CanChooseSeed, CantBeGuess, SholdPublicRole, FateWeaveCoolDown])
    {

    }
    public Citation? Citation => throw new NotImplementedException();
    public static readonly Miraclist MyRole = new();
    public RuntimeRole CreateInstance(GamePlayer player, int[] arguments)
    {
        return new Instance(player);
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
        public void OnActivated()
        {
            fateWeave = DarkUtils.AsImage("Crewmates/FateWeave.png");
            canUseFateWeave = false;
            var fateWeaveBtn = NebulaAPI.Modules.AbilityButton(this,MyPlayer,VirtualKeyInput.Ability,FateWeaveCoolDown,"FATE",fateWeave,(button)=>canUseFateWeave);
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


                },p => p.MyPlayer != MyPlayer
            ));
        }
    }
}

// 奇迹之人
public class MiracleTouched: DefinedModifierTemplate, HasCitation, DefinedModifier, RuntimeAssignableGenerator<RuntimeModifier>
{
    public MiracleTouched() : base("miracletouched",Cor.Golden) { }

    public Citation? Citation => Core.Citations.TheDarkGalaxy;

    public RuntimeModifier CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public static MiracleTouched MyRole = new();
    public class Instance: RuntimeAssignableTemplate, RuntimeModifier
    {
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
public class Miracle: DefinedModifierTemplate, HasCitation, DefinedModifier, RuntimeAssignableGenerator<RuntimeModifier>
{
    public Miracle() : base("miracle",Cor.Golden) { }

    public Citation? Citation => Core.Citations.TheDarkGalaxy;

    public RuntimeModifier CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public static Miracle MyRole = new();
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
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
public class DivineSeed: DefinedModifierTemplate, HasCitation, DefinedModifier, RuntimeAssignableGenerator<RuntimeModifier>
{
    public DivineSeed() : base("divineseed",Cor.Golden) { }

    public Citation? Citation => Core.Citations.TheDarkGalaxy;

    public RuntimeModifier CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public static DivineSeed MyRole = new();
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        public Instance(GamePlayer player) : base(player)
        {
        }

        public DefinedModifier Modifier => MyRole;

        public void OnActivated()
        {
        }
    }
}