using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CursorPresetTool.Gui.Localization
{
    public sealed class LocalizationService
    {
        private readonly Dictionary<string, string> _strings =
            new(StringComparer.OrdinalIgnoreCase);

        public string this[string key]
            => _strings.TryGetValue(key, out var v) ? v : key;

        public string LangCode { get; }

        public LocalizationService(string langCode)
        {
            LangCode = string.IsNullOrWhiteSpace(langCode) ? "ja_jp" : langCode;
            Load(langCode);
        }

        private void Load(string langCode)
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var path = Path.Combine(baseDir, "lang", langCode + ".json");
                if (!File.Exists(path))
                    return;

                var json = File.ReadAllText(path);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict is null) return;

                _strings.Clear();
                foreach (var kv in dict)
                {
                    _strings[kv.Key] = kv.Value ?? string.Empty;
                }
            }
            catch
            {
                // エラー時は空のまま（キーをそのまま表示）
            }
        }
    }
}
