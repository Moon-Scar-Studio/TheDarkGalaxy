using Il2CppInterop.Runtime.Injection;

namespace Dark.Scripts.Patches;
public class DeadPlayerStatistics : MonoBehaviour
{
    static DeadPlayerStatistics()
    {
        ClassInjector.RegisterTypeInIl2Cpp<DeadPlayerStatistics>();
    }
    MetaScreen? screen;
    GameStatisticsViewer? viewer;
    CustomUpperRightContent? upBtn;
    string signature = "";
    float refrehsTimer;
    static bool CanViewReplay =>
        GamePlayer.LocalPlayer?.IsDead == true
        && NebulaGameManager.Instance?.CanSeeAllInfo == true
        && NebulaGameManager.Instance?.GameState < NebulaGameStates.Finished;

    public void Update()
    {
        var canView = CanViewReplay;

        if (upBtn == null) RegisterUpperBtn();

        if (!canView)
        {
            upBtn?.Button.gameObject.SetActive(false);
            CloseScreen();
            return;
        }

        if (upBtn == null) return;
        if (!upBtn.Button.gameObject.activeSelf) upBtn.Button.gameObject.SetActive(true);
        if (screen == null) return;
        refrehsTimer -= Time.deltaTime;
        if (refrehsTimer > 0) return;
        refrehsTimer = 0.25f;
        RefreshIfNeeded();
    }
    void RegisterUpperBtn()
    {
        if (upBtn != null) return;
        var holder = ModSingleton<UpperRightButtons>.Instance;
        if (holder == null) return;
        var img = DividedSpriteLoader.FromResource("Nebula.Resources.UpperRightButton.png",100,100,94,true);
        upBtn = new CustomUpperRightContent(
            999, img.AsLoader(3), img.AsLoader(4), img.AsLoader(5), ToggleScreen, () => CanViewReplay, () => screen != null
            );
        upBtn.Button.gameObject.SetActive(false);
        holder.Register(upBtn);
        upBtn.Button.OnMouseOver.AddListener(() =>
        {
            if (upBtn != null)
                NebulaManager.Instance.SetHelpWidget(upBtn.Button, BuildHoverWidget());
        });

        upBtn.Button.OnMouseOut.AddListener(() =>
        {
            if (upBtn != null)
                NebulaManager.Instance.HideHelpWidgetIf(upBtn.Button);
        });
    }
    void ToggleScreen()
    {
        if (screen != null)
            CloseScreen();
        else
            OpenScreen();
    }
    void CloseScreen()
    {
        if (viewer != null)
            Destroy(viewer.gameObject);

        viewer = null;
        signature = "";

        screen?.CloseScreen();

        screen = null;
    }
    void OpenScreen()
    {
        if (!CanViewReplay) return;

        var game = NebulaGameManager.Instance;
        var hud = HudManager.Instance;
        if (game == null || hud == null) return;
        screen = MetaScreen.GenerateWindow(new(6.7f,4.2f),hud.transform,VVector3.Zero,true,false,false,false);

        var sorting = screen.GetComponent<UnityEngine.Rendering.SortingGroup>();
        if (sorting != null) sorting.enabled = false;

        CreateViewer(null);
    }
    void CreateViewer(float? selectedTime)
    {
        var game = NebulaGameManager.Instance;
        var hud = HudManager.Instance;
        if (screen == null || game == null || hud == null) return;

        var mapAsset = VanillaAsset.MapAsset[NebulaAPI.AmongUs.MapId];

        viewer = UnityHelper.CreateObject<GameStatisticsViewer>("Statistics",screen.transform,new VVector3(0f, 2.5f, -20f),LayerExpansion.GetUILayer());

        viewer.Initialize(game,hud.IntroPrefab.PlayerPrefab,mapAsset.MapPrefab,mapAsset.MapScale,hud.IntroPrefab.TeamTitle,true,0f);

        signature = CurrentSignature();

        if (selectedTime.HasValue)
            StartCoroutine(RestoreSelection(viewer, selectedTime.Value).WrapToIl2Cpp());
    }
    void RefreshIfNeeded()
    {
        if (screen == null) return;

        var next = CurrentSignature();
        if (next == signature) return;

        var selected = viewer?.SelectedTime;

        if (viewer != null)
            Destroy(viewer.gameObject);

        viewer = null;
        CreateViewer(selected);
    }
    string CurrentSignature()
    {
        var events = NebulaGameManager.Instance?.GameStatistics.Sealed;
        if (events == null || events.Length == 0) return "0";

        var last = events[^1];
        return $"{events.Length}|{last.EventVariation.Id}|{last.Time:F2}|{last.SourceId}|{last.TargetIdMask}";
    }
    System.Collections.IEnumerator RestoreSelection(GameStatisticsViewer target, float time)
    {
        yield return null;
        if (target == null || NebulaGameManager.Instance == null) yield break;
        var events = ((IArchivedGame)NebulaGameManager.Instance).ArchivedEvents.Where(e => e.EventVariation.IsShownInGame).ToArray();
        int best = -1;
        float bestDiff = float.MaxValue;

        for (int i = 0; i < events.Length; i++)
        {
            float diff = Mathf.Abs(events[i].Time - time);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = i;
            }
        }

