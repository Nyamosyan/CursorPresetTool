namespace CursorPresetTool.Core.Packs
{
    /// <summary>
    /// プリセットフォルダを ZIP としてエクスポートするインターフェース。
    /// </summary>
    public interface IPackExporter
    {
        /// <summary>
        /// 指定されたプリセットフォルダを ZIP ファイルとしてエクスポートする。
        /// </summary>
        /// <param name="presetFolderPath">pack.json を含むプリセットフォルダ。</param>
        /// <param name="destinationZipPath">出力 ZIP ファイルのパス。</param>
        /// <param name="overwrite">既存のファイルを上書きするかどうか。</param>
        void ExportToZip(string presetFolderPath, string destinationZipPath, bool overwrite = false);
    }
}
