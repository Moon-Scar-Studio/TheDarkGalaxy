using Dark.Scripts.Core;

namespace Dark.Scripts.Roles.Crewmates.Miracle;

/*
 * 「你我所亲手缔造的奇迹」
 * 神迹者 Half sugar's gift
 *
 * 你拥有「神迹」技能，可以在会议上对一人使用，使其成为「神谕者」。
 *
 * 固有能力【神迹创造】
 * 你可以通过神视选择一人标记为「神谕者」。
 * 此时，进入第一阶段，神谕者在你眼中名字后缀有金色的“O”。
 * 如果神谕者进行了击杀、而且他不是船员，那么你会失去奇迹创造能力、成为船员。
 * 当 X 轮后，且神谕者的阵营为船员，他的职业变更为「奇迹」。这时，他将会得知你的存在。
 * 之后，你可以使用「命途编织」为其重新更改一次职业。更改后，你的职业公开，且不可被赌。
 * 奇迹可以选择一位“神种”，当神种或奇迹死亡，你会根据房主设置获得提示。
 *
 * 神视
 * 在会议上选择一人标记为「神谕者」
 *
 * 提示
 * · 命途只能选择船员职业。
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
    static readonly BoolConfiguration NeedCrew = NebulaAPI.Configurations.Configuration("options.role.miraclist.NeedCrew", true);
    static readonly BoolConfiguration PreventDemotion = NebulaAPI.Configurations.Configuration("options.role.miraclist.PreventDemotion", false);
    static readonly SimpleRoleFilterConfiguration RoleFilter = new("options.role.miraclist.RoleFilter")
    {
        InvertOption = true,
        PreviewOnlySpawnableRoles = true,
        RolePredicate = r => r.Category == RoleCategory.CrewmateRole
    };
    static readonly IntegerConfiguration RoleCount = NebulaAPI.Configurations.Configuration("options.role.miraclist.RoleCount",(1,15,1),5);
    public Miraclist() : base("miraclist", Cor.MiraclistGold, RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [RoleCount,HasPrivateChat, CantBeGuess, SholdPublicRole, FateWeaveCoolDown, NeedMeetingCount, BeMiracleType, NeedCrew, PreventDemotion, RoleFilter])
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [Miracle.MyRole.ConfigurationHolder!, DivineSeed.MyRole.ConfigurationHolder!]);
        ConfigurationHolder!.Illustration = DarkUtils.AsImage("RoleImage/Miraclist.png");
    }
    public Citation? Citation => Core.Citations.TheDarkGalaxy_hvtXsvc;
    Image? DefinedAssignable.IconImage => DarkUtils.AsImage("RoleIcon/MiraclistIcon.png", 115f);
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new AssignableDocumentImage(
            DarkUtils.AsImage("Buttons/Crewmates/DivineSightMeeting.png"),
            "role.miraclist.doc.divinesight");
        yield return new AssignableDocumentImage(
            DarkUtils.AsImage("Buttons/Crewmates/FateWeave.png"),
            "role.miraclist.doc.fateweave");
    }
    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new AssignableDocumentReplacement("%MEETING%", NeedMeetingCount.GetValue().ToString());
        yield return new AssignableDocumentReplacement("%COUNT%", RoleCount.GetValue().ToString());
        yield return new AssignableDocumentReplacement("%CONDITION%", BeMiracleType.GetValue() == 0
            ? Language.Translate("role.miraclist.doc.condition.tasks")
            : Language.Translate("role.miraclist.doc.condition.meeting").Replace("%MEETING%", NeedMeetingCount.GetValue().ToString()));
        yield return new AssignableDocumentReplacement("%PUBLIC%", Language.Translate(SholdPublicRole.GetValue() ? "role.miraclist.doc.public.true" : "role.miraclist.doc.public.false"));
        yield return new AssignableDocumentReplacement("%PENALTY%", Language.Translate(PreventDemotion.GetValue() ? "role.miraclist.doc.penalty.false" : "role.miraclist.doc.penalty.true"));
        yield return new AssignableDocumentReplacement("%SEED%", Language.Translate(Miracle.CanChooseSeedOption.GetValue() ? "role.miraclist.doc.seed.true" : "role.miraclist.doc.seed.false"));
        yield return new AssignableDocumentReplacement("%SEEDNOTIFY%", Language.Translate(DivineSeed.TellMiraclistOption.GetValue() ? "role.miraclist.doc.seednotify.true" : "role.miraclist.doc.seednotify.false"));
        yield return new AssignableDocumentReplacement("%MIRACLENOTIFY%", Language.Translate(Miracle.TellMiraclistOption.GetValue() ? "role.miraclist.doc.miraclenotify.true" : "role.miraclist.doc.miraclenotify.false"));
    }
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
        GamePlayer _miraclePlayer;
        int meetingCount;
        bool fateHasChangedRole;
        static HashSet<DefinedRole> miracleRoleCache;
        public void OnActivated()
        {
            if (!AmOwner) return;
            fateWeave = DarkUtils.AsImage("Buttons/Crewmates/FateWeave.png") ?? DarkUtils.AsImage("Buttons/Crewmates/MaskedDancerStart.png");
            miracleRoleCache = [];
            fateHasChangedRole = false;
            meetingCount = 0;
            canUseFateWeave = false;
            _oraclistPlayer = null!;
            _miraclePlayer = null!;
            var fateWeaveBtn = NebulaAPI.Modules.AbilityButton(this, MyPlayer, VirtualKeyInput.Ability, FateWeaveCoolDown, "FATE", fateWeave, _ => canUseFateWeave && _miraclePlayer != null && !MyPlayer.IsDead);
            MiraclistChatUtil.Register(MyPlayer.PlayerId, this, () => MyPlayer.Role.Role == MyRole || MyPlayer.Role.Role == PublicMiraclist.MyRole);
            DivineSightImg = DarkUtils.AsImage("Buttons/Crewmates/DivineSightMeeting.png") ?? DarkUtils.AsImage("Buttons/Crewmates/MaskedDancerInvite.png");
            fateWeaveBtn.OnClick = btn =>
            {
                if (!canUseFateWeave || _miraclePlayer == null) return;
                if (OpenFateScreen() != null) btn.StartCoolDown();
            };
        }
        MetaScreen? OpenFateScreen()
        {
            MetaScreen screen = null!;
            // 应为：选择器排除的、不可被分配的、不可被赌的不出现，然后固定X个随机取
            HashSet<DefinedRole> shown = miracleRoleCache.Count == 0 ? SelectRoleFirstTime() : miracleRoleCache;
            if (shown.Count == 0)
            {
                DarkUtils.ShowTitle(Language.Translate("role.miraclist.fateweave.empty"), Cor.Golden, 2.5f);
                return null;
            }

            UIUtil.OpenRoleSelectWindowUsingTabsP17(shown, [(null, (Predicate<DefinedRole>?)null)], false, Language.Translate("role.miraclist.fateweave.tip"), role =>
            {
                var target = _miraclePlayer;
                if (target == null || role == null) return;

                target.SetRole(role);
                target.RemoveModifier(Miracle.MyRole);
                canUseFateWeave = false;
                fateHasChangedRole = true;
                _miraclePlayer = null!;

                if (SholdPublicRole.GetValue()) MyPlayer.SetRole(PublicMiraclist.MyRole);
                AmongUsUtil.PlayCustomFlash(Cor.Golden, 0.2f, 1.5f);
                screen?.CloseScreen();
            }, ref screen);
            return screen;
        }
        HashSet<DefinedRole> SelectRoleFirstTime()
        {
            static bool Allowed(DefinedRole r) => r.Category == RoleCategory.CrewmateRole && !r.IsSystemRole && r.IsSpawnable
                && r != MyRole && r != PublicMiraclist.MyRole && RoleFilter.Contains(r);

            var pool = Nebula.Roles.Roles.AllRoles.Where(Allowed).GroupBy(r => r.InternalName).Select(g => g.First()).ToList();
            if (pool.Count == 0)
                pool = Nebula.Roles.Roles.AllRoles
                    .Where(r => r.Category == RoleCategory.CrewmateRole && !r.IsSystemRole && r != MyRole && r != PublicMiraclist.MyRole)
                    .GroupBy(r => r.InternalName).Select(g => g.First()).ToList();

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            var shown = pool.Take(Math.Min(RoleCount.GetValue(), pool.Count)).ToHashSet();
            miracleRoleCache = shown;
            return shown;
        }
        [Local]
        public void OnMeetingStart(MeetingStartEvent ev)
        {
            if (!AmOwner || HasOraclist() || _miraclePlayer != null || fateHasChangedRole) return;

            var buttonManager = NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>();
            buttonManager?.RegisterMeetingAction(new(DivineSightImg, state =>
            {
                var target = state.MyPlayer;
                if (target == null || target == MyPlayer || target.IsDead || HasOraclist() || _miraclePlayer != null) return;
                if (NeedCrew.GetValue() && target.Role.Role.Category != RoleCategory.CrewmateRole) return;

                _oraclistPlayer = target;
                target.AddModifier(Oraclist.MyRole);
                meetingCount = 0;
                AmongUsUtil.PlayQuickFlash(Cor.Golden);
                DarkUtils.ShowTitle(Language.Translate("role.miraclist.oracle.selected"), Cor.Golden, 2f);
            }, p => p.MyPlayer != MyPlayer && !p.MyPlayer.IsDead && !HasOraclist() && _miraclePlayer == null
                && (!NeedCrew.GetValue() || p.MyPlayer.Role.Role.Category == RoleCategory.CrewmateRole)));
        }
        // 按轮次算的
        [Local]
        public void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (!AmOwner || !HasOraclist()) return;
            if (r() != BeMiracleTypes.Meeting) return;

            meetingCount++;
            if (meetingCount >= NeedMeetingCount.GetValue())
                BecomeMiracle();
        }
        // 按任务算
        [Local]
        public void DoAllTask(PlayerTaskCompleteEvent ev)
        {
            if (!AmOwner || !HasOraclist()) return;
            if (r() != BeMiracleTypes.DoAllTasks) return;
            if (ev.Player != _oraclistPlayer) return;
            if (!ev.Player.Tasks.IsCompletedTotalTasks) return;

            BecomeMiracle();
        }
        void BecomeMiracle()
        {
            if (!HasOraclist()) return;

            var target = _oraclistPlayer;
            target.RemoveModifier(Oraclist.MyRole);
            target.AddModifier(Miracle.MyRole, [MyPlayer.PlayerId]);

            _oraclistPlayer = null!;
            _miraclePlayer = target;
            canUseFateWeave = true;
            AmongUsUtil.PlayCustomFlash(Cor.OtherMiracleGold, 0.2f, 1.2f);
            DarkUtils.ShowTitle(Language.Translate("role.miraclist.miracle.created"), Cor.OtherMiracleGold, 2.5f);
        }
        [Local]
        public void OnPlayerDied(PlayerDieEvent ev)
        {
            if (!AmOwner) return;

            if (HasOraclist() && ev.Player == _oraclistPlayer)
            {
                _oraclistPlayer = null!;
                canUseFateWeave = false;
                DarkUtils.ShowTitle(Language.Translate("role.miraclist.oracle.died"), Cor.Golden, 2.5f);
                return;
            }

            if (_miraclePlayer == ev.Player && Miracle.TellMiraclistOption.GetValue())
                DarkUtils.ShowTitle(Language.Translate("role.miraclist.miracle.died"), Cor.OtherMiracleGold, 3f);

            if (ev.Player.TryGetModifier<DivineSeed.Instance>(out var seed)
                && seed.MiraclistId == MyPlayer.PlayerId
                && DivineSeed.TellMiraclistOption.GetValue())
                DarkUtils.ShowTitle(Language.Translate("role.miraclist.seed.died"), Cor.OtherMiracleGold, 3f);
        }
        public void Deco27(PlayerDecorateNameEvent ev)
        {
            if (!AmOwner) return;

            if (ev.Player.TryGetModifier<Oraclist.Instance>(out var _)) ev.Name += " <color=#D4AF37>O</color>";
            if (ev.Player.TryGetModifier<Miracle.Instance>(out var _)) ev.Name = ColorUtils.CreateGradient(ev.Name, new UnityEngine.Color(0.72f, 0.42f, 0.05f), new UnityEngine.Color(1.00f, 0.93f, 0.55f));
            if (ev.Player.TryGetModifier<DivineSeed.Instance>(out var _)) ev.Name = ColorUtils.CreateGradient(ev.Name, new UnityEngine.Color(0.72f, 0.42f, 0.05f), new UnityEngine.Color(1.00f, 0.93f, 0.55f));
        }
        [Local]
        public void OnCanBeGuess(PlayerCanGuessPlayerLocalEvent ev)
        {
            if (ev.Target == MyPlayer && CantBeGuess.GetValue())
                ev.CanGuess = false;
        }
        [Local]
        public void PlayerKillEvent(PlayerKillPlayerEvent ev)
        {
            if (!AmOwner || !HasOraclist() || ev.Murderer != _oraclistPlayer) return;
            if (ev.Murderer.Role.Role.Category == RoleCategory.CrewmateRole) return;

            _oraclistPlayer.RemoveModifier(Oraclist.MyRole);
            _oraclistPlayer = null!;
            canUseFateWeave = false;

            if (!PreventDemotion.GetValue() && !MyPlayer.IsDead)
                MyPlayer.SetRole(Crewmate.MyRole);

            AmongUsUtil.PlayCustomFlash(Cor.Golden, 0.2f, 3f, 0.6f);
            DarkUtils.ShowTitle(Language.Translate("role.miraclist.oracle.killed"), Cor.Golden, 3f);
        }
        [Local]
        public void ClearFilterCache(GameEndEvent ev)
        {
            miracleRoleCache?.Clear();
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
        return owner != null &&
            (owner.Role.Role == Miraclist.MyRole || owner.Role.Role == PublicMiraclist.MyRole);
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
        if (player.PlayerId == ownerId
            && (player.Role.Role == Miraclist.MyRole || player.Role.Role == PublicMiraclist.MyRole))
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
public class Oraclist : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier, IAssignableDocument
{
    public Oraclist() : base("oraclist", "oraclist", Cor.OtherMiracleGold) { }

    public Citation? Citation => Core.Citations.TheDarkGalaxy_hvtXsvc;
    bool IAssignableDocument.HasTips => true;
    bool ISpawnable.IsSpawnable => false;
    public RuntimeModifier CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    public static Oraclist MyRole = new();
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
// 奇迹
public class Miracle : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier, IAssignableDocument
{
    public Miracle() : base("miracle", "miracle", Cor.OtherMiracleGold, [CanChooseSeedOption, TellMiraclistOption])
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [Miraclist.MyRole.ConfigurationHolder!, DivineSeed.MyRole.ConfigurationHolder!]);
    }
    internal static readonly BoolConfiguration CanChooseSeedOption = NebulaAPI.Configurations.Configuration("options.role.miraclist.CanChooseSeed", true);
    internal static readonly BoolConfiguration TellMiraclistOption = NebulaAPI.Configurations.Configuration("options.Miracle.TellMiraclist", true);
    public Citation? Citation => Core.Citations.TheDarkGalaxy_hvtXsvc;
    bool IAssignableDocument.HasTips => true;
    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new AssignableDocumentReplacement("%SEED%", Language.Translate(CanChooseSeedOption.GetValue()
            ? "role.miracle.doc.seed.true"
            : "role.miracle.doc.seed.false"));
        yield return new AssignableDocumentReplacement("%NOTIFY%", Language.Translate(TellMiraclistOption.GetValue()
            ? "role.miracle.doc.notify.true"
            : "role.miracle.doc.notify.false"));
    }
    bool ISpawnable.IsSpawnable => false;
    public RuntimeModifier CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments != null && arguments.Length > 0 ? arguments[0] : -1);
    public static Miracle MyRole = new();
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        bool RuntimeAssignable.CanBeAwareAssignment => false;
        public int MiraclistId { get; }
        bool seedChosen;
        Image? seedImage;

        public Instance(GamePlayer player, int miraclistId) : base(player)
        {
            MiraclistId = miraclistId;
        }

        public DefinedModifier Modifier => MyRole;

        public void OnActivated()
        {
            MiraclistChatUtil.Register(
                MiraclistId,
                this, () => MyPlayer.TryGetModifier<Instance>(out var miracle) && miracle.MiraclistId == MiraclistId);
            AmongUsUtil.PlayCustomFlash(Cor.OtherMiracleGold, 0.2f, 1.1f);

            if (!AmOwner || !CanChooseSeedOption.GetValue()) return;
            seedImage = DarkUtils.AsImage("Buttons/Crewmates/DivineSightMeeting.png") ?? DarkUtils.AsImage("Buttons/Crewmates/MaskedDancerInvite.png");

            var tracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, p => ObjectTrackers.PlayerlikeStandardPredicate(p)
                && p.RealPlayer != MyPlayer && !p.RealPlayer.IsDead && p.RealPlayer.PlayerId != MiraclistId, null, false);

            NebulaAPI.Modules.InteractButton(this, MyPlayer, tracker, new PlayerInteractParameter(RealPlayerOnly: true), false, true, VirtualKeyInput.Ability, null, 0f, "SEED", seedImage,
                (p, button) =>
                {
                    var target = p.RealPlayer;
                    if (target == null || target == MyPlayer || target.IsDead || target.PlayerId == MiraclistId || seedChosen) return;
                    target.AddModifier(DivineSeed.MyRole, [MiraclistId]);
                    seedChosen = true;
                    button.StartCoolDown();
                    AmongUsUtil.PlayQuickFlash(Cor.OtherMiracleGold);
                    DarkUtils.ShowTitle(Language.Translate("role.miracle.seed.chosen"), Cor.OtherMiracleGold, 2f);
                }, _ => !seedChosen, _ => !seedChosen && !MyPlayer.IsDead);
        }

        public void Deco(PlayerDecorateNameEvent ev)
        {
            if (!AmOwner) return;
            if (ev.Player == MyPlayer) ev.Name += ColorUtils.ColorString(Cor.MiraclistGold, " M");
            if (ev.Player.PlayerId == MiraclistId) ev.Name = ColorUtils.ColorString(Cor.MiraclistGold, ev.Name);

        }
    }
}
// 神种
public class DivineSeed : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier, IAssignableDocument
{
    public DivineSeed() : base("divineseed", "ds", Cor.OtherMiracleGold, [HasPrivateChat, TellMiraclistOption])
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [Miracle.MyRole.ConfigurationHolder!, Miraclist.MyRole.ConfigurationHolder!]);
    }
    public static readonly BoolConfiguration HasPrivateChat = NebulaAPI.Configurations.Configuration("options.DivineSeed.HasPrivateChat", true);
    internal static readonly BoolConfiguration TellMiraclistOption = NebulaAPI.Configurations.Configuration("options.DivineSeed.TellMiraclist", true);
    public Citation? Citation => Core.Citations.TheDarkGalaxy_hvtXsvc;
    bool IAssignableDocument.HasTips => true;
    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new AssignableDocumentReplacement("%NOTIFY%", Language.Translate(TellMiraclistOption.GetValue()
            ? "role.divineseed.doc.notify.true"
            : "role.divineseed.doc.notify.false"));
    }
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
            if (!HasPrivateChat.GetValue()) return;

            MiraclistChatUtil.Register(
                MiraclistId,
                this,
                () => HasPrivateChat.GetValue()
                    && MyPlayer.TryGetModifier<Instance>(out var seed)
                    && seed.MiraclistId == MiraclistId);
        }
    }
}
public class PublicMiraclist : DefinedRoleTemplate, RuntimeAssignableGenerator<RuntimeRole>, HasCitation, DefinedRole, IAssignableDocument
{
    public PublicMiraclist() : base(
        "publicmiraclist",Cor.Golden,RoleCategory.CrewmateRole,NebulaTeams.CrewmateTeam,withAssignmentOption:false,withOptionHolder:false
        )
    {
    }

    public Citation? Citation => Core.Citations.TheDarkGalaxy_hvtXsvc;
    Image? DefinedAssignable.IconImage => DarkUtils.AsImage("RoleIcon/MiraclistIcon.png", 115f);
    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => false;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages() => [];
    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements() => [];
    bool ISpawnable.IsSpawnable => false;
    public RuntimeRole CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public static readonly PublicMiraclist MyRole = new();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, IGameOperator
    {
        public Instance(GamePlayer player) : base(player)
        {
        }

        public DefinedRole Role => MyRole;

        public void OnActivated()
        {
            if (!AmOwner) return;
            MiraclistChatUtil.Register(
                MyPlayer.PlayerId,
                this,
                () => MyPlayer.Role.Role == MyRole);
            AmongUsUtil.PlayCustomFlash(Cor.MiraclistGold, 0.2f, 1.5f);
        }

        public void Deco(PlayerDecorateNameEvent ev)
        {
            if (!AmOwner) return;
            if (ev.Player == MyPlayer)
                ev.Name += " <color=#D4AF37>公开神迹</color>";
        }

        [Local]
        public void OnCanBeGuess(PlayerCanGuessPlayerLocalEvent ev)
        {
            if (ev.Target == MyPlayer)
                ev.CanGuess = false;
        }

        private void OnPublicRole(PlayerSetFakeRoleNameEvent ev)
        {
            ev.Alternate(Language.Translate("role.publicmiraclist.name"));
        }
    }
}
