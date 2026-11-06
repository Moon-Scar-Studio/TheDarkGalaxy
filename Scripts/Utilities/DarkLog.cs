
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebula.Modules.Logging;
using System.IO;

namespace Dark.Scripts.Utilities;


public static class DarkLog
{
    private const long MaxLogSizeBytes = 10 * 1024 * 1024; // 10MB

    private static readonly string LogFilePath;

    static DarkLog()
    {
        string gameRootPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "TheDarkGalaxy.log");
        string determinedPath;

        try
        {
            using (File.Open(gameRootPath, FileMode.OpenOrCreate, FileAccess.Write)) { }
            determinedPath = gameRootPath;
        }
        catch
        {
            string localLow = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "..", "LocalLow", "Innersloth", "Among Us"
            );
            determinedPath = Path.Combine(Path.GetFullPath(localLow), "GiftLog.log");
            string? directory = Path.GetDirectoryName(determinedPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        LogFilePath = determinedPath;

        try
        {
            var info = new FileInfo(LogFilePath);
            if (info.Exists && info.Length > MaxLogSizeBytes)
                ClearLog();
        }
        catch { }
    }

    public static void DebugLog(string message) => Write("Debug", message);
    public static void LogInfo(string message) => Write("Info", message);
    public static void LogWarning(string message) => Write("Warning", message);
    public static void LogError(string message) => Write("Error", message);

    public static void ClearLog()
    {
        try
        {
            File.WriteAllText(LogFilePath, string.Empty);
        }
        catch { }
    }

    static void Write(string level, string? message)
    {
        message ??= "null";
        string final = $"[{DateTime.Now:yyyy/MM/dd HH:mm} | {level}] {message}";
        AppendLog(final);
    }

    static void AppendLog(string content)
    {
        try
        {
            File.AppendAllText(LogFilePath, content + Environment.NewLine);
        }
        catch { }
    }
}