using Dark.Scripts.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector3 = UnityEngine.Vector3;

namespace Dark.Scripts.Roles.Crewmates;
public class MaskedDancer : DefinedRoleTemplate, HasCitation, RuntimeAssignableGenerator<RuntimeRole>, IAssignableDocument, DefinedRole
{
    static readonly IntegerConfiguration MaxUses = NebulaAPI.Configurations.Configuration(
        "options.role.maskeddancer.maxuses",(1,10),2
        );
    static readonly FloatConfiguration InviteCooldown = NebulaAPI.Configurations.Configuration(
        "options.role.maskeddancer.invitecooldown", (0f, 60f, 2.5f), 25f,
        FloatConfigurationDecorator.Second, null, null);
    static readonly FloatConfiguration StartCooldown = NebulaAPI.Configurations.Configuration(
        "options.role.maskeddancer.startcooldown", (10f, 120f, 5f), 30f,
        FloatConfigurationDecorator.Second, null, null);
    static readonly ValueConfiguration<int> HOWTOKILLNEUS = NebulaAPI.Configurations.Configuration(
        "options.role.maskeddancer.neu", ["options.role.maskeddancer.neu.killrandomneu", "options.role.maskeddancer.neu.killrandomnoneu"],0
        );
    public MaskedDancer():base(
        "maskeddancer", new Virial.Color(0.5f, 0.8f, 1f), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [MaxUses, InviteCooldown,StartCooldown,HOWTOKILLNEUS]
        )
    {
        ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/MaskedDancer.png")?.AsImage(115f);
    }
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleIcon/MaskedDancerIcon.png")?.AsImage(115);
    public Citation? Citation => Core.Citations.Werewolf;
    public static readonly MaskedDancer MyRole = new();
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new AssignableDocumentImage(
            NebulaAPI.AddonAsset.GetResource("Buttons/Crewmates/MaskedDancerInvite.png")?.AsImage(100f),
            
            "role.maskeddancer.doc.invite");
        yield return new AssignableDocumentImage(
            NebulaAPI.AddonAsset.GetResource("Buttons/Crewmates/MaskedDancerStart.png")?.AsImage(100f),
            "role.maskeddancer.doc.start");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new AssignableDocumentReplacement("%USES%", MaxUses.GetValue().ToString());
        yield return new AssignableDocumentReplacement("%NEU%", HOWTOKILLNEUS.GetValue() == 0 ? Language.Translate("role.maskeddancer.doc.neu.0") : Language.Translate("role.maskeddancer.doc.neu.1")
            );
    }
    public RuntimeRole CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, IGameOperator
    {
        public Instance(GamePlayer player) : base(player)
        {
        }

        public DefinedRole Role => MyRole;
        int usesLeft;
        Image? InviteImage;
        Image? StartImage;
        bool CanStartParty = false;

        List<GamePlayer> InvitePlayers = new();
        List<PoolablePlayer> invitedIcons = new();
        Dictionary<byte, PoolablePlayer> iconDict = new();
        GameObject? iconHolder;
        void RuntimeAssignable.OnActivated()
        {
            if (!AmOwner) return;
            usesLeft = MaxUses;
            InvitePlayers.Clear();
            CanStartParty = false;

            invitedIcons.Clear();
            iconDict.Clear();
            InviteImage = NebulaAPI.AddonAsset.GetResource("Buttons/Crewmates/MaskedDancerInvite.png")?.AsImage(115);
            StartImage = NebulaAPI.AddonAsset.GetResource("Buttons/Crewmates/MaskedDancerStart.png")?.AsImage(115);
            var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
            playerTracker.SetColor(MyRole.RoleColor);
            var inviteBtn = NebulaAPI.Modules.AbilityButton(
                this,MyPlayer,VirtualKeyInput.Ability,InviteCooldown,"maskeddancer.InviteBtn",InviteImage, _=>playerTracker.CurrentTarget != null
                    && playerTracker.CurrentTarget != MyPlayer
                    && !InvitePlayers.Contains(playerTracker.CurrentTarget)
                    && InvitePlayers.Count < 3,_ => !MyPlayer.IsDead && usesLeft > 0
                );
            var startBtn = NebulaAPI.Modules.AbilityButton(
                this, MyPlayer, VirtualKeyInput.FixedAbility, StartCooldown, "maskeddancer.StartBtn", StartImage, _ => CanStartParty, _ => MyPlayer.IsAlive
                );
            startBtn.ShowUsesIcon(0, usesLeft.ToString());
            inviteBtn.OnClick = (button) =>
            {
                var v = playerTracker.CurrentTarget;
                if (v == null) return;
                var p = v.RealPlayer;
                if (p == null) return;
                if (InvitePlayers.Contains(p)) return;
                InvitePlayers.Add(p);
                // 开始左下角图标
                var pc = p.VanillaPlayer;
                var holder = LoadIconHolder();
                if (pc != null && holder != null)
                {
                    var icon = AmongUsUtil.GetPlayerIcon(
                        pc.Data.DefaultOutfit,
                        holder.transform,
                        Vector3.zero,
                        Vector3.one * 0.31f,
                        false, true
                    );
                    icon.ToggleName(false);
                    icon.SetAlpha(0.5f);
                    int i = InvitePlayers.Count - 1;
                    icon.transform.localPosition = new Vector3(i * 0.29f - 0.3f, -0.1f, -i * 0.01f);
                    invitedIcons.Add(icon);
                    iconDict[p.PlayerId] = icon;
                }
                button.StartCoolDown();
                UpdateIconsLayout();
            };
            startBtn.OnClick = (button) => 
            {
                if (InvitePlayers.Count != 3 || !CanStartParty) return;
                int c=0, i=0, n=0;
                foreach(var p in InvitePlayers)
                {
                    switch (p.Role.Role.Category)
                    {
                        case RoleCategory.CrewmateRole: c++; break;
                        case RoleCategory.ImpostorRole:i++;break;
                        case RoleCategory.NeutralRole:n++;break;
                    }
                }
                // 均有：随机死一个
                // 2狼：死剩下那一个
                // 2船：同上
                // 2中：根据设置，死剩下那一个或随机死一个中立。

                // 均有
                if (c == 1 && i == 1 && n == 1)
                {
                    var t = InvitePlayers[UnityEngine.Random.Range(0,3)];
                    InvokeExtra(t, InvitePlayers);
                }
                // 2狼
                else if (i == 2)
                {
                    var ts = InvitePlayers.Where(p => p.Role.Role.Category != RoleCategory.ImpostorRole).ToList();
                    var t = ts[0];
                    InvokeExtra(t, InvitePlayers);
                }
                // 2船
                else if (c == 2)
                {
                    var ts = InvitePlayers.Where(p=>p.Role.Role.Category != RoleCategory.CrewmateRole).ToList();
                    var t = ts[0];
                    InvokeExtra(t, InvitePlayers);
                }
                // 2中
                else if (n == 2)
                {
                    int htk = HOWTOKILLNEUS.GetValue();
                    if (htk == 0) // 击杀随机中立
                    {
                    var ts = InvitePlayers.Where(p => p.Role.Role.Category == RoleCategory.NeutralRole).ToList();

                        var t = ts[UnityEngine.Random.Range(0,ts.Count)];
                        InvokeExtra(t, InvitePlayers);
                    }
                    else // 击杀剩余
                    {
                        var ts = InvitePlayers.Where(p => p.Role.Role.Category != RoleCategory.NeutralRole).ToList();
                        var t = ts[0];
                        InvokeExtra(t,InvitePlayers);
                    }
                }
                else
                {
                    MyPlayer.Suicide(State.PartyAccident,null,KillParameter.NormalKill);
                }
                foreach (var icon in invitedIcons)
                    if (icon) GameObject.Destroy(icon.gameObject);
                invitedIcons.Clear();
                iconDict.Clear();
                InvitePlayers.Clear();
                CanStartParty = false;
                usesLeft--;
                button.UpdateUsesIcon(usesLeft.ToString());
            };
        }
        GameObject? LoadIconHolder()
        {
            if (iconHolder) return iconHolder;
            try
            {
                iconHolder = HudContent.InstantiateContent("MaskedDancerIcons", true, true).gameObject;
                this.BindGameObject(iconHolder);
            }
            catch (Exception ex)
            {
                DarkLog.LogError($"创建邀请头像容器失败：{ex}");
                iconHolder = null;
            }
            return iconHolder;
        }
        private void UpdateIconsLayout()
        {
            for (int i = 0; i < invitedIcons.Count; i++)
            {
                var icon = invitedIcons[i];
                if (icon) { icon.transform.localPosition = new Vector3(-0.5f + i * 0.35f, 0f, 0f); }
            }
        }
        void InvokeExtra(GamePlayer t,List<GamePlayer> ip)
        {
            t.Suicide(State.PartyAccident, null, KillParameter.NormalKill);
            var becauseWho = ip.Where(p => p.PlayerId != t.PlayerId).ToList(); // 绝对只剩下俩。
            if (becauseWho.Count != 2) return;
            var a = becauseWho[0];
            var b = becauseWho[1];
            RpcSys.RpcShareExtraInfo.Invoke((a, b, t));
        }
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (AmOwner && !MyPlayer.IsDead && InvitePlayers.Count == 3)
                CanStartParty = true;
            // 去掉死者。
            var deadList = InvitePlayers.Where(p => p.IsDead).ToList();
            foreach (var dead in deadList)
            {
                if (iconDict.TryGetValue(dead.PlayerId, out var icon))
                {
                    if (icon) GameObject.Destroy(icon.gameObject);
                    iconDict.Remove(dead.PlayerId);
                    invitedIcons.Remove(icon);
                }
                InvitePlayers.Remove(dead);
            }
            UpdateIconsLayout();
        }
    }
}
[NebulaRPCHolder]
static file class RpcSys
{
    record AccidentExtraDeadInfo(GamePlayer A,GamePlayer B) : GamePlayer.ExtraDeadInfo(Core.State.PartyAccident)
    {
        public override string ToStateText() => $"by {A.PlayerName} and {B.PlayerName}";
    }
    public static RemoteProcess<(GamePlayer a, GamePlayer b,GamePlayer c)> RpcShareExtraInfo = new("ShareExInfoMaskedDancer",
        (message, _) => {
            message.c.PlayerStateExtraInfo = new AccidentExtraDeadInfo(message.a,message.b);
        }
    );

}
