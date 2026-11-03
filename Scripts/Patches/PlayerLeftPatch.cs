using Color = Virial.Color;

namespace Dark.Scripts.Patches;



[HarmonyPatch(typeof(GameData),nameof(GameData.ShowNotification))]
public static class PlayerLeftPatch
{
    [HarmonyPrefix]
    public static bool a(string playerName, DisconnectReasons reason)
    {
        if (OptionsManager.ShowPlayerLeftDetailOpt.Value == 0) return true; // 0为禁用
        Color color;
        try
        {
            var dp = GamePlayer.AllPlayers.FirstOrDefault(p => p.PlayerName == playerName);
            if (dp == null)
            {
                // 换用pc尝试
                // var pc = PlayerControl.AllPlayerControls 
                // 老牧师的东西不给用linq
                // 换成foreach
                PlayerControl pc = null!;
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.Data.PlayerName == playerName) 
                    {
                        pc = p;
                        break;
                    }
                }
                if (pc == null)
                {
                    // 还是null
                    DarkLog.LogError("PlayerLeftPatch:离开的玩家GamePlayer与pc均为null，回退默认红色。");
                    color = Color.Red;
                }
                else
                {
                    color = pc.Data.Color;
                }
            }
            else
            {
                // GP取成功了
                // 用上面的dp拿
                color = dp.VanillaPlayer.Data.Color;
            }

            // 有可能玩家已经退完了所以Data是null 
            // 但我懒得试了所以直接try上。
        }
        catch (NullReferenceException)
        {
            color = Color.Red; // 可能的预期行为
        }
        catch (Exception ex)
        {
            color = Color.Red;
            DarkLog.LogError(ex.ToString());
        }

        // 颜色取完了这里拿本体。

        switch (reason)
        {
            case DisconnectReasons.ExitGame:
            case DisconnectReasons.IntentionalLeaving:
                // 抽离一下
                return Show(l("exit"),playerName,reason,color);
            case DisconnectReasons.Hacking:
            case DisconnectReasons.Banned:
                return Show(l("ban"),playerName,reason,color);
            case DisconnectReasons.Kicked:
                return Show(l("kick"),playerName,reason,color);
            default:
                return Show(l("disconnect"),playerName,reason,color);
        }
    }
    static bool Show(string c,string pn,DisconnectReasons dr,Color col)
    {
        DarkLog.LogInfo($"{pn}离开了游戏。（{dr}）");
        string content = c.Replace("%NAME%", pn).Replace("%REA%", dr.ToString()).Replace("%COLOR%", ColorUtility.ToHtmlStringRGB(col.ToUnityColor()));
        if (DestroyableSingleton<HudManager>._instance)
        {
            DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage(content);
            return false;
        }
        return true;
    }
    static string l(string t) => Language.Translate($"notifier.disconnectmsg.{t}");
    
}