using System.Globalization;
using System.Text.RegularExpressions;
using PhotoRenamerApp.Models;

namespace PhotoRenamerApp.Services;

public static class FilenameRuleEngine
{
    private static readonly Regex InvalidChars = new($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]", RegexOptions.Compiled);
    private static readonly Regex TokenRegex = new("\\{(?<name>[A-Za-z]+)(:(?<format>[^}]+))?\\}", RegexOptions.Compiled);

    public static string BuildFileName(FileItem item, string template)
    {
        var resolved = TokenRegex.Replace(template, match =>
        {
            var name = match.Groups["name"].Value;
            var format = match.Groups["format"].Success ? match.Groups["format"].Value : null;
            return ResolveToken(item, name, format);
        });

        var parts = resolved
            .Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(Sanitize)
            .Where(value => !string.IsNullOrWhiteSpace(value) && value != "Untitled")
            .ToList();

        var basename = parts.Count == 0
            ? Sanitize(Path.GetFileNameWithoutExtension(item.CurrentPath))
            : string.Join(" - ", parts);

        return basename + item.Extension;
    }

    public static string BuildDestinationFolder(FileItem item)
    {
        var yearBucket = ExtractYear(item.DateTaken);
        var box = string.IsNullOrWhiteSpace(item.Box) ? "Unsorted Box" : Sanitize(item.Box);
        var subject = string.IsNullOrWhiteSpace(item.Subject) ? "General" : Sanitize(item.Subject);
        var prefix = string.IsNullOrWhiteSpace(item.FolderPrefix) ? string.Empty : Sanitize(item.FolderPrefix) + " ";
        var suffix = string.IsNullOrWhiteSpace(item.FolderSuffix) ? string.Empty : " " + Sanitize(item.FolderSuffix);
        return Path.Combine(box, $"{prefix}{yearBucket} - {subject}{suffix}".Trim());
    }

    public static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Untitled";
        var cleaned = InvalidChars.Replace(value.Trim(), "_");
        cleaned = Regex.Replace(cleaned, "\\s+", " ");
        cleaned = cleaned.Trim().Trim('.');
        return string.IsNullOrWhiteSpace(cleaned) ? "Untitled" : cleaned;
    }

    private static string ResolveToken(FileItem item, string token, string? format)
    {
        return token switch
        {
            nameof(FileItem.DateTaken) => FormatDate(item.DateTaken, format),
            nameof(FileItem.Artist) => item.Artist,
            nameof(FileItem.Subject) => item.Subject,
            nameof(FileItem.Location) => item.Location,
            nameof(FileItem.BriefNote) => item.BriefNote,
            nameof(FileItem.Box) => item.Box,
            nameof(FileItem.PresetName) => item.PresetName,
            nameof(FileItem.DisplayName) => Path.GetFileNameWithoutExtension(item.DisplayName),
            "Year" => ExtractYear(item.DateTaken),
            _ => string.Empty
        };
    }

    private static string FormatDate(string raw, string? format)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed.ToString(format ?? "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (Regex.IsMatch(raw, "^\\d{4}$"))
        {
            return raw;
        }

        return raw;
    }

    private static string ExtractYear(string raw)
    {
        if (DateTime.TryParse(raw, out var parsed))
        {
            return parsed.Year.ToString(CultureInfo.InvariantCulture);
        }

        var match = Regex.Match(raw ?? string.Empty, "\\b(18|19|20)\\d{2}\\b");
        return match.Success ? match.Value : "Undated";
    }
}
