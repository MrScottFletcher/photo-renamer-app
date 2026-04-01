using PhotoRenamerApp.Infrastructure;

namespace PhotoRenamerApp.Models;

public sealed class AppConfig : ObservableObject
{
    private string _watchFolder = string.Empty;
    private string _destinationRootFolder = string.Empty;
    private string _presetsFilePath = "presets.json";
    private bool _watchSubfolders;
    private bool _autoStartWatcher = true;
    private bool _copyBeforeMoveForCrossVolumeSafety = true;
    private bool _createOperationBackups = true;
    private bool _enableOcrHook;
    private string _ocrExecutablePath = string.Empty;
    private string _filenameTemplate = "{DateTaken} - {Artist} - {Subject} - {BriefNote}";
    private string _auditLogFilePath = "operations.log";
    private string _sidecarManifestFolder = "manifests";
    private bool _useExifTool = true;
    private string _exifToolExecutablePath = @"C:\Tools\exiftool\exiftool.exe";
    private string _exifToolAdditionalArgs = "-m -P -charset filename=UTF8 -overwrite_original_in_place";
    private int _pdfPreviewWidth = 1400;
    private int _pdfPreviewHeight = 1800;

    public string WatchFolder { get => _watchFolder; set => SetProperty(ref _watchFolder, value); }
    public string DestinationRootFolder { get => _destinationRootFolder; set => SetProperty(ref _destinationRootFolder, value); }
    public string PresetsFilePath { get => _presetsFilePath; set => SetProperty(ref _presetsFilePath, value); }
    public bool WatchSubfolders { get => _watchSubfolders; set => SetProperty(ref _watchSubfolders, value); }
    public bool AutoStartWatcher { get => _autoStartWatcher; set => SetProperty(ref _autoStartWatcher, value); }
    public bool CopyBeforeMoveForCrossVolumeSafety { get => _copyBeforeMoveForCrossVolumeSafety; set => SetProperty(ref _copyBeforeMoveForCrossVolumeSafety, value); }
    public bool CreateOperationBackups { get => _createOperationBackups; set => SetProperty(ref _createOperationBackups, value); }
    public bool EnableOcrHook { get => _enableOcrHook; set => SetProperty(ref _enableOcrHook, value); }
    public string OcrExecutablePath { get => _ocrExecutablePath; set => SetProperty(ref _ocrExecutablePath, value); }
    public string FilenameTemplate { get => _filenameTemplate; set => SetProperty(ref _filenameTemplate, value); }
    public string AuditLogFilePath { get => _auditLogFilePath; set => SetProperty(ref _auditLogFilePath, value); }
    public string SidecarManifestFolder { get => _sidecarManifestFolder; set => SetProperty(ref _sidecarManifestFolder, value); }
    public bool UseExifTool { get => _useExifTool; set => SetProperty(ref _useExifTool, value); }
    public string ExifToolExecutablePath { get => _exifToolExecutablePath; set => SetProperty(ref _exifToolExecutablePath, value); }
    public string ExifToolAdditionalArgs { get => _exifToolAdditionalArgs; set => SetProperty(ref _exifToolAdditionalArgs, value); }
    public int PdfPreviewWidth { get => _pdfPreviewWidth; set => SetProperty(ref _pdfPreviewWidth, value); }
    public int PdfPreviewHeight { get => _pdfPreviewHeight; set => SetProperty(ref _pdfPreviewHeight, value); }

    public List<string> SupportedExtensions { get; set; } =
    [
        ".jpg", ".jpeg", ".tif", ".tiff", ".png", ".pdf"
    ];
}
