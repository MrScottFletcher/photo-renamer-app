using PhotoRenamerApp.Models;
using System.IO;

namespace PhotoRenamerApp.Services;

public sealed class FileWatcherService : IDisposable
{
    private FileSystemWatcher? _watcher;

    public event EventHandler<string>? StableFileDetected;
    public bool IgnoreEvents { get; set; }

    public void Start(AppConfig config)
    {
        Stop();

        if (string.IsNullOrWhiteSpace(config.WatchFolder) || !Directory.Exists(config.WatchFolder))
        {
            return;
        }

        _watcher = new FileSystemWatcher(config.WatchFolder)
        {
            IncludeSubdirectories = config.WatchSubfolders,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            Filter = "*.*",
            EnableRaisingEvents = true
        };

        _watcher.Created += OnChanged;
        _watcher.Renamed += OnRenamed;
    }

    public void Stop()
    {
        if (_watcher is null)
        {
            return;
        }

        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnChanged;
        _watcher.Renamed -= OnRenamed;
        _watcher.Dispose();
        _watcher = null;
    }

    private void OnChanged(object sender, FileSystemEventArgs e) => _ = ProbeAsync(e.FullPath);
    private void OnRenamed(object sender, RenamedEventArgs e) => _ = ProbeAsync(e.FullPath);

    private async Task ProbeAsync(string path)
    {
        if (IgnoreEvents)
            return;
    
        for (var i = 0; i < 8; i++)
        {
            try
            {
                if (File.Exists(path))
                {
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                    StableFileDetected?.Invoke(this, path);
                    return;
                }
            }
            catch
            {
                // still being written
            }

            await Task.Delay(500);
        }
    }

    public void Dispose() => Stop();
}
