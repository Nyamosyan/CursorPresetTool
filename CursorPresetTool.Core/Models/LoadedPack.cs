using CursorPresetTool.Core.Models;

namespace CursorPresetTool.Core.Packs
{
    /// <summary>
    /// パック読み込みの結果を表す。
    /// CursorPack 本体に加えて、カーソルファイルのベースディレクトリも含む。
    /// </summary>
    public sealed class LoadedPack
    {
        /// <summary>
        /// 読み込まれたカーソルパック。
        /// </summary>
        public CursorPack Pack { get; }

        /// <summary>
        /// カーソルファイルのベースディレクトリ。
        /// ResolveCursorPaths で使う。
        /// </summary>
        public string BaseDirectory { get; }

        /// <summary>
        /// ZIP から読み込んだ場合の一時展開ディレクトリ（任意）。
        /// フォルダパックの場合は null。
        /// </summary>
        public string? TempDirectory { get; }

        public LoadedPack(CursorPack pack, string baseDirectory, string? tempDirectory = null)
        {
            Pack = pack;
            BaseDirectory = baseDirectory;
            TempDirectory = tempDirectory;
        }
    }
}
