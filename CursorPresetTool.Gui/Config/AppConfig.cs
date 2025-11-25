using System;
using System.Collections.Generic;

namespace CursorPresetTool.Gui.Config
{
    public sealed class AppConfig
    {
        public string Lang { get; set; } = "ja_jp";

        /// <summary>ピン留めしているプリセットのフォルダ名一覧。</summary>
        public List<string> PinnedPresets { get; set; } = new();

        public static AppConfig CreateDefault()
            => new AppConfig();
    }
}
