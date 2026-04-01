using PhotoRenamerApp.Models;

namespace PhotoRenamerApp.Services;

public sealed class FileOperationService
{
    public string RenameFile(FileItem item, string template)
    {
        var directory = Path.GetDirectoryName(item.CurrentPath)!;
        var targetPath = MakeUnique(Path.Combine(directory, FilenameRuleEngine.BuildFileName(item, template)));

        if (!string.Equals(item.CurrentPath, targetPath, StringComparison.OrdinalIgnoreCase))
        {
            File.Move(item.CurrentPath, targetPath);
            item.CurrentPath = targetPath;
            item.DisplayName = Path.GetFileName(targetPath);
            item.Status = "Renamed";
        }

        return targetPath;
    }

    public string MoveToDestination(FileItem item, string destinationRoot, bool copyBeforeMove)
    {
        var targetDirectory = Path.Combine(destinationRoot, FilenameRuleEngine.BuildDestinationFolder(item));
        Directory.CreateDirectory(targetDirectory);

        var targetPath = MakeUnique(Path.Combine(targetDirectory, Path.GetFileName(item.CurrentPath)));

        if (copyBeforeMove)
        {
            File.Copy(item.CurrentPath, targetPath, overwrite: false);
            File.Delete(item.CurrentPath);
        }
        else
        {
            File.Move(item.CurrentPath, targetPath);
        }

        item.CurrentPath = targetPath;
        item.DisplayName = Path.GetFileName(targetPath);
        item.Status = "Moved";
        return targetPath;
    }

    public string CreateBackup(string fullPath)
    {
        var backupPath = fullPath + ".bak";
        File.Copy(fullPath, backupPath, overwrite: true);
        return backupPath;
    }

    private static string MakeUnique(string fullPath)
    {
        if (!File.Exists(fullPath)) return fullPath;
        var dir = Path.GetDirectoryName(fullPath)!;
        var name = Path.GetFileNameWithoutExtension(fullPath);
        var ext = Path.GetExtension(fullPath);
        for (var i = 2; i < 10000; i++)
        {
            var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            if (!File.Exists(candidate)) return candidate;
        }
        throw new IOException($"Could not create a unique file name for {fullPath}.");
    }
}
