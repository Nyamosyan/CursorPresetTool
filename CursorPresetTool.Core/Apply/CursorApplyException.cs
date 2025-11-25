using System;

namespace CursorPresetTool.Core.Apply
{
    /// <summary>
    /// カーソル適用処理中に発生したエラーを表す例外。
    /// </summary>
    public class CursorApplyException : Exception
    {
        public CursorApplyException(string message)
            : base(message)
        {
        }

        public CursorApplyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
