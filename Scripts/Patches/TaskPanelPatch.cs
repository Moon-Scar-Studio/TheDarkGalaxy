
namespace Dark.Scripts.Patches;

// 源于Aunlocker。应该拼对了吧。
[HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive), typeof(PlayerControl), typeof(RoleBehaviour), typeof(bool))]
public static class TaskPanelPatch
{
    public static void Postfix(HudManager __instance, RoleBehaviour role, bool isActive)
    {
        if (!MeetingHud.Instance || OptionsManager.ShowTaskPanelOpt.Value != 1) return; // 1是开，没开就退

        var openPosition = __instance.TaskPanel.openPosition;
        openPosition.z = -20f;
        __instance.TaskPanel.openPosition = openPosition;
        __instance.TaskPanel.gameObject.SetActive(true);
    }
}