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
        RefreshIfNeed();
    }
    void RegisterUpperBtn()
    {
        if (upBtn != null) return;
        var holder = ModSingleton<UpperRightButtons>.Instance;
        if (holder == null) return;
        var img = DividedSpriteLoader.FromResource("Nebula.Resources.UpperRightButton.png",100,100,94,true);
        upBtn = new CustomUpperRightContent(
            12, img.AsLoader(3), img.AsLoader(4), img.AsLoader(5), () => {/*TODO*/ }, () => GamePlayer.LocalPlayer?.IsDead == true, () => screen != null
            );
        holder.Register(upBtn);
    }
    void OpenScreen()
    {
        var game = NebulaGameManager.Instance;
        var hud = HudManager.Instance;
        if (game == null || hud == null) return;
        screen = MetaScreen.GenerateWindow(new(6.7f,4.2f),hud.transform,VVector3.Zero,true,false,false,false);
    }
    void RefreshIfNeed()
    {
        if (screen == null) return;

        
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
