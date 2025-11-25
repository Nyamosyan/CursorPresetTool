namespace CursorPresetTool.Core.Packs
{
    /// <summary>
    /// ZIP またはフォルダからプリセットフォルダにインポートするためのインターフェース。
    /// </summary>
    public interface IPackImporter
    {
        /// <summary>
        /// フォルダパックをプリセットディレクトリにインポートする。
        /// </summary>
        /// <param name="sourceFolder">pack.json を含むフォルダのパス。</param>
        /// <returns>インポートされたプリセット情報。</returns>
        PresetInfo ImportFromFolder(string sourceFolder);

        /// <summary>
        /// ZIP パックをプリセットディレクトリにインポートする。
        /// ZIP 内部の pack.json を探し、そのフォルダ構造を元にインポートする。
        /// </summary>
        /// <param name="zipFilePath">ZIP ファイルのパス。</param>
        /// <returns>インポートされたプリセット情報。</returns>
        PresetInfo ImportFromZip(string zipFilePath);
    }
}
