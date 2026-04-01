using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PhotoRenamerApp.Models;

namespace PhotoRenamerApp.Services;

public interface IMetadataService
{
    void UpdateMetadata(FileItem item, AppConfig config);
}

public sealed class ExifToolMetadataService : IMetadataService
{
    private static readonly Regex TokenRegex = new("\"[^\"]+\"|[^\\s]+", RegexOptions.Compiled);

    public void UpdateMetadata(FileItem item, AppConfig config)
    {
        if (!config.UseExifTool)
        {
            throw new InvalidOperationException("ExifTool metadata writing is disabled in configuration.");
        }

        var exePath = ResolveExecutable(config.ExifToolExecutablePath);
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            throw new FileNotFoundException(
                "ExifTool executable was not found. Set Config.ExifToolExecutablePath to exiftool.exe.",
                config.ExifToolExecutablePath);
        }

        var arguments = BuildArguments(item, config);
        Execute(exePath, arguments, item.CurrentPath);
    }

    private static string ResolveExecutable(string configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuredPath));
        }

        return "exiftool";
    }

    private static IReadOnlyList<string> BuildArguments(FileItem item, AppConfig config)
    {
        var args = new List<string>();
        args.AddRange(Tokenize(config.ExifToolAdditionalArgs));

        ApplyIfPresent(args, "-DateTimeOriginal", NormalizeExifDate(item.DateTaken));
        ApplyIfPresent(args, "-CreateDate", NormalizeExifDate(item.DateTaken));
        ApplyIfPresent(args, "-ModifyDate", NormalizeExifDate(item.DateTaken));
        ApplyIfPresent(args, "-Artist", item.Artist);
        ApplyIfPresent(args, "-Creator", item.Artist);
        ApplyIfPresent(args, "-Author", item.Artist);
        ApplyIfPresent(args, "-By-line", item.Artist);
        ApplyIfPresent(args, "-Subject", item.Subject);
        ApplyIfPresent(args, "-Title", BuildTitle(item));
        ApplyIfPresent(args, "-Comment", item.BriefNote);
        ApplyIfPresent(args, "-Description", BuildDescription(item));
        ApplyIfPresent(args, "-Caption-Abstract", BuildDescription(item));
        ApplyIfPresent(args, "-City", item.Location);
        ApplyIfPresent(args, "-XMP-dc:Coverage", item.Location);

        foreach (var keyword in BuildKeywords(item))
        {
            args.Add($"-Keywords+={keyword}");
            args.Add($"-Subject+={keyword}");
        }

        args.Add(item.CurrentPath);
        return args;
    }

    private static IEnumerable<string> BuildKeywords(FileItem item)
    {
        return new[]
        {
            item.Box,
            item.Location,
            item.Subject,
            item.PresetName,
            ExtractYear(item.DateTaken)
        }
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x!.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildTitle(FileItem item)
    {
        var parts = new[] { item.Subject, item.BriefNote }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToList();

        return parts.Count == 0 ? item.DisplayName : string.Join(" - ", parts);
    }

    private static string BuildDescription(FileItem item)
    {
        var parts = new[]
        {
            item.Subject,
            item.BriefNote,
            item.Location,
            string.IsNullOrWhiteSpace(item.Box) ? null : $"Box: {item.Box}"
        }
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x!.Trim());

        return string.Join(" | ", parts);
    }

    private static void ApplyIfPresent(ICollection<string> args, string tagName, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            args.Add($"{tagName}={value}");
        }
    }

    private static IReadOnlyList<string> Tokenize(string argumentString)
    {
        if (string.IsNullOrWhiteSpace(argumentString))
        {
            return Array.Empty<string>();
        }

        return TokenRegex.Matches(argumentString)
            .Select(m => m.Value.Trim())
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Select(m => m.StartsWith('"') && m.EndsWith('"') ? m[1..^1] : m)
            .ToList();
    }

    private static void Execute(string exePath, IReadOnlyList<string> arguments, string filePath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(filePath) ?? AppContext.BaseDirectory
        };

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start ExifTool process.");
        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(stdErr) ? stdOut : stdErr;
            throw new InvalidOperationException($"ExifTool failed for '{Path.GetFileName(filePath)}': {message}".Trim());
        }
    }

    private static string? NormalizeExifDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed.ToString("yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        if (Regex.IsMatch(raw, "^\\d{4}$"))
        {
            return $"{raw}:01:01 00:00:00";
        }

        var monthYear = Regex.Match(raw, "^(?<year>\\d{4})-(?<month>\\d{2})$");
        if (monthYear.Success)
        {
            return $"{monthYear.Groups["year"].Value}:{monthYear.Groups["month"].Value}:01 00:00:00";
        }

        return null;
    }

    private static string ExtractYear(string raw)
    {
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed.Year.ToString(CultureInfo.InvariantCulture);
        }

        var match = Regex.Match(raw ?? string.Empty, "\\b(18|19|20)\\d{2}\\b");
        return match.Success ? match.Value : string.Empty;
    }
}
