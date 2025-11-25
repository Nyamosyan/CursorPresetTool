using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CursorPresetTool.Core.Models;

namespace CursorPresetTool.Core.Packs
{
    /// <summary>
    /// ZIP / フォルダからプリセットディレクトリへ
    /// パックをインポートする実装。
    /// 
    /// ポイント：
    /// - 内部表現は常に「フォルダパック」に統一
    /// - ZIP 内部の構造に関わらず、pack.json があるフォルダを「ルート」として扱う
    /// - そのフォルダの中身だけを presets/{Name}/... にコピーするので、
    ///   ZIP からエクスポートしても name/name/pack.json にはならない
    /// </summary>
    public sealed class PackImporter : IPackImporter
    {
        private readonly IPackReader _reader;
        private readonly string _presetRootDirectory;

        /// <param name="reader">pack.json を読み込むためのリーダー。</param>
        /// <param name="presetRootDirectory">プリセットを保存するルートディレクトリ（絶対パス）。</param>
        public PackImporter(IPackReader reader, string presetRootDirectory)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));

            if (string.IsNullOrWhiteSpace(presetRootDirectory))
                throw new ArgumentException("Preset root directory must not be null or empty.", nameof(presetRootDirectory));

            _presetRootDirectory = Path.GetFullPath(presetRootDirectory);
            Directory.CreateDirectory(_presetRootDirectory);
        }

        public PresetInfo ImportFromFolder(string sourceFolder)
        {
            if (string.IsNullOrWhiteSpace(sourceFolder))
                throw new ArgumentException("Source folder must not be null or empty.", nameof(sourceFolder));

            sourceFolder = Path.GetFullPath(sourceFolder);
            if (!Directory.Exists(sourceFolder))
                throw new DirectoryNotFoundException($"Source folder does not exist: {sourceFolder}");

            // pack.json を含むフォルダとして読み込む
            var pack = _reader.LoadFromFolder(sourceFolder);

            // プリセット名（フォルダ名）を決める
            var presetName = CreateSafePresetName(pack.PackName, Path.GetFileName(sourceFolder));
            var targetFolder = EnsureUniquePresetFolder(presetName);

            // pack.json があるフォルダをルートとして、その中身を targetFolder にコピー
            CopyDirectoryContents(sourceFolder, targetFolder);

            // 改めて targetFolder から pack を読み込んでおくと、
            // 将来的に pack.json を変換したいときなどにも安全
            var importedPack = _reader.LoadFromFolder(targetFolder);

            return new PresetInfo(presetName, targetFolder, importedPack);
        }

        public PresetInfo ImportFromZip(string zipFilePath)
        {
            if (string.IsNullOrWhiteSpace(zipFilePath))
                throw new ArgumentException("Zip file path must not be null or empty.", nameof(zipFilePath));
            if (!File.Exists(zipFilePath))
                throw new FileNotFoundException("Zip file not found.", zipFilePath);

            // 一時フォルダに展開
            var tempRoot = Path.Combine(Path.GetTempPath(), "CursorPresetTool", "import");
            Directory.CreateDirectory(tempRoot);

            var tempDirName = Path.GetFileNameWithoutExtension(zipFilePath) + "_" + Guid.NewGuid().ToString("N");
            var tempDir = Path.Combine(tempRoot, tempDirName);
            Directory.CreateDirectory(tempDir);

            try
            {
                ZipFile.ExtractToDirectory(zipFilePath, tempDir);

                // 展開先から pack.json を探す（複数あったら最初のもの）
                var packJsonFiles = Directory.GetFiles(tempDir, "pack.json", SearchOption.AllDirectories);
                if (packJsonFiles.Length == 0)
                {
                    throw new PackReadException("pack.json not found in zip.", zipFilePath);
                }

                var packJsonPath = packJsonFiles[0];
                var packFolder = Path.GetDirectoryName(packJsonPath)
                                 ?? throw new PackReadException("Failed to get directory of pack.json.", zipFilePath);

                // pack.json のあるフォルダを「ルート」として読み込む
                var pack = _reader.LoadFromFolder(packFolder);

                var presetName = CreateSafePresetName(
                    pack.PackName,
                    Path.GetFileNameWithoutExtension(zipFilePath));

                var targetFolder = EnsureUniquePresetFolder(presetName);

                // packFolder の中身だけを targetFolder にコピー
                CopyDirectoryContents(packFolder, targetFolder);

                var importedPack = _reader.LoadFromFolder(targetFolder);

                return new PresetInfo(presetName, targetFolder, importedPack);
            }
            finally
            {
                // 一時フォルダは用が済んだら削除
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, recursive: true);
                    }
                }
                catch
                {
                    // 後始末失敗は致命的ではないので握りつぶす
                }
            }
        }

        /// <summary>
        /// PackName とフォルダ名候補から、ファイルシステムに安全なプリセット名を作る。
        /// </summary>
        private static string CreateSafePresetName(string? packName, string? fallbackName)
        {
            var baseName = string.IsNullOrWhiteSpace(packName)
                ? (fallbackName ?? "Preset")
                : packName;

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                baseName = baseName.Replace(c, '_');
            }

            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "Preset";
            }

            return baseName.Trim();
        }

        /// <summary>
        /// プリセットルート内で一意なフォルダパスを確保する。
        /// 同名フォルダが存在する場合は "Name_1", "Name_2" のように連番を付与する。
        /// </summary>
        private string EnsureUniquePresetFolder(string presetName)
        {
            var basePath = Path.Combine(_presetRootDirectory, presetName);
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
                return basePath;
            }

            int index = 1;
            while (true)
            {
                var candidateName = $"{presetName}_{index}";
                var candidatePath = Path.Combine(_presetRootDirectory, candidateName);

                if (!Directory.Exists(candidatePath))
                {
                    Directory.CreateDirectory(candidatePath);
                    return candidatePath;
                }

                index++;
            }
        }

        /// <summary>
        /// sourceDir の中にあるファイル・ディレクトリを
        /// すべて destDir 直下にコピーする。
        /// (sourceDir 自体を作らないので、name/name/pack.json にはならない)
        /// </summary>
        private static void CopyDirectoryContents(string sourceDir, string destDir)
        {
            foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(sourceDir, dir);
                var targetSubDir = Path.Combine(destDir, relative);
                Directory.CreateDirectory(targetSubDir);
            }

            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(sourceDir, file);
                var targetPath = Path.Combine(destDir, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                File.Copy(file, targetPath, overwrite: true);
            }
        }
    }
}
