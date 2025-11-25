namespace CursorPresetTool.Core.Models
{
    /// <summary>
    /// カーソルパックのソース種別。
    /// </summary>
    public enum PackSourceType
    {
        /// <summary>
        /// Windows 現在の設定を表す組み込みプリセット。
        /// pack.json を持たない。
        /// </summary>
        Builtin,

        /// <summary>
        /// フォルダに含まれる pack.json をソースとするパック。
        /// </summary>
        Folder,

        /// <summary>
        /// ZIP ファイル内に含まれる pack.json をソースとするパック。
        /// </summary>
        Zip
    }
}
