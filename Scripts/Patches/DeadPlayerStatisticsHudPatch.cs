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
    public void Update()
    {
        
        var local = GamePlayer.LocalPlayer;
        if(local == null || local.IsAlive)
        {
            upBtn?.Button.gameObject.SetActive(false);
        }
        if (upBtn == null) RegisterUpperBtn(); // Start没注册上。 有可能吗。 有可能吧。 嗯。
        if (upBtn == null)
        {
            DarkLog.LogError("[DeadPlayerStatistics] 判空upbtn注册后再次判空依旧为空");
            return;
        }
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
            12, img.AsLoader(3), img.AsLoader(4), img.AsLoader(5), ToggleScreen, () => GamePlayer.LocalPlayer?.IsDead == true, () => screen != null
            );
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
        var game = NebulaGameManager.Instance;
        var hud = HudManager.Instance;
        if (game == null || hud == null) return;
        screen = MetaScreen.GenerateWindow(new(6.7f,4.2f),hud.transform,VVector3.Zero,true,false,false,false);
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
        if (game == null) return widget;

        string endText = game.EndState?.EndCondition.DisplayText.GetString() ?? "";
        string title = Language.Translate("ui.DeadPlayerStatistics.gameinprocess");

        widget.Append(new MetaWidgetOld.Text(new TextAttributeOld(TextAttributeOld.BoldAttrLeft))
        {
            RawText = title,
            Alignment = IMetaWidgetOld.AlignmentOption.Left
        });

        foreach (var p in game.AllPlayerInfo)
        {
            string role = ResultText.RoleText(game.RoleHistory, p.PlayerId);
            if (string.IsNullOrEmpty(role))
                role = p.Role.Role.DisplayColoredName;

            bool winner = game.EndState?.Winners.Test(p) ?? false;
            string name = winner ? p.Name.Color(UnityEngine.Color.yellow) : p.Name;

            widget.Append(new MetaWidgetOld.Text(new TextAttributeOld(TextAttributeOld.NormalAttrLeft))
            {
                RawText = $"{name}    {role}",
                Alignment = IMetaWidgetOld.AlignmentOption.Left
            });
        }

        return widget;
    }
    void OnDestroy()
    {
        CloseScreen();
    }
}

static class DeadPlayerStatisticsHudPatch
{
    [HarmonyPatch(typeof(HudManager),nameof(HudManager.Start))]
    static void Postfix(HudManager __instance)
    {
        if (__instance.GetComponent<DeadPlayerStatistics>() == null)
            __instance.gameObject.AddComponent<DeadPlayerStatistics>();
    }
}
