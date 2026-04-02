using System.IO;

namespace PhotoRenamerApp.Services;
public sealed class AuditLogService
{
    public void Append(string logPath, string message)
    {
        var fullPath = Path.GetFullPath(logPath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.AppendAllText(fullPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}{Environment.NewLine}");
    }
}
