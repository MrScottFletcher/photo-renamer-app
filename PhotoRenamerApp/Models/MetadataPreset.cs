using PhotoRenamerApp.Infrastructure;

namespace PhotoRenamerApp.Models;

public sealed class MetadataPreset : ObservableObject
{
    private string _name = string.Empty;
    private string _box = string.Empty;
    private string _dateTaken = string.Empty;
    private string _artist = string.Empty;
    private string _subject = string.Empty;
    private string _location = string.Empty;
    private string _folderPrefix = string.Empty;
    private string _folderSuffix = string.Empty;
    private string _briefNote = string.Empty;

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Box { get => _box; set => SetProperty(ref _box, value); }
    public string DateTaken { get => _dateTaken; set => SetProperty(ref _dateTaken, value); }
    public string Artist { get => _artist; set => SetProperty(ref _artist, value); }
    public string Subject { get => _subject; set => SetProperty(ref _subject, value); }
    public string Location { get => _location; set => SetProperty(ref _location, value); }
    public string FolderPrefix { get => _folderPrefix; set => SetProperty(ref _folderPrefix, value); }
    public string FolderSuffix { get => _folderSuffix; set => SetProperty(ref _folderSuffix, value); }
    public string BriefNote { get => _briefNote; set => SetProperty(ref _briefNote, value); }
}
