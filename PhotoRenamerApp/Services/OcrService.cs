using System.Diagnostics;
using PhotoRenamerApp.Models;
using System.IO;


namespace PhotoRenamerApp.Services;

public interface IOcrService
{
    Task<string> ExtractTextAsync(FileItem item, AppConfig config, CancellationToken cancellationToken = default);
}

public sealed class OcrService : IOcrService
{
    public async Task<string> ExtractTextAsync(FileItem item, AppConfig config, CancellationToken cancellationToken = default)
    {
        if (!config.EnableOcrHook || string.IsNullOrWhiteSpace(config.OcrExecutablePath) || !File.Exists(config.OcrExecutablePath))
        {
            return string.Empty;
        }

        var tempOutputBase = Path.Combine(Path.GetTempPath(), $"PhotoRenamerOCR_{Guid.NewGuid():N}");
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = config.OcrExecutablePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            psi.ArgumentList.Add(item.CurrentPath);
            psi.ArgumentList.Add(tempOutputBase);
            psi.ArgumentList.Add("quiet");

            using var process = Process.Start(psi);
            if (process is null)
            {
                return string.Empty;
            }

            await process.WaitForExitAsync(cancellationToken);
            var txtFile = tempOutputBase + ".txt";
            return File.Exists(txtFile) ? await File.ReadAllTextAsync(txtFile, cancellationToken) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
        finally
        {
            var txtFile = tempOutputBase + ".txt";
            if (File.Exists(txtFile)) File.Delete(txtFile);
        }
    }
}
