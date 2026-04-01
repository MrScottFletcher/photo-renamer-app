using System.Windows.Media.Imaging;
using PhotoRenamerApp.Infrastructure;
using PhotoRenamerApp.Services;

namespace PhotoRenamerApp.Models;

public sealed class FileItem : ObservableObject
{
    private string _originalPath = string.Empty;
    private string _currentPath = string.Empty;
    private string _displayName = string.Empty;
    private string _dateTaken = string.Empty;
    private string _artist = string.Empty;
    private string _subject = string.Empty;
    private string _location = string.Empty;
    private string _briefNote = string.Empty;
    private string _box = string.Empty;
    private string _folderPrefix = string.Empty;
    private string _folderSuffix = string.Empty;
    private string _status = "New";
    private string _presetName = string.Empty;
    private bool _isSelected;
    private string _ocrText = string.Empty;
    private BitmapSource? _previewImage;
    private string _previewLabel = "No preview";
    private long _fileSizeBytes;
    private DateTime _discoveredAt = DateTime.Now;
    private string _filenameTemplate = "{DateTaken} - {Artist} - {Subject} - {BriefNote}";

    public string OriginalPath { get => _originalPath; set => SetProperty(ref _originalPath, value); }
    public string CurrentPath { get => _currentPath; set { if (SetProperty(ref _currentPath, value)) RefreshComputed(); } }
    public string DisplayName { get => _displayName; set => SetProperty(ref _displayName, value); }
    public string Extension => Path.GetExtension(CurrentPath).ToLowerInvariant();
    public string DateTaken { get => _dateTaken; set { if (SetProperty(ref _dateTaken, value)) RefreshComputed(); } }
    public string Artist { get => _artist; set { if (SetProperty(ref _artist, value)) RefreshComputed(); } }
    public string Subject { get => _subject; set { if (SetProperty(ref _subject, value)) RefreshComputed(); } }
    public string Location { get => _location; set => SetProperty(ref _location, value); }
    public string BriefNote { get => _briefNote; set { if (SetProperty(ref _briefNote, value)) RefreshComputed(); } }
    public string Box { get => _box; set { if (SetProperty(ref _box, value)) RefreshComputed(); } }
    public string FolderPrefix { get => _folderPrefix; set { if (SetProperty(ref _folderPrefix, value)) RefreshComputed(); } }
    public string FolderSuffix { get => _folderSuffix; set { if (SetProperty(ref _folderSuffix, value)) RefreshComputed(); } }
    public string PresetName { get => _presetName; set => SetProperty(ref _presetName, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    public string OcrText { get => _ocrText; set => SetProperty(ref _ocrText, value); }
    public BitmapSource? PreviewImage { get => _previewImage; set => SetProperty(ref _previewImage, value); }
    public string PreviewLabel { get => _previewLabel; set => SetProperty(ref _previewLabel, value); }
    public long FileSizeBytes { get => _fileSizeBytes; set => SetProperty(ref _fileSizeBytes, value); }
    public DateTime DiscoveredAt { get => _discoveredAt; set => SetProperty(ref _discoveredAt, value); }
    public string FilenameTemplate { get => _filenameTemplate; set { if (SetProperty(ref _filenameTemplate, value)) RefreshComputed(); } }

    public string FileSizeDisplay => FileSizeBytes switch
    {
        < 1024 => $"{FileSizeBytes} B",
        < 1024 * 1024 => $"{FileSizeBytes / 1024d:F1} KB",
        _ => $"{FileSizeBytes / 1024d / 1024d:F1} MB"
    };

    public string ProposedFileName => FilenameRuleEngine.BuildFileName(this, FilenameTemplate);
    public string DestinationFolderPreview => FilenameRuleEngine.BuildDestinationFolder(this);

    public void RefreshComputed()
    {
        OnPropertyChanged(nameof(ProposedFileName));
        OnPropertyChanged(nameof(DestinationFolderPreview));
    }
}
