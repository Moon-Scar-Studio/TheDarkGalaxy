using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Virial.Runtime;

namespace Dark.Scripts.Utilities;

public static class DarkUtils
{
    public static Image? AsImage(string path,float defaultPixsPerUnit = 115f)=> NebulaAPI.AddonAsset.GetResource(path)?.AsImage(defaultPixsPerUnit);
  
    public static List<GamePlayer> GetAllImp() => GamePlayer.AllPlayers.Where(p=>p.Role.Role.Category == RoleCategory.ImpostorRole).ToList();
    public static List<GamePlayer> GetAllCrew() => GamePlayer.AllPlayers.Where(p => p.Role.Role.Category == RoleCategory.CrewmateRole).ToList();
    public static List<GamePlayer> GetAllNeu() => GamePlayer.AllPlayers.Where(p => p.Role.Role.Category == RoleCategory.NeutralRole).ToList();
    public static GamePlayer ToVirialPlayer(this PlayerControl pc) => GamePlayer.GetPlayer(pc.PlayerId)!;
    public static void ShowTitle(string content,Virial.Color color,float dur,bool shake = false)
    {
        var title = NebulaAPI.CurrentGame?.GetModule<TitleShower>();
        title?.SetText(content,color,dur,shake);
    }
}

public static class PrivateChatUtil
{
    static Type? managerType;
    static PropertyInfo? instanceProperty;
    static MethodInfo? registerString;
    static MethodInfo? registerFunc;
    static MethodInfo? registerPublic;
    static MethodInfo? registerPublicGroup;
    static MethodInfo? setPredicate;

    public static bool Available { get; private set; }

    internal static void Initialize()
    {
        try
        {
            managerType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetTypes).FirstOrDefault(t => t.FullName == "Plana.Core.API.PrivateChatManager");

            if (managerType == null)
                return;

            instanceProperty = managerType.GetProperty(
                "Instance",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (instanceProperty == null)
                return;

            registerString = managerType.GetMethod("Register",
            [
                typeof(Virial.Color), typeof(Virial.Color), typeof(string), typeof(string),
                typeof(ILifespan), typeof(Func<bool>),
                typeof(Func<GamePlayer, GamePlayer, bool>), typeof(Action<string>)
            ]);

            registerFunc = managerType.GetMethod("Register",
            [
                typeof(Virial.Color), typeof(Virial.Color), typeof(string), typeof(Func<string>),
                typeof(ILifespan), typeof(Func<bool>),
                typeof(Func<GamePlayer, GamePlayer, bool>), typeof(Action<string>)
            ]);

            registerPublic = managerType.GetMethod("RegisterPublicChannel",
            [
                typeof(Virial.Color), typeof(Virial.Color), typeof(string), typeof(string),
                typeof(ILifespan), typeof(Func<bool>), typeof(bool), typeof(Action<string>)
            ]);

            registerPublicGroup = managerType.GetMethod("RegisterPublicChannel",
            [
                typeof(Virial.Color), typeof(Virial.Color), typeof(string), typeof(string),
                typeof(ILifespan), typeof(Func<bool>), typeof(bool), typeof(Action<string>), typeof(int)
            ]);

            setPredicate = managerType.GetMethod("SetPredicate",
            [
                typeof(string), typeof(Func<bool>),
                typeof(Func<GamePlayer, GamePlayer, bool>)
            ]);

            Available = true;
        }
        catch
        {
            Available = false;
        }
    }

    static IEnumerable<Type> GetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.OfType<Type>();
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }

    static void Invoke(MethodInfo? method, params object?[] args)
    {
        if (!Available || method == null)
            return;

        try
        {
            var manager = instanceProperty?.GetValue(null);
            if (manager != null)
                method.Invoke(manager, args);
        }
        catch
        {
        }
    }

    public static void Register(
        Virial.Color color,
        Virial.Color textFieldColor,
        string chatId,
        string localizedName,
        ILifespan lifespan,
        Func<bool> useChannelPredicate,
        Func<GamePlayer, GamePlayer, bool> canReceive,
        Action<string>? onSendMessage = null)
    {
        Invoke(registerString, color, textFieldColor, chatId, localizedName, lifespan, useChannelPredicate, canReceive, onSendMessage);
    }

    public static void Register(
        Virial.Color color,
        Virial.Color textFieldColor,
        string chatId,
        Func<string> localizedName,
        ILifespan lifespan,
        Func<bool> useChannelPredicate,
        Func<GamePlayer, GamePlayer, bool> canReceive,
        Action<string>? onSendMessage = null)
    {
        Invoke(registerFunc, color, textFieldColor, chatId, localizedName, lifespan, useChannelPredicate, canReceive, onSendMessage);
    }

    public static void RegisterPublicChannel(
        Virial.Color color,
        Virial.Color textFieldColor,
        string chatId,
        string localizedName,
        ILifespan lifespan,
        Func<bool> useChannelPredicate,
        bool donotSendMessage = false,
        Action<string>? onSendMessage = null,
        int group = 0)
    {
        if (group == 0)
            Invoke(registerPublic, color, textFieldColor, chatId, localizedName, lifespan, useChannelPredicate, donotSendMessage, onSendMessage);
        else
            Invoke(registerPublicGroup, color, textFieldColor, chatId, localizedName, lifespan, useChannelPredicate, donotSendMessage, onSendMessage, group);
    }

    public static void SetPredicate(
        string chatId,
        Func<bool> useChannelPredicate,
        Func<GamePlayer, GamePlayer, bool>? canReceive = null)
    {
        Invoke(setPredicate, chatId, useChannelPredicate, canReceive);
    }
}

[NebulaPreprocess(PreprocessPhase.PostLoadAddons)]
public static class PrivateChatUtilInitializer
{
    static void Preprocess(NebulaPreprocessor preprocessor) => PrivateChatUtil.Initialize();
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
    public static string CreateGradient(string text,UnityEngine.Color colorA,UnityEngine.Color colorB,float speed = 0.35f,float step = 0.06f,float? time = null,float offset = 0f)
    {
        if (string.IsNullOrEmpty(text)) return "";

        float t = time ?? Time.time;
        var sb = new StringBuilder(text.Length * 24);

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c)) { sb.Append(c); continue; }

            float phase = (t * speed + i * step + offset) * Mathf.PI * 2f;
            float k = (Mathf.Sin(phase) + 1f) * 0.5f;
            UnityEngine.Color col = UnityEngine.Color.Lerp(colorA, colorB, k);

            sb.Append($"<color=#{ColorToHex(col)}>{c}</color>");
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