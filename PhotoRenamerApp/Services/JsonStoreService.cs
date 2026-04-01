using System.Text.Json;

namespace PhotoRenamerApp.Services;

public sealed class JsonStoreService
{
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public T LoadOrDefault<T>(string path, T fallback) where T : class
    {
        if (!File.Exists(path))
        {
            return fallback;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, _options) ?? fallback;
    }

    public void Save<T>(string path, T data)
    {
        var fullPath = Path.GetFullPath(path);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(data, _options);
        File.WriteAllText(fullPath, json);
    }
}
