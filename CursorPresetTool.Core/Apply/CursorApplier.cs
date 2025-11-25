using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using CursorPresetTool.Core.Models;

namespace CursorPresetTool.Core.Apply
{
    /// <summary>
    /// カーソルパックを Windows に適用する実装。
    /// 
    /// 1. 必要に応じて現在のカーソル設定をバックアップ
    /// 2. pack.CursorMap からファイルパスを解決
    /// 3. レジストリに書き込み
    /// 4. SystemParametersInfo(SPI_SETCURSORS) で即時反映
    /// 5. 途中で失敗した場合はバックアップからロールバック
    /// </summary>
    public sealed class CursorApplier : ICursorApplier
    {
        private readonly IRegistryBackupService _backupService;

        public CursorApplier(IRegistryBackupService backupService)
        {
            _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        }

        /// <inheritdoc />
        public BackupSnapshot? ApplyPack(CursorPack pack, string baseDirectory, bool createBackup = true)
        {
            if (pack == null) throw new ArgumentNullException(nameof(pack));
            if (string.IsNullOrWhiteSpace(baseDirectory))
                throw new ArgumentException("Base directory must not be null or empty.", nameof(baseDirectory));

            BackupSnapshot? backup = null;

            try
            {
                if (createBackup)
                    backup = _backupService.CreateBackup();

                // 1) pack.CursorMap のスロット → フルパス解決（既存処理）
                var resolved = ResolveCursorPaths(pack, baseDirectory);

                // 2) レジストリ適用（既存処理）
                ApplyToRegistry(resolved);

                // 3) ★ Missing（＝CursorMap に無いスロット）を標準カーソルへ戻す
                ResetMissingSlotsToDefault(pack);

                // 4) カーソル反映
                if (!RefreshCursors())
                {
                    throw new CursorApplyException(
                        "SystemParametersInfo(SPI_SETCURSORS) failed to update cursors.");
                }

                return backup;
            }
            catch (Exception ex) when (ex is not CursorApplyException)
            {
                var wrapped = new CursorApplyException("Failed to apply cursor pack.", ex);

                if (backup != null)
                {
                    try
                    {
                        _backupService.Restore(backup);
                        RefreshCursors();
                    }
                    catch
                    {
                    }
                }

                throw wrapped;
            }
        }

        private static void ResetMissingSlotsToDefault(CursorPack pack)
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Cursors", writable: true);

            if (key == null)
                throw new CursorApplyException(
                    "Unable to open HKCU\\Control Panel\\Cursors for writing.");

            // pack.CursorMap に含まれていないスロット → Missing と判断
            var defined = pack.CursorMap.Paths; // Dictionary<CursorSlotId, string>

            foreach (CursorSlotId slot in Enum.GetValues(typeof(CursorSlotId)))
            {
                if (defined.ContainsKey(slot))
                    continue; // 設定があるスロット → 変更済み

                if (!RegistryBackupService.TryGetRegistryName(slot, out var regName) ||
                    string.IsNullOrEmpty(regName))
                    continue;

                // ★ 空文字を書き込む → Windows の標準カーソルに戻る
                key.SetValue(regName, string.Empty, Microsoft.Win32.RegistryValueKind.String);
            }
        }

        /// <summary>
        /// CursorPack から実際に適用するカーソルファイルのフルパスを解決する。
        /// </summary>
        private static IReadOnlyDictionary<CursorSlotId, string> ResolveCursorPaths(CursorPack pack, string baseDirectory)
        {
            var result = new Dictionary<CursorSlotId, string>();

            foreach (var kv in pack.CursorMap.Paths)
            {
                var slot = kv.Key;
                var relativePath = kv.Value;

                // 相対パスを結合
                var fullPath = Path.Combine(baseDirectory, relativePath);

                if (!File.Exists(fullPath))
                {
                    throw new CursorApplyException(
                        $"Cursor file not found for slot '{slot}': {fullPath}");
                }

                result[slot] = fullPath;
            }

            return result;
        }

        /// <summary>
        /// 解決済みのカーソルファイルパスをレジストリに書き込む。
        /// </summary>
        private static void ApplyToRegistry(IReadOnlyDictionary<CursorSlotId, string> paths)
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Cursors",
                writable: true);

            if (key is null)
            {
                throw new CursorApplyException(
                    @"Unable to open HKCU\Control Panel\Cursors for writing.");
            }

            foreach (var kv in paths)
            {
                if (!RegistryBackupService.TryGetRegistryName(kv.Key, out var regName) ||
                    string.IsNullOrEmpty(regName))
                {
                    // 未対応スロットは無視
                    continue;
                }

                key.SetValue(regName, kv.Value, Microsoft.Win32.RegistryValueKind.String);
            }
        }

        /// <summary>
        /// SystemParametersInfo を呼び出してカーソル設定を即時反映する。
        /// </summary>
        private static bool RefreshCursors()
        {
            const uint SPI_SETCURSORS = 0x0057;
            const uint SPIF_SENDCHANGE = 0x0002;

            return SystemParametersInfo(
                uiAction: SPI_SETCURSORS,
                uiParam: 0,
                pvParam: IntPtr.Zero,
                fWinIni: SPIF_SENDCHANGE);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(
            uint uiAction,
            uint uiParam,
            IntPtr pvParam,
            uint fWinIni);
    }
}
