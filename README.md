# Photo Renamer and Categorizer - WPF App

This is a Windows desktop app in C#/.NET 8 for processing archival scans and camera captures.

## What changed in this version

- Metadata writing now runs through ExifTool instead of the earlier in-process PDF/JPEG metadata code.
- PDF preview now renders the first page to an actual bitmap thumbnail instead of showing a placeholder.
- Preview loading includes a simple in-memory cache keyed by file path and last-write timestamp.
- App config now includes ExifTool path and PDF render size settings.

## What it does

- Watches an incoming folder for new files.
- Supports JPG, JPEG, TIFF, PNG, and PDF.
- Loads app config from JSON at startup.
- Loads reusable metadata presets from JSON at startup.
- Lets you apply a preset to one or many files.
- Lets you edit file metadata per item after the preset is applied.
- Renames files based on date, artist, subject, and note.
- Writes metadata through ExifTool for images and PDFs.
- Moves processed files into a structured destination folder under a Box grouping.
- Writes an audit log for every update and move.
- Never exposes any delete operation in the UI or service layer.

## ExifTool setup

- Download the Windows executable build of ExifTool.
- Extract it to a stable folder such as `C:\Tools\exiftool`.
- Make sure `exiftool.exe` sits beside the `exiftool_files` folder.
- Point `ExifToolExecutablePath` in `appsettings.json` to that `exiftool.exe`.

## PDF preview setup

PDF first-page thumbnails are rendered in-process with `Docnet.Core`.

Config knobs:

- `PdfPreviewWidth`
- `PdfPreviewHeight`

These control the render canvas size used for the first-page preview.

## Suggested next features already worth adding

- Thumbnail strip for adjacent files in the same box or batch.
- Duplicate detection by SHA-256 hash.
- Batch note templates like `page 01`, `slide 02`, `front`, `back`.
- Dry-run mode that previews rename and move results without touching files.
- Undo journal for rename and move operations.
- Warning badges for missing year, subject, or box.
- CSV export of the processed manifest.
- ExifTool readback verification after write.
- OCR-assisted filename suggestions.

## Build

Open the solution in Visual Studio 2022 or later and restore NuGet packages.

NuGet packages used here:

- `Docnet.Core` for PDF first-page raster preview.

## Configuration files

Copy these starter files next to the executable and rename them:

- `appsettings.sample.json` -> `appsettings.json`
- `presets.sample.json` -> `presets.json`
