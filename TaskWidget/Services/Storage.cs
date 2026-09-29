using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using TaskWidget.Models;

namespace TaskWidget.Services;

/// <summary>
/// Persists tasks and settings as JSON in %APPDATA%\TaskWidget, outside the install folder,
/// so data survives closing the app and replacing the exe. Each save keeps the previous
/// version as a .bak file, used as a fallback if the main file can't be read.
/// </summary>
public static class Storage
{
    public static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskWidget");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static List<TaskItem> LoadTasks() => Load<List<TaskItem>>("tasks.json") ?? new();
    public static AppSettings LoadSettings() => Load<AppSettings>("settings.json") ?? new();

    public static void SaveTasks(IEnumerable<TaskItem> tasks) => Save("tasks.json", tasks);
    public static void SaveSettings(AppSettings settings) => Save("settings.json", settings);

    private static T? Load<T>(string fileName)
    {
        var path = Path.Combine(Dir, fileName);
        if (TryRead<T>(path, out var data)) return data;

        if (File.Exists(path))
        {
            // Keep the unreadable file aside instead of silently overwriting it on next save.
            File.Copy(path, path + $".corrupt-{DateTime.Now:yyyyMMdd-HHmmss}", overwrite: true);
        }
        return TryRead<T>(path + ".bak", out var backup) ? backup : default;
    }

    private static bool TryRead<T>(string path, out T? data)
    {
        data = default;
        if (!File.Exists(path)) return false;
        try
        {
            data = JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
            return data is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void Save<T>(string fileName, T data)
    {
        Directory.CreateDirectory(Dir);
        var path = Path.Combine(Dir, fileName);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(data, Options));
        if (File.Exists(path))
            File.Replace(tmp, path, path + ".bak");
        else
            File.Move(tmp, path);
    }
}
