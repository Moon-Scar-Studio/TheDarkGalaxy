using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dark.Scripts.Utilities;

public static class DarkUtils
{
    public static Image? AsImage(string path,float defaultPixsPerUnit = 115f)=> NebulaAPI.AddonAsset.GetResource(path)?.AsImage(defaultPixsPerUnit);
  
    public static List<GamePlayer> GetAllImp() => GamePlayer.AllPlayers.Where(p=>p.Role.Role.Category == RoleCategory.ImpostorRole).ToList();
    public static List<GamePlayer> GetAllCrew() => GamePlayer.AllPlayers.Where(p => p.Role.Role.Category == RoleCategory.CrewmateRole).ToList();
    public static List<GamePlayer> GetAllNeu() => GamePlayer.AllPlayers.Where(p => p.Role.Role.Category == RoleCategory.NeutralRole).ToList();
    public static GamePlayer ToVirialPlayer(this PlayerControl pc) => GamePlayer.GetPlayer(pc.PlayerId)!;
}
public static class ColorUtils
{
    public static string ColorString(Virial.Color color, string content) => $"<color=#{ColorUtility.ToHtmlStringRGB(color.ToUnityColor())}>{content}</color>";
    public static string ColorString(UnityEngine.Color color, string content) => $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{content}</color>";
    public static Virial.Color ToVirialColor(this UnityEngine.Color color) => new(color.r, color.g, color.b, color.a);

    public static string Create(string suffix, float hueSpeed = 0.35f, float hueStep = 0.06f, float saturation = 0.9f, float value = 1f, float? time = null, float offset = 0f)
    {
        if (string.IsNullOrEmpty(suffix)) return "";

        float t = time ?? Time.time;
        float baseHue = (t * hueSpeed + offset) % 1f;
        var sb = new StringBuilder(suffix.Length * 24);

        for (int i = 0; i < suffix.Length; i++)
        {
            char c = suffix[i];
            if (c == ' ')
            {
                sb.Append(' ');
                continue;
            }
            float hue = (baseHue + i * hueStep) % 1f;
            UnityEngine.Color color = UnityEngine.Color.HSVToRGB(hue, saturation, value);
            sb.Append($"<color=#{ColorToHex(color)}>{c}</color>");
        }
        return sb.ToString();
    }
    public static string CreateRandom(string suffix, float saturation = 0.9f, float value = 1f, float? time = null)
    {
        if (string.IsNullOrEmpty(suffix)) return "";
        int seed = time.HasValue ? (int)(time.Value * 10000) : Environment.TickCount;
        var rand = new System.Random(seed);
        var sb = new StringBuilder(suffix.Length * 24);

        for (int i = 0; i < suffix.Length; i++)
        {
            char c = suffix[i];
            if (c == ' ')
            {
                sb.Append(' ');
                continue;
            }
            float hue = (float)rand.NextDouble();
            UnityEngine.Color color = UnityEngine.Color.HSVToRGB(hue, saturation, value);
            sb.Append($"<color=#{ColorToHex(color)}>{c}</color>");
        }
        return sb.ToString();
    }
    public static string ColorToHex(UnityEngine.Color color)
    {
        byte r = (byte)(color.r * 255);
        byte g = (byte)(color.g * 255);
        byte b = (byte)(color.b * 255);
        byte a = (byte)(color.a * 255);
        return $"{r:X2}{g:X2}{b:X2}{a:X2}";
    }
}