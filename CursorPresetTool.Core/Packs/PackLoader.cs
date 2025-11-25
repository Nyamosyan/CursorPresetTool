using System;
using System.IO;
using System.IO.Compression;
using CursorPresetTool.Core.Models;

namespace CursorPresetTool.Core.Packs
{
    /// <summary>
    /// フォルダパック／ZIPパックを読み込み、
    /// <see cref="LoadedPack"/> として返す高レベル API。
    /// 
    /// GUI / CLI の両方から使うことを想定している。
    /// </summary>
    public sealed class PackLoader
    {
        private readonly IPackReader _reader;

        public PackLoader(IPackReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        /// <summary>
        /// フォルダパスからパックを読み込む。
        /// </summary>
        public LoadedPack LoadFromFolder(string folderPath)
        {
            var pack = _reader.LoadFromFolder(folderPath);
            return new LoadedPack(pack, baseDirectory: folderPath);
        }

        /// <summary>
        /// ZIPファイルからパックを読み込み、一時フォルダに展開したカーソルを使えるようにする。
        /// </summary>
        public LoadedPack LoadFromZip(string zipFilePath)
        {
            if (string.IsNullOrWhiteSpace(zipFilePath))
                throw new ArgumentException("Zip file path must not be null or empty.", nameof(zipFilePath));
            if (!File.Exists(zipFilePath))
                throw new PackReadException($"Zip file does not exist: {zipFilePath}", zipFilePath);

            // 一時フォルダルート: %TEMP%\CursorPresetTool\packs
            var tempRoot = Path.Combine(Path.GetTempPath(), "CursorPresetTool", "packs");
            Directory.CreateDirectory(tempRoot);

            var tempDirName = Path.GetFileNameWithoutExtension(zipFilePath) + "_" + Guid.NewGuid().ToString("N");
            var tempDir = Path.Combine(tempRoot, tempDirName);
            Directory.CreateDirectory(tempDir);

            // ZIP展開
            ZipFile.ExtractToDirectory(zipFilePath, tempDir);

            // pack.json を再帰検索
            var packJsonFiles = Directory.GetFiles(tempDir, "pack.json", SearchOption.AllDirectories);
            if (packJsonFiles.Length == 0)
            {
                throw new PackReadException("pack.json not found in zip.", zipFilePath);
            }

            var packJsonPath = packJsonFiles[0];
            var packFolder = Path.GetDirectoryName(packJsonPath)
                             ?? throw new PackReadException("Failed to get directory of pack.json.", zipFilePath);

            var pack = _reader.LoadFromFolder(packFolder);
            pack.SourceType = PackSourceType.Zip;
            pack.SourcePath = zipFilePath;

            return new LoadedPack(pack, baseDirectory: packFolder, tempDirectory: tempDir);
        }
    }
}
