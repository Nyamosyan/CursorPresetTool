using CursorPresetTool.Core.Apply;
using CursorPresetTool.Core.Models;
using CursorPresetTool.Core.Packs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CursorPresetTool.Gui.Services
{
    /// <summary>
    /// Core 側の機能（Pack 読み込み / インポート / エクスポート / 適用）
    /// を GUI から使いやすい形にまとめたサービス。
    /// </summary>
    public sealed class PresetService
    {
        private readonly string _presetRoot;
        private readonly IPackReader _packReader;
        private readonly IPackImporter _packImporter;
        private readonly IPackExporter _packExporter;
        private readonly ICursorApplier _cursorApplier;

        public string PresetRoot => _presetRoot;

        public PresetService(string presetRoot)
        {
            _presetRoot = Path.GetFullPath(presetRoot);
            Directory.CreateDirectory(_presetRoot);

            _packReader = new PackReader();
            _packImporter = new PackImporter(_packReader, _presetRoot);
            _packExporter = new PackExporter();

            var backupService = new RegistryBackupService();
            _cursorApplier = new CursorApplier(backupService);
        }

        /// <summary>
        /// presets フォルダ内に存在するすべてのプリセットを列挙する。
        /// </summary>
        public IEnumerable<PresetInfo> LoadAllPresets()
        {
            if (!Directory.Exists(_presetRoot))
            {
                yield break;
            }

            foreach (var dir in Directory.GetDirectories(_presetRoot))
            {
                var packJson = Path.Combine(dir, "pack.json");
                if (!File.Exists(packJson))
                    continue;

                CursorPack pack;
                try
                {
                    pack = _packReader.LoadFromFolder(dir);
                }
                catch
                {
                    continue; // 壊れたプリセットはスキップ
                }

                var name = Path.GetFileName(dir);
                yield return new PresetInfo(name, dir, pack);
            }
        }

        /// <summary>
        /// フォルダからプリセットをインポートする。
        /// </summary>
        public PresetInfo ImportFromFolder(string sourceFolder)
        {
            return _packImporter.ImportFromFolder(sourceFolder);
        }

        /// <summary>
        /// ZIP からプリセットをインポートする。
        /// </summary>
        public PresetInfo ImportFromZip(string zipPath)
        {
            return _packImporter.ImportFromZip(zipPath);
        }

        /// <summary>
        /// 指定されたプリセットを適用する。
        /// </summary>
        public void ApplyPreset(PresetInfo preset)
        {
            var pack = preset.Pack;
            var baseDir = preset.FolderPath;

            _cursorApplier.ApplyPack(pack, baseDir, createBackup: true);
        }

        /// <summary>
        /// 指定されたプリセットを ZIP としてエクスポートする。
        /// </summary>
        /// <param name="preset">エクスポート対象のプリセット。</param>
        /// <param name="destinationZipPath">出力先 ZIP パス。</param>
        /// <param name="overwrite">既存ファイルを上書きするかどうか。</param>
        public void ExportPresetToZip(PresetInfo preset, string destinationZipPath, bool overwrite)
        {
            _packExporter.ExportToZip(preset.FolderPath, destinationZipPath, overwrite);
        }

        /// <summary>
        /// プリセットフォルダを削除する。
        /// </summary>
        public void DeletePreset(PresetInfo preset)
        {
            if (preset == null) throw new ArgumentNullException(nameof(preset));

            if (Directory.Exists(preset.FolderPath))
            {
                Directory.Delete(preset.FolderPath, recursive: true);
            }
        }

        /// <summary>
        /// プリセット名とフォルダ名を変更する。
        /// </summary>
        /// <param name="preset">対象プリセット。</param>
        /// <param name="newName">新しいプリセット名（フォルダ名）。</param>
        /// <returns>更新後のプリセット情報。</returns>
        public PresetInfo RenamePreset(PresetInfo preset, string newName)
        {
            if (preset == null) throw new ArgumentNullException(nameof(preset));
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("新しい名前が空です。", nameof(newName));

            // ファイル名に使えない文字を弾く
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                if (newName.Contains(c))
                {
                    throw new ArgumentException("プリセット名に使用できない文字が含まれています。", nameof(newName));
                }
            }

            newName = newName.Trim();
            var oldFolder = preset.FolderPath;
            var newFolder = Path.Combine(_presetRoot, newName);

            if (string.Equals(oldFolder, newFolder, StringComparison.OrdinalIgnoreCase))
            {
                // フォルダパスが変わらないなら何もしない
                return preset;
            }

            if (Directory.Exists(newFolder))
            {
                throw new IOException($"同名のプリセットがすでに存在します: {newName}");
            }

            if (!Directory.Exists(oldFolder))
            {
                throw new DirectoryNotFoundException($"元のプリセットフォルダが見つかりません: {oldFolder}");
            }

            Directory.Move(oldFolder, newFolder);

            // pack.json を読み直して新しい PresetInfo を作る
            var pack = _packReader.LoadFromFolder(newFolder);
            return new PresetInfo(newName, newFolder, pack);
        }

        /// <summary>
        /// 選択中のプリセットを複製する。
        /// </summary>
        /// <param name="preset">複製元のプリセット。</param>
        /// <returns>複製された新しいプリセット情報。</returns>
        public PresetInfo DuplicatePreset(PresetInfo preset)
        {
            if (preset == null) throw new ArgumentNullException(nameof(preset));

            var sourceFolder = preset.FolderPath;
            if (!Directory.Exists(sourceFolder))
            {
                throw new DirectoryNotFoundException($"元のプリセットフォルダが見つかりません: {sourceFolder}");
            }

            // ベース名："元フォルダ名_Copy"
            var baseName = preset.Name + "_Copy";
            var targetFolder = Path.Combine(_presetRoot, baseName);

            // 同名があれば連番を振る
            int index = 1;
            while (Directory.Exists(targetFolder))
            {
                var candidateName = $"{baseName}_{index}";
                targetFolder = Path.Combine(_presetRoot, candidateName);
                index++;
            }

            // フォルダ丸ごとコピー
            CopyDirectoryRecursive(sourceFolder, targetFolder);

            // 新しい pack.json を読み込んで PresetInfo を作り直す
            var pack = _packReader.LoadFromFolder(targetFolder);
            var folderName = Path.GetFileName(targetFolder)!;
            return new PresetInfo(folderName, targetFolder, pack);
        }

        // ヘルパー：フォルダを再帰コピー
        private static void CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destPath = Path.Combine(destDir, fileName);
                File.Copy(file, destPath, overwrite: false);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                var destSubDir = Path.Combine(destDir, dirName);
                CopyDirectoryRecursive(dir, destSubDir);
            }
        }

        /// <summary>
        /// 新しいプリセットを指定フォルダに保存し、その内容で再読み込みした PresetInfo を返します。
        /// </summary>
        /// <param name="targetFolder">プリセットを保存するフォルダのパス。</param>
        /// <param name="pack">保存対象となるカーソルパックの内容。</param>
        /// <param name="relativePathsToFullPaths">
        /// pack.CursorMap に含まれる相対パスと、実際のカーソルファイルのフルパスとの対応表。
        /// 値が null の場合はファイルコピーを行わず、pack.json のみを更新します。
        /// </param>
        /// <returns>保存後の内容で再読み込みされた PresetInfo。</returns>
        public PresetInfo SaveNewPreset(string targetFolder, CursorPack pack, IReadOnlyDictionary<string, string?> relativePathsToFullPaths)
        {
            if (string.IsNullOrWhiteSpace(targetFolder))
                throw new ArgumentException("targetFolder が空です。", nameof(targetFolder));

            // フォルダ作成（存在すれば上書き前提で使用される）
            Directory.CreateDirectory(targetFolder);

            // 1. pack.json 書き込み
            WritePackToFolder(targetFolder, pack, relativePathsToFullPaths);

            // 2. 各カーソルファイルをコピー（相対パス → 実ファイルパス）
            foreach (var kv in relativePathsToFullPaths)
            {
                var relative = kv.Key;          // 例: "Arrow.cur"
                var fullPath = kv.Value;        // 実ファイルパス（null の場合 Missing）

                if (string.IsNullOrWhiteSpace(relative) || string.IsNullOrWhiteSpace(fullPath))
                    continue;

                var dest = Path.Combine(targetFolder, relative);

                // 必要ならサブフォルダを生成（現状は使わない想定）
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

                File.Copy(fullPath, dest, overwrite: true);
            }

            // pack.json を元に PresetInfo 再ロード
            var packReloaded = _packReader.LoadFromFolder(targetFolder);
            var folderName = Path.GetFileName(targetFolder)!;

            return new PresetInfo(folderName, targetFolder, packReloaded);
        }

        /// <summary>
        /// 既存のプリセットフォルダに対して内容を上書き保存し、その内容で再読み込みした PresetInfo を返します。
        /// </summary>
        /// <param name="existingFolder">上書き対象となる既存プリセットフォルダのパス。</param>
        /// <param name="pack">保存対象となるカーソルパックの内容。</param>
        /// <param name="relativePathsToFullPaths">
        /// pack.CursorMap に含まれる相対パスと、実際のカーソルファイルのフルパスとの対応表。
        /// 値が null の場合はファイルコピーを行わず、pack.json の更新と不要ファイルの削除のみ行います。
        /// </param>
        /// <returns>保存後の内容で再読み込みされた PresetInfo。</returns>
        public PresetInfo SaveExistingPreset(string existingFolder, CursorPack pack, IReadOnlyDictionary<string, string?> relativePathsToFullPaths)
        {
            if (!Directory.Exists(existingFolder))
                throw new DirectoryNotFoundException($"既存プリセットフォルダが見つかりません: {existingFolder}");

            // ----- 1. 既存ファイルの一覧取得 -----
            var existingFiles = new HashSet<string>(
                Directory.GetFiles(existingFolder),
                StringComparer.OrdinalIgnoreCase);

            // ----- 2. 必要なファイルを書き込み or コピー -----
            var usedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in relativePathsToFullPaths)
            {
                var relative = kv.Key;
                var fullPath = kv.Value;

                if (string.IsNullOrWhiteSpace(relative) || string.IsNullOrWhiteSpace(fullPath))
                    continue;

                var dest = Path.Combine(existingFolder, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

                File.Copy(fullPath, dest, overwrite: true);
                usedFiles.Add(dest);
            }

            // ----- 3. 不要になったファイルを削除 -----
            foreach (var oldFile in existingFiles)
            {
                if (Path.GetFileName(oldFile).Equals("pack.json", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!usedFiles.Contains(oldFile))
                {
                    try { File.Delete(oldFile); }
                    catch { /* ロック中などは無視 */ }
                }
            }

            // ----- 4. pack.json 上書き -----
            WritePackToFolder(existingFolder, pack, relativePathsToFullPaths);

            // ----- 5. 再ロードして PresetInfo として返す -----
            var newPack = _packReader.LoadFromFolder(existingFolder);
            var folderName = Path.GetFileName(existingFolder)!;

            return new PresetInfo(folderName, existingFolder, newPack);
        }

        /// <summary>
        /// pack.json の書き込みと、カーソルファイル（相対→実ファイルパス）のコピーを行う。
        /// SaveNewPreset / SaveExistingPreset から内部的に使用される。
        /// </summary>
        private void WritePackToFolder(
            string targetFolder,
            CursorPack pack,
            IReadOnlyDictionary<string, string?> relativePathsToFullPaths)
        {
            if (targetFolder is null) throw new ArgumentNullException(nameof(targetFolder));

            Directory.CreateDirectory(targetFolder);

            // 1. CursorMap → RawCursorMap に反映
            pack.SyncToRawCursorMap();

            // 2. pack.json の書き込み
            var packJsonPath = Path.Combine(targetFolder, "pack.json");
            using (var stream = File.Create(packJsonPath))
            {
                JsonSerializer.Serialize(stream, pack, PackReader.DefaultJsonOptions);
            }

            // 3. カーソルファイルのコピー
            foreach (var kv in relativePathsToFullPaths)
            {
                var relative = kv.Key;   // "Arrow.cur" など
                var fullPath = kv.Value; // 実ファイルの絶対パス

                if (string.IsNullOrWhiteSpace(relative) || string.IsNullOrWhiteSpace(fullPath))
                    continue;

                var dest = Path.Combine(targetFolder, relative);

                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(fullPath, dest, overwrite: true);
            }
        }

    }
}
