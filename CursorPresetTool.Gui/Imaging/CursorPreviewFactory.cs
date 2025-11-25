using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CursorPresetTool.Gui.Imaging
{
    /// <summary>
    /// .cur / .ani ファイルから WPF 用の ImageSource を生成するヘルパ。
    /// プレビュー用に 32x32 のビットマップを返す。
    /// </summary>
    internal static class CursorPreviewFactory
    {
        private const int IMAGE_CURSOR = 2;

        // LR_LOADFROMFILE: ファイルから読み込む
        // LR_DEFAULTSIZE: システム既定サイズ（基本 32x32）を使用
        private const int LR_DEFAULTCOLOR = 0x0000;
        private const int LR_LOADFROMFILE = 0x0010;
        private const int LR_DEFAULTSIZE = 0x0040;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadImageW(
            IntPtr hInst,
            string lpszName,
            uint uType,
            int cxDesired,
            int cyDesired,
            uint fuLoad);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// 指定パスのカーソルファイル（.cur / .ani）から 32x32 の ImageSource を作成する。
        /// 読み込みに失敗した場合は null を返す。
        /// </summary>
        public static ImageSource? CreatePreview(string? path, int size = 32)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return null;

                if (!File.Exists(path))
                    return null;

                // .cur / .ani 以外はとりあえず対象外とする（将来拡張余地あり）
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext != ".cur" && ext != ".ani")
                    return null;

                // Win32 API でカーソルを読み込む
                uint flags = LR_LOADFROMFILE | LR_DEFAULTSIZE | LR_DEFAULTCOLOR;
                IntPtr hCursor = LoadImageW(IntPtr.Zero, path, IMAGE_CURSOR, size, size, flags);

                if (hCursor == IntPtr.Zero)
                    return null;

                try
                {
                    // HICON/HCURSOR から WPF BitmapSource を生成
                    var src = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        hCursor,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(size, size));

                    // フリーズしてスレッドセーフに
                    src.Freeze();
                    return src;
                }
                finally
                {
                    DestroyIcon(hCursor);
                }
            }
            catch
            {
                // 何かあっても UI 全体は落とさない
                return null;
            }
        }
    }
}
