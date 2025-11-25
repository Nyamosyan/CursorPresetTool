using System.Text.Json.Serialization;

namespace CursorPresetTool.Core.Models
{
    /// <summary>
    /// アプリ全体の設定を表すモデル。
    /// config.json にマップされる。
    /// </summary>
    public sealed class AppConfig
    {
        /// <summary>
        /// 設定ファイルのスキーマバージョン。
        /// 将来的な互換性維持用。
        /// </summary>
        [JsonPropertyName("SchemaVersion")]
        public int SchemaVersion { get; set; } = 1;

        /// <summary>
        /// UI の言語。
        /// "en-us" などを想定。MVP では en-us 固定でもよい。
        /// </summary>
        [JsonPropertyName("lang")]
        public string Language { get; set; } = "en-us";

        /// <summary>
        /// プリセットディレクトリ（任意）。
        /// 未設定の場合は exe からの相対パスなど、アプリ側がデフォルトを決める。
        /// </summary>
        [JsonPropertyName("presetDirectory")]
        public string? PresetDirectory { get; set; }

        /// <summary>
        /// 最後に使ったパックのパス（任意）。
        /// GUI の初期選択用に利用。
        /// </summary>
        [JsonPropertyName("lastUsedPackPath")]
        public string? LastUsedPackPath { get; set; }
    }
}
