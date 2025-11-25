using CursorPresetTool.Core.Models;

namespace CursorPresetTool.Core.Apply
{
    /// <summary>
    /// カーソルパックを Windows に適用するためのインターフェース。
    /// </summary>
    public interface ICursorApplier
    {
        /// <summary>
        /// 指定されたカーソルパックを適用する。
        /// 必要に応じて適用前にバックアップを取得し、失敗時はロールバックを試みる。
        /// </summary>
        /// <param name="pack">
        /// 適用対象のカーソルパック。
        /// pack.CursorMap は <see cref="CursorPack.SyncFromRawCursorMap"/> 済みであること。
        /// </param>
        /// <param name="baseDirectory">
        /// カーソルファイルのベースディレクトリ。
        /// pack.json が存在するフォルダパスを想定する。
        /// </param>
        /// <param name="createBackup">
        /// 適用前にバックアップを取得するかどうか。
        /// true の場合、失敗時にはロールバックを試みる。
        /// </param>
        /// <returns>
        /// 取得したバックアップスナップショット。バックアップを行っていない場合は null。
        /// 呼び出し側が任意に保存しておく用途を想定。
        /// </returns>
        /// <exception cref="CursorApplyException">
        /// 適用処理に失敗した場合。
        /// </exception>
        BackupSnapshot? ApplyPack(CursorPack pack, string baseDirectory, bool createBackup = true);
    }
}
