using System.Text.RegularExpressions;
using PhotoRenamerApp.Models;

namespace PhotoRenamerApp.Services;

public static class FilenameBuilder
{
    private static readonly Regex InvalidChars = new($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]", RegexOptions.Compiled);

    public static string BuildFileName(FileItem item)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(item.DateTaken)) parts.Add(item.DateTaken.Trim());
        if (!string.IsNullOrWhiteSpace(item.Artist)) parts.Add(item.Artist.Trim());
        if (!string.IsNullOrWhiteSpace(item.Subject)) parts.Add(item.Subject.Trim());
        if (!string.IsNullOrWhiteSpace(item.BriefNote)) parts.Add(item.BriefNote.Trim());

        var baseName = parts.Count == 0 ? Path.GetFileNameWithoutExtension(item.CurrentPath) : string.Join(" - ", parts);
        baseName = Sanitize(baseName);
        return baseName + item.Extension.ToLowerInvariant();
    }

    public static string BuildDestinationFolder(FileItem item)
    {
        var yearBucket = !string.IsNullOrWhiteSpace(item.DateTaken) && item.DateTaken.Length >= 4
            ? item.DateTaken[..4]
            : "Undated";

        var subjectBucket = string.IsNullOrWhiteSpace(item.Subject) ? "General" : Sanitize(item.Subject);
        var prefix = string.IsNullOrWhiteSpace(item.FolderPrefix) ? string.Empty : Sanitize(item.FolderPrefix) + " ";
        var suffix = string.IsNullOrWhiteSpace(item.FolderSuffix) ? string.Empty : " " + Sanitize(item.FolderSuffix);
        var box = string.IsNullOrWhiteSpace(item.Box) ? "Unsorted Box" : Sanitize(item.Box);

        return Path.Combine(box, $"{prefix}{yearBucket} - {subjectBucket}{suffix}".Trim());
    }

    public static string Sanitize(string value)
    {
        var cleaned = InvalidChars.Replace(value, "_");
        cleaned = cleaned.Replace("  ", " ").Trim().Trim('.');
        return string.IsNullOrWhiteSpace(cleaned) ? "Untitled" : cleaned;
    }
}