        if (best >= 0) target.OnSelect(best);
    }
    MetaWidgetOld BuildHoverWidget()
    {
        var widget = new MetaWidgetOld();
        var game = NebulaGameManager.Instance;
        var hud = HudManager.Instance;
        if (game == null) return widget;

        var font = hud?.IntroPrefab.TeamTitle.font;
        widget.Append(new MetaWidgetOld.Text(new TextAttributeOld(TextAttributeOld.BoldAttrLeft) { Font = font })
        {
            RawText = Language.Translate("ui.DeadPlayerStatistics.gameinprocess"),
            Alignment = IMetaWidgetOld.AlignmentOption.Left
        });
        widget.Append(new MetaWidgetOld.VerticalMargin(0.15f));

        string? alternativeText = game.GameMode?.GetAlternativePlayerStatusText();
        List<(string name, string state, string task, string role)> players = [];
        foreach (var p in game.AllPlayerInfo)
        {
            string nameText = p.Name.Color(UnityEngine.Color.white);
            if (p.TryGetModifier<ExtraMission.Instance>(out var mission))
                nameText += (" <size=60%>(" + (mission.target?.Name ?? "ERROR") + ")</size>").Color((UnityEngine.Color)ExtraMission.MyRole.RoleColor);
            if (p.TryGetModifier<Obsessional.Instance>(out var obsessional))
                nameText += (" <size=60%>(" + (obsessional.Obsession?.Name ?? "ERROR") + ")</size>").Color((UnityEngine.Color)Obsessional.MyRole.RoleColor);

            string stateText = p.Unbox().GetStateText();
            string taskText = ResultText.TaskText(p);
            string roleText = "\u200B" + ResultText.RoleText(game.RoleHistory, p.PlayerId);
            players.Add((nameText, stateText, taskText, roleText));
        }

        widget.Append(new MetaWidgetOld.VariableText(new TextAttributeOld(TextAttributeOld.BoldAttr)
        {
            Font = font,
            Size = new VVector2(8f, 5f),
            Alignment = TMPro.TextAlignmentOptions.Left
        }.EditFontSize(1.1f, 1.1f, 1.1f))
        {
            Alignment = IMetaWidgetOld.AlignmentOption.Left,
            PreResizeBuilder = t =>
            {
                t.fontSizeMax = 1.4f;
                t.fontSize = 1.4f;

                if (alternativeText != null)
                {
                    t.text = alternativeText;
                    return;
                }

                (float nameMax, float stateMax, float taskMax, float roleMax) = (0f, 0f, 0f, 0f);
                float coeff = t.fontSize / t.font.faceInfo.pointSize * t.font.faceInfo.scale * 10f;
                foreach (var p in players)
                {
                    nameMax = Mathn.Max(t.GetPreferredValues(p.name).x / coeff, nameMax);
                    stateMax = Mathn.Max(t.GetPreferredValues(p.state).x / coeff, stateMax);
                    taskMax = Mathn.Max(t.GetPreferredValues(p.task).x / coeff, taskMax);
                    roleMax = Mathn.Max(t.GetPreferredValues(p.role).x / coeff, roleMax);
                }

                float indent = 0;
                indent += nameMax + 5.5f;
                string afterNameIndent = $"<indent={indent:F3}em>";
                indent += taskMax + 2.3f;
                string afterTaskIndent = $"<indent={indent:F3}em>";
                indent += stateMax + 6f;
                string afterStateIndent = $"<indent={indent:F3}em>";

                t.fontSizeMax = 1.4f;
                t.fontSize = 1.4f;
                t.text = string.Join("\n", players.Select(p => $"{p.name}{afterNameIndent}{p.task}</indent>{afterTaskIndent}{p.state}</indent>{afterStateIndent}{p.role}</indent>"));
            }
        });

        return widget;
    }
    void OnDestroy()
    {
        CloseScreen();
    }
}

[HarmonyPatch(typeof(HudManager),nameof(HudManager.Start))]
public static class DeadPlayerStatisticsHudPatch
{
    static void Postfix(HudManager __instance)
    {
        if (__instance.GetComponent<DeadPlayerStatistics>() == null)
            __instance.gameObject.AddComponent<DeadPlayerStatistics>();
    }
}
