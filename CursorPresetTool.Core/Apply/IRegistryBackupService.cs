using CursorPresetTool.Core.Models;

namespace CursorPresetTool.Core.Apply
{
    /// <summary>
    /// 現在のカーソル設定をバックアップ／復元するためのインターフェース。
    /// </summary>
    public interface IRegistryBackupService
    {
        /// <summary>
        /// 現在のカーソル設定をバックアップする。
        /// </summary>
        /// <returns>バックアップスナップショット。</returns>
        BackupSnapshot CreateBackup();

        /// <summary>
        /// 指定されたバックアップスナップショットを元にカーソル設定を復元する。
        /// </summary>
        /// <param name="snapshot">復元元のスナップショット。</param>
        void Restore(BackupSnapshot snapshot);
    }
}
