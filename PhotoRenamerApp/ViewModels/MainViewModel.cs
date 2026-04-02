using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using PhotoRenamerApp.Infrastructure;
using PhotoRenamerApp.Models;
using PhotoRenamerApp.Services;
using System.IO;


namespace PhotoRenamerApp.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly JsonStoreService _jsonStore = new();
    private readonly FileWatcherService _watcher = new();
    private readonly IMetadataService _metadataService = new ExifToolMetadataService();
    private readonly FileOperationService _fileOperations = new();
    private readonly AuditLogService _auditLog = new();
    private readonly PreviewService _previewService = new();
    private readonly ManifestService _manifestService = new();
    private readonly IOcrService _ocrService = new OcrService();

    private string _configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    private AppConfig _config = new();
    private FileItem? _selectedFile;
    private MetadataPreset? _selectedPreset;
    private string _statusMessage = "Ready.";
    private bool _watcherRunning;

    public MainViewModel()
    {
        Files = new ObservableCollection<FileItem>();
        Presets = new ObservableCollection<MetadataPreset>();

        SaveCommand = new RelayCommand(_ => SaveAll());
        RefreshCommand = new RelayCommand(_ => LoadWatchFolderFiles());
        StartWatcherCommand = new RelayCommand(_ => StartWatcher(), _ => !_watcherRunning);
        StopWatcherCommand = new RelayCommand(_ => StopWatcher(), _ => _watcherRunning);
        BrowseWatchFolderCommand = new RelayCommand(_ => BrowseWatchFolder());
        BrowseDestinationCommand = new RelayCommand(_ => BrowseDestinationFolder());
        AddPresetCommand = new RelayCommand(_ => AddPreset());
        RemovePresetCommand = new RelayCommand(_ => RemovePreset(), _ => SelectedPreset is not null);
        ApplyPresetCommand = new RelayCommand(_ => ApplyPresetToCheckedFiles(), _ => SelectedPreset is not null && GetTargetFiles().Any());
        RenameAndWriteMetadataCommand = new RelayCommand(_ => RenameAndWriteMetadata(GetTargetFiles()), _ => GetTargetFiles().Any());
        MoveToDestinationCommand = new RelayCommand(_ => MoveToDestination(GetTargetFiles()), _ => GetTargetFiles().Any());
        RunOcrCommand = new AsyncRelayCommand(_ => RunOcrOnSelectionAsync(), _ => GetTargetFiles().Any());

        _watcher.StableFileDetected += OnStableFileDetected;
        LoadAll();
    }

    public ObservableCollection<FileItem> Files { get; }
    public ObservableCollection<MetadataPreset> Presets { get; }

    public AppConfig Config
    {
        get => _config;
        set
        {
            if (SetProperty(ref _config, value))
            {
                _config.PropertyChanged += (_, __) => RefreshFileComputedValues();
                RefreshFileComputedValues();
            }
        }
    }

    public FileItem? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (SetProperty(ref _selectedFile, value))
            {
                if (value is not null)
                {
                    _previewService.LoadPreview(value, Config);
                }
                RaiseCommandStates();
            }
        }
    }

    public MetadataPreset? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (SetProperty(ref _selectedPreset, value))
            {
                RemovePresetCommand.RaiseCanExecuteChanged();
                ApplyPresetCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    public RelayCommand SaveCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand StartWatcherCommand { get; }
    public RelayCommand StopWatcherCommand { get; }
    public RelayCommand BrowseWatchFolderCommand { get; }
    public RelayCommand BrowseDestinationCommand { get; }
    public RelayCommand AddPresetCommand { get; }
    public RelayCommand RemovePresetCommand { get; }
    public RelayCommand ApplyPresetCommand { get; }
    public RelayCommand RenameAndWriteMetadataCommand { get; }
    public RelayCommand MoveToDestinationCommand { get; }
    public AsyncRelayCommand RunOcrCommand { get; }

    public void LoadAll()
    {
        Config = _jsonStore.LoadOrDefault(_configPath, new AppConfig());
        var presetPath = ResolvePresetPath();
        var loadedPresets = _jsonStore.LoadOrDefault(presetPath, new List<MetadataPreset>());
        Presets.Clear();
        foreach (var preset in loadedPresets.OrderBy(p => p.Name)) Presets.Add(preset);

        LoadWatchFolderFiles();
        if (Config.AutoStartWatcher) StartWatcher();
    }

    public void SaveAll()
    {
        _jsonStore.Save(_configPath, Config);
        _jsonStore.Save(ResolvePresetPath(), Presets.ToList());
        StatusMessage = "Configuration and presets saved.";
    }

    public void LoadWatchFolderFiles()
    {
        Files.Clear();

        if (string.IsNullOrWhiteSpace(Config.WatchFolder) || !Directory.Exists(Config.WatchFolder))
        {
            StatusMessage = "Watch folder does not exist yet.";
            return;
        }

        var files = Directory.EnumerateFiles(
            Config.WatchFolder,
            "*.*",
            Config.WatchSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        foreach (var path in files.Where(IsSupported).OrderBy(Path.GetFileName))
        {
            Files.Add(CreateFileItem(path));
        }

        SelectedFile = Files.FirstOrDefault();
        StatusMessage = $"Loaded {Files.Count} file(s).";
        RaiseCommandStates();
    }

    public void RenameAndWriteMetadata(IEnumerable<FileItem> items)
    {
        string stage = "Begin";
        var count = 0;
        foreach (var item in items)
        {
            try
            {
                if (Config.CreateOperationBackups)
                {
                    stage = "CreateBackup";
                    _fileOperations.CreateBackup(item.CurrentPath);
                }
                //Need to close the file so tha the EXIF tool can open and write the file.

                _watcher.IgnoreEvents = true;

                stage = "RenameFile";
                var renamedPath = _fileOperations.RenameFile(item, Config.FilenameTemplate, true);

                stage = "UpdateMetadata";
                _metadataService.UpdateMetadata(item, Config);

                stage = "WriteManifest";
                _manifestService.WriteManifest(item, Config);

                item.Status = "Metadata Written";
                _auditLog.Append(Config.AuditLogFilePath, $"UPDATED | {renamedPath}");
                count++;
            }
            catch (Exception ex)
            {
                item.Status = "Error " + stage;
                _auditLog.Append(Config.AuditLogFilePath, $"ERROR | in {stage} - {item.CurrentPath} | {ex.Message}");
            }
            finally
            {
                _watcher.IgnoreEvents = false;
            }
        }

        StatusMessage = $"Processed {count} file(s).";
        SelectedFile?.RefreshComputed();
        RaiseCommandStates();
    }

    public void MoveToDestination(IEnumerable<FileItem> items)
    {
        string stage = "Begin";
        var count = 0;
        foreach (var item in items.ToList())
        {
            try
            {
                _watcher.IgnoreEvents = true;

                if (Config.CreateOperationBackups)
                {
                    stage = "CreateBackup";
                    _fileOperations.CreateBackup(item.CurrentPath);
                }

                stage = "RenameFile";
                var renamedPath = _fileOperations.RenameFile(item, Config.FilenameTemplate,false);

                stage = "UpdateMetadata"; 
                _metadataService.UpdateMetadata(item, Config);

                stage = "MoveToDestination"; 
                var movedPath = _fileOperations.MoveToDestination(item, Config.DestinationRootFolder, Config.CopyBeforeMoveForCrossVolumeSafety);

                stage = "WriteManifest";
                _manifestService.WriteManifest(item, Config);
                
                stage = "_auditLog.Append";
                _auditLog.Append(Config.AuditLogFilePath, $"MOVED | {renamedPath} => {movedPath}");
                count++;
            }
            catch (Exception ex)
            {
                item.Status = "Error " + stage;
                _auditLog.Append(Config.AuditLogFilePath, $"ERROR | in {stage} - {item.CurrentPath} | {ex.Message}");
            }
            finally
            {
                _watcher.IgnoreEvents = false;
            }
        }

        StatusMessage = $"Moved {count} file(s).";
        LoadWatchFolderFiles();
    }

    public void Shutdown()
    {
        SaveAll();
        StopWatcher();
    }

    private IEnumerable<FileItem> GetTargetFiles()
    {
        var selected = Files.Where(f => f.IsSelected).ToList();
        if (selected.Count > 0) return selected;
        return SelectedFile is not null ? [SelectedFile] : Array.Empty<FileItem>();
    }

    private FileItem CreateFileItem(string path)
    {
        var file = new FileItem
        {
            OriginalPath = path,
            CurrentPath = path,
            DisplayName = Path.GetFileName(path),
            FileSizeBytes = new FileInfo(path).Length,
            DiscoveredAt = File.GetCreationTime(path),
            FilenameTemplate = Config.FilenameTemplate,
        };

        file.RefreshComputed();
        return file;
    }

    private bool IsSupported(string path) => Config.SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    private void OnStableFileDetected(object? sender, string path)
    {
        if (!IsSupported(path)) return;

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (Files.Any(f => string.Equals(f.CurrentPath, path, StringComparison.OrdinalIgnoreCase))) return;
            var item = CreateFileItem(path);
            Files.Add(item);
            SelectedFile = item;
            StatusMessage = $"Detected new file: {Path.GetFileName(path)}";
            RaiseCommandStates();
        }, DispatcherPriority.Background);
    }

    private void StartWatcher()
    {
        _watcher.Start(Config);
        _watcherRunning = true;
        StatusMessage = "Watcher started.";
        StartWatcherCommand.RaiseCanExecuteChanged();
        StopWatcherCommand.RaiseCanExecuteChanged();
    }

    private void StopWatcher()
    {
        _watcher.Stop();
        _watcherRunning = false;
        StatusMessage = "Watcher stopped.";
        StartWatcherCommand.RaiseCanExecuteChanged();
        StopWatcherCommand.RaiseCanExecuteChanged();
    }

    private void BrowseWatchFolder()
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            Config.WatchFolder = dialog.SelectedPath;
            OnPropertyChanged(nameof(Config));
            LoadWatchFolderFiles();
        }
    }

    private void BrowseDestinationFolder()
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            Config.DestinationRootFolder = dialog.SelectedPath;
            OnPropertyChanged(nameof(Config));
        }
    }

    private void AddPreset()
    {
        var preset = new MetadataPreset { Name = "New Preset" };
        Presets.Add(preset);
        SelectedPreset = preset;
        StatusMessage = "Preset added.";
    }

    private void RemovePreset()
    {
        if (SelectedPreset is null) return;
        Presets.Remove(SelectedPreset);
        SelectedPreset = Presets.FirstOrDefault();
        StatusMessage = "Preset removed.";
    }

    private void ApplyPresetToCheckedFiles()
    {
        if (SelectedPreset is null)
        {
            StatusMessage = "Select a preset first.";
            return;
        }

        var count = 0;
        foreach (var item in GetTargetFiles())
        {
            item.DateTaken = SelectedPreset.DateTaken;
            item.Artist = SelectedPreset.Artist;
            item.Subject = SelectedPreset.Subject;
            item.Location = SelectedPreset.Location;
            item.BriefNote = SelectedPreset.BriefNote;
            item.Box = SelectedPreset.Box;
            item.FolderPrefix = SelectedPreset.FolderPrefix;
            item.FolderSuffix = SelectedPreset.FolderSuffix;
            item.PresetName = SelectedPreset.Name;
            item.Status = "Preset Applied";
            count++;
        }

        StatusMessage = $"Applied preset '{SelectedPreset.Name}' to {count} file(s).";
        RaiseCommandStates();
    }

    private async Task RunOcrOnSelectionAsync()
    {
        var count = 0;
        foreach (var item in GetTargetFiles())
        {
            item.OcrText = await _ocrService.ExtractTextAsync(item, Config);
            item.Status = string.IsNullOrWhiteSpace(item.OcrText) ? "OCR skipped" : "OCR captured";
            count++;
        }

        StatusMessage = $"OCR processed for {count} file(s).";
    }

    private string ResolvePresetPath() => Path.GetFullPath(Config.PresetsFilePath);

    private void RefreshFileComputedValues()
    {
        foreach (var file in Files)
        {
            file.FilenameTemplate = Config.FilenameTemplate;
            file.RefreshComputed();
        }
    }

    private void RaiseCommandStates()
    {
        ApplyPresetCommand.RaiseCanExecuteChanged();
        RenameAndWriteMetadataCommand.RaiseCanExecuteChanged();
        MoveToDestinationCommand.RaiseCanExecuteChanged();
        RunOcrCommand.RaiseCanExecuteChanged();
    }
}
