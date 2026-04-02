using System.Collections.Concurrent;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Docnet.Core;
using Docnet.Core.Models;
using PhotoRenamerApp.Models;
using System.IO;


namespace PhotoRenamerApp.Services;

public sealed class PreviewService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    public void LoadPreview(FileItem item, AppConfig config)
    {
        try
        {
            if (TryLoadCached(item, out var cachedImage, out var cachedLabel))
            {
                item.PreviewImage = cachedImage;
                item.PreviewLabel = cachedLabel;
                return;
            }

            if (item.Extension is ".jpg" or ".jpeg" or ".png" or ".tif" or ".tiff")
            {
                var image = LoadBitmapPreview(item.CurrentPath, 900);
                item.PreviewImage = image;
                item.PreviewLabel = "Image preview";
                Cache(item, image, item.PreviewLabel);
                return;
            }

            if (item.Extension == ".pdf")
            {
                var image = RenderPdfFirstPage(item.CurrentPath, config);
                item.PreviewImage = image;
                item.PreviewLabel = "PDF first page";
                Cache(item, image, item.PreviewLabel);
                return;
            }

            item.PreviewImage = null;
            item.PreviewLabel = "Preview unavailable";
        }
        catch (Exception ex)
        {
            item.PreviewImage = null;
            item.PreviewLabel = $"Preview failed: {ex.Message}";
        }
    }

    private static BitmapSource LoadBitmapPreview(string path, int decodeWidth)
    {
        using (var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite)) // allows other processes to access too
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad; // CRITICAL
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze(); // optional but recommended

            return image;
        } // stream is CLOSED here
    }

    private static BitmapSource RenderPdfFirstPage(string path, AppConfig config)
    {
        using var docReader = DocLib.Instance.GetDocReader(path, new PageDimensions(config.PdfPreviewWidth, config.PdfPreviewHeight));
        using var pageReader = docReader.GetPageReader(0);

        var rawBytes = pageReader.GetImage();
        var width = pageReader.GetPageWidth();
        var height = pageReader.GetPageHeight();
        var stride = width * 4;

        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, rawBytes, stride);
        bitmap.Freeze();
        return bitmap;
    }

    private bool TryLoadCached(FileItem item, out BitmapSource? image, out string label)
    {
        image = null;
        label = string.Empty;

        if (_cache.TryGetValue(item.CurrentPath, out var cacheEntry))
        {
            var lastWrite = File.GetLastWriteTimeUtc(item.CurrentPath);
            if (cacheEntry.LastWriteUtc == lastWrite)
            {
                image = cacheEntry.Image;
                label = cacheEntry.Label;
                return true;
            }

            _cache.TryRemove(item.CurrentPath, out _);
        }

        return false;
    }

    private void Cache(FileItem item, BitmapSource image, string label)
    {
        _cache[item.CurrentPath] = new CacheEntry(File.GetLastWriteTimeUtc(item.CurrentPath), image, label);
    }

    private sealed record CacheEntry(DateTime LastWriteUtc, BitmapSource Image, string Label);
}
