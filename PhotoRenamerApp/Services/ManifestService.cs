using System.Text.Json;
using PhotoRenamerApp.Models;

namespace PhotoRenamerApp.Services;

public sealed class ManifestService
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public void WriteManifest(FileItem item, AppConfig config)
    {
        var baseFolder = Path.GetFullPath(config.SidecarManifestFolder);
        Directory.CreateDirectory(baseFolder);
        var manifestPath = Path.Combine(baseFolder, Path.GetFileName(item.CurrentPath) + ".json");

        var payload = new
        {
            item.OriginalPath,
            item.CurrentPath,
            item.DisplayName,
            item.DateTaken,
            item.Artist,
            item.Subject,
            item.Location,
            item.BriefNote,
            item.Box,
            item.FolderPrefix,
            item.FolderSuffix,
            item.PresetName,
            item.Status,
            item.OcrText,
            ManifestCreatedUtc = DateTime.UtcNow
        };

        File.WriteAllText(manifestPath, JsonSerializer.Serialize(payload, _options));
    }
}
