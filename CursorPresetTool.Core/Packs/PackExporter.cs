using System;
using System.IO;
using System.IO.Compression;

namespace CursorPresetTool.Core.Packs
{
    /// <summary>
    /// プリセットフォルダを ZIP 形式でエクスポートする実装。
    /// 
    /// ZIP 内部のパスは「プリセットフォルダからの相対パス」にするので、
    /// name/name/pack.json のような二重構造にはならない。
    /// </summary>
    public sealed class PackExporter : IPackExporter
    {
        public void ExportToZip(string presetFolderPath, string destinationZipPath, bool overwrite = false)
        {
            if (string.IsNullOrWhiteSpace(presetFolderPath))
                throw new ArgumentException("Preset folder path must not be null or empty.", nameof(presetFolderPath));
            if (!Directory.Exists(presetFolderPath))
                throw new DirectoryNotFoundException($"Preset folder does not exist: {presetFolderPath}");

            destinationZipPath = Path.GetFullPath(destinationZipPath);

            var destDir = Path.GetDirectoryName(destinationZipPath);
            if (!string.IsNullOrWhiteSpace(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            if (File.Exists(destinationZipPath))
            {
                if (!overwrite)
                {
                    throw new IOException($"Destination zip already exists: {destinationZipPath}");
                }

                File.Delete(destinationZipPath);
            }

            var root = Path.GetFullPath(presetFolderPath);

            using var zipStream = File.OpenWrite(destinationZipPath);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

            foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                // プリセットフォルダからの相対パスを ZIP 内のパスにする
                var relativePath = Path.GetRelativePath(root, file);

                // Windowsのパス区切り（\）を ZIP 用に（/）に変換しておくとより安全
                var entryName = relativePath.Replace('\\', '/');

                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var fileStream = File.OpenRead(file);
                fileStream.CopyTo(entryStream);
            }
        }
    }
}
