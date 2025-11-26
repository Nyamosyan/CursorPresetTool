using System.Windows;
using Application = System.Windows.Application;

namespace CursorPresetTool.Gui.Localization
{
    /// <summary>
    /// コード側から簡単にローカライズ文字列を取得するヘルパー。
    /// </summary>
    public static class Loc
    {
        public static string Get(string key)
        {
            if (Application.Current is not App app || app.Localization is null)
            {
                return key; // どうにもならないときはキーをそのまま返す
            }

            return app.Localization[key];
        }
    }
}
