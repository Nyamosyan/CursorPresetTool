using System;
using System.IO;
using System.Text.Json;

namespace CursorPresetTool.Gui.Config
{
    public sealed class ConfigService
    {
        private readonly string _configPath;
        private AppConfig _current;

        public AppConfig Current => _current;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public ConfigService()
        {
            var baseDir = AppContext.BaseDirectory;
            _configPath = Path.Combine(baseDir, "config.json");
            _current = LoadInternal();
        }

        private AppConfig LoadInternal()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    return AppConfig.CreateDefault();
                }

                var json = File.ReadAllText(_configPath);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
                return cfg ?? AppConfig.CreateDefault();
            }
            catch
            {
                // 何かあってもアプリが落ちないようにデフォルトを返す
                return AppConfig.CreateDefault();
            }
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_current, JsonOptions);
                File.WriteAllText(_configPath, json);
            }
            catch
            {
                // ログを仕込むならここ。今は黙って失敗させておく。
            }
        }

        /// <summary>
        /// ピン留めプリセット一覧を書き換えて即保存する。
        /// </summary>
        public void UpdatePinnedPresets(IEnumerable<string> folderNames)
        {
            _current.PinnedPresets = new List<string>(folderNames);
            Save();
        }
    }
}
