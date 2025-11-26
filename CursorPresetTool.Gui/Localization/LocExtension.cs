using System;
using System.Windows;
using System.Windows.Markup;
using Application = System.Windows.Application;

namespace CursorPresetTool.Gui.Localization
{
    /// <summary>
    /// XAML からローカライズ文字列を取得するためのマークアップ拡張。
    /// 使用例: Text="{loc:Loc Key=MainWindow.PresetList}"
    /// </summary>
    [MarkupExtensionReturnType(typeof(string))]
    public sealed class LocExtension : MarkupExtension
    {
        public string Key { get; set; } = string.Empty;

        public LocExtension()
        {
        }

        public LocExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Application.Current is not App app)
            {
                // ありえないけど一応
                return Key;
            }

            var loc = app.Localization;
            if (loc == null)
            {
                return Key;
            }

            return loc[Key];
        }
    }
}
