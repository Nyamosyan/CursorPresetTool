using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using CursorPresetTool.Core.Models;

namespace CursorPresetTool.Core.Packs
{
    /// <summary>
    /// フォルダまたは ZIP から pack.json を読み込み、
    /// <see cref="CursorPack"/> として返す実装クラス。
    /// 
    /// JSON パース部分は <see cref="System.Text.Json"/> を利用し、
    /// その後 <see cref="CursorPack.SyncFromRawCursorMap"/> を呼び出して
    /// アプリ内部用の <see cref="CursorMap"/> に変換する。
    /// </summary>
    public sealed class PackReader : IPackReader
    {
        // pack.json のファイル名は現時点では固定。
        private const string PackFileName = "pack.json";

        // JSON の読み書きに使うオプション。
        public static JsonSerializerOptions DefaultJsonOptions => JsonOptions;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true, // "PackName" / "packname" とかをゆるく許容
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true
        };

        /// <inheritdoc />
        public CursorPack LoadFromFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("Folder path must not be null or empty.", nameof(folderPath));

            if (!Directory.Exists(folderPath))
                throw new PackReadException($"Folder does not exist: {folderPath}", folderPath);

            var packJsonPath = Path.Combine(folderPath, PackFileName);
            if (!File.Exists(packJsonPath))
                throw new PackReadException($"pack.json not found in folder: {folderPath}", folderPath);

            try
            {
                using var stream = File.OpenRead(packJsonPath);
                var cursorPack = DeserializePack(stream);

                // ソース情報を付与
                cursorPack.SourceType = PackSourceType.Folder;
                cursorPack.SourcePath = folderPath;

                // 軽い妥当性チェック
                ValidateOrThrow(cursorPack, packJsonPath);

                return cursorPack;
            }
            catch (PackReadException)
            {
                // すでに PackReadException にラップされている場合はそのまま飛ばす
                throw;
            }
            catch (Exception ex)
            {
                throw new PackReadException($"Failed to read pack.json from folder: {folderPath}", ex, folderPath);
            }
        }

        /// <inheritdoc />
        public CursorPack LoadFromZip(string zipFilePath)
        {
            if (string.IsNullOrWhiteSpace(zipFilePath))
                throw new ArgumentException("Zip file path must not be null or empty.", nameof(zipFilePath));

            if (!File.Exists(zipFilePath))
                throw new PackReadException($"Zip file does not exist: {zipFilePath}", zipFilePath);

            try
            {
                using var zipStream = File.OpenRead(zipFilePath);
                using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);

                // ZIP 内の pack.json を探す。
                // ルート直下想定だが、"someDir/pack.json" のようなパターンも許容するなら
                // ここでロジックを調整する。
                var entry = FindPackJsonEntry(zip);
                if (entry is null)
                    throw new PackReadException($"pack.json not found in zip: {zipFilePath}", zipFilePath);

                using var entryStream = entry.Open();
                var cursorPack = DeserializePack(entryStream);

                cursorPack.SourceType = PackSourceType.Zip;
                cursorPack.SourcePath = zipFilePath;

                // 軽い妥当性チェック
                ValidateOrThrow(cursorPack, zipFilePath);

                return cursorPack;
            }
            catch (PackReadException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new PackReadException($"Failed to read pack.json from zip: {zipFilePath}", ex, zipFilePath);
            }
        }

        /// <summary>
        /// 共通の JSON デシリアライズ処理。
        /// RawCursorMap から CursorMap への同期もここで行う。
        /// </summary>
        private static CursorPack DeserializePack(Stream jsonStream)
        {
            var pack = JsonSerializer.Deserialize<CursorPack>(jsonStream, JsonOptions);
            if (pack is null)
            {
                throw new PackReadException("Failed to deserialize pack.json (result was null).");
            }

            // RawCursorMap → CursorMap へ変換
            pack.SyncFromRawCursorMap();

            return pack;
        }

        /// <summary>
        /// ZIP 内から pack.json のエントリを探す。
        /// 
        /// 現状は「ファイル名が pack.json の最初のエントリ」を採用する。
        /// ルート直下に限定したい場合は FullName のチェックを絞る。
        /// </summary>
        private static ZipArchiveEntry? FindPackJsonEntry(ZipArchive zip)
        {
            foreach (var entry in zip.Entries)
            {
                if (string.Equals(entry.Name, PackFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// <see cref="CursorPack"/> の基本的な妥当性チェックを行い、
        /// 問題があれば <see cref="PackReadException"/> を投げる。
        /// </summary>
        private static void ValidateOrThrow(CursorPack pack, string? contextPath)
        {
            var errors = pack.Validate();
            if (errors.Count > 0)
            {
                var message = $"Invalid pack.json";

                if (!string.IsNullOrWhiteSpace(contextPath))
                {
                    message += $" at '{contextPath}'";
                }

                message += ":\n - " + string.Join("\n - ", errors);

                throw new PackReadException(message, contextPath);
            }
        }
    }
}
