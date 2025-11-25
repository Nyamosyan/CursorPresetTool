using System;

namespace CursorPresetTool.Core.Packs
{
    /// <summary>
    /// パック読み込み時に発生したエラーを表す例外。
    /// 
    /// どのパスで、どのような理由で失敗したかを
    /// 呼び出し側に分かりやすく伝えるためのもの。
    /// </summary>
    public class PackReadException : Exception
    {
        /// <summary>
        /// 問題が発生したパス（フォルダまたは ZIP ファイル）。
        /// 不明な場合は null。
        /// </summary>
        public string? TargetPath { get; }

        public PackReadException(string message, string? targetPath = null)
            : base(message)
        {
            TargetPath = targetPath;
        }

        public PackReadException(string message, Exception innerException, string? targetPath = null)
            : base(message, innerException)
        {
            TargetPath = targetPath;
        }
    }
}
