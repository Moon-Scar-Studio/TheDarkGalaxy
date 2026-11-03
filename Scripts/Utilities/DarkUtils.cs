using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dark.Scripts.Utilities;

public static class DarkUtils 
{
    public static Image? AsImage(string path,float defaultPixsPerUnit = 115f)=> NebulaAPI.AddonAsset.GetResource(path)?.AsImage(defaultPixsPerUnit);
    public static string ColorString(Virial.Color color,string content)=>$"<color=#{ColorUtility.ToHtmlStringRGB(color.ToUnityColor())}>{content}</color>";
    public static string ColorString(UnityEngine.Color color, string content) => $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{content}</color>";
    public static Virial.Color ToUnityColor(this UnityEngine.Color color) => new(color.r, color.g, color.b, color.a);
    public static List<GamePlayer> GetAllImp() => GamePlayer.AllPlayers.Where(p=>p.Role.Role.Category == RoleCategory.ImpostorRole).ToList();
    public static List<GamePlayer> GetAllCrew() => GamePlayer.AllPlayers.Where(p => p.Role.Role.Category == RoleCategory.CrewmateRole).ToList();
    public static List<GamePlayer> GetAllNeu() => GamePlayer.AllPlayers.Where(p => p.Role.Role.Category == RoleCategory.NeutralRole).ToList();


}