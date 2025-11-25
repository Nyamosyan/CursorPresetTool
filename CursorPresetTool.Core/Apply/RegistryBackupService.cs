using System;
using System.Collections.Generic;
using Microsoft.Win32;
using CursorPresetTool.Core.Models;

namespace CursorPresetTool.Core.Apply
{
    /// <summary>
    /// Windows レジストリからカーソル設定をバックアップ／リストアする実装。
    ///
    /// 対象キー:
    ///   HKEY_CURRENT_USER\Control Panel\Cursors
    ///
    /// 対象スロット:
    ///   CursorSlot.All に定義されたすべてのスロット
    ///   （Arrow, Help, AppStarting, Wait, Crosshair, IBeam, NWPen, No, SizeNS, SizeWE,
    ///    SizeNWSE, SizeNESW, SizeAll, UpArrow, Hand, Pin, Person など）
    /// </summary>
    public sealed class RegistryBackupService : IRegistryBackupService
    {
        private const string CursorsKeyPath = @"Control Panel\Cursors";

        public BackupSnapshot CreateBackup()
        {
            using var key = Registry.CurrentUser.OpenSubKey(CursorsKeyPath, writable: false);
            if (key is null)
            {
                throw new InvalidOperationException(
                    $@"Unable to open HKCU\{CursorsKeyPath} for backup.");
            }

            var dict = new Dictionary<CursorSlotId, string>();

            // CursorSlot.All を唯一の定義ソースとして使用
            foreach (var slot in CursorSlot.All)
            {
                var value = key.GetValue(slot.Key) as string ?? string.Empty;
                dict[slot.Id] = value;
            }

            return new BackupSnapshot(DateTime.Now, dict);
        }

        public void Restore(BackupSnapshot snapshot)
        {
            using var key = Registry.CurrentUser.OpenSubKey(CursorsKeyPath, writable: true);
            if (key is null)
            {
                throw new InvalidOperationException(
                    $@"Unable to open HKCU\{CursorsKeyPath} for restore.");
            }

            foreach (var pair in snapshot.CursorPaths)
            {
                // Id → CursorSlot を引き、そこからレジストリ値名（Key）を取得
                var slot = CursorSlot.FromId(pair.Key);
                if (slot is null)
                {
                    // 定義から外れたスロットは無視
                    continue;
                }

                key.SetValue(slot.Key, pair.Value ?? string.Empty, RegistryValueKind.String);
            }
        }

        /// <summary>
        /// CursorSlotId に対応するレジストリ値名を取得するヘルパ。
        /// （既存コードとの互換用に残しておく）
        /// </summary>
        public static bool TryGetRegistryName(CursorSlotId slotId, out string? registryName)
        {
            var slot = CursorSlot.FromId(slotId);
            if (slot is null)
            {
                registryName = null;
                return false;
            }

            registryName = slot.Key;
            return true;
        }
    }
}
