using System;
using System.Collections.Generic;

namespace CursorPresetTool.Core.Models
{
    /// <summary>
    /// カーソル設定のバックアップを表すモデル。
    /// レジストリ適用前の状態を保存しておき、
    /// 失敗時やユーザー操作でロールバックできるようにする。
    /// </summary>
    public sealed class BackupSnapshot
    {
        /// <summary>
        /// バックアップを取った日時（ローカル時刻）。
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Windows の各カーソルスロットに対応する
        /// レジストリ上の実際のパスを保存しておく。
        /// ここでは抽象的に "slot名→実パス" としている。
        /// </summary>
        public IReadOnlyDictionary<CursorSlotId, string> CursorPaths { get; }

        /// <summary>
        /// バックアップファイルが保存されているパス（任意）。
        /// メタ情報として持っておく。
        /// </summary>
        public string? StoragePath { get; }

        public BackupSnapshot(
            DateTime createdAt,
            IDictionary<CursorSlotId, string> cursorPaths,
            string? storagePath = null)
        {
            CreatedAt = createdAt;
            CursorPaths = new Dictionary<CursorSlotId, string>(cursorPaths);
            StoragePath = storagePath;
        }
    }
}
