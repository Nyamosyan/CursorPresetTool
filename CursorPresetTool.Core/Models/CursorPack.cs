using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CursorPresetTool.Core.Models
{
    /// <summary>
    /// 1 つのカーソルパック（pack.json）を表すモデル。
    /// JSON とのマッピングを意識したプロパティ構成になっている。
    /// 
    /// 実際のファイルの存在確認や ZIP 展開などは別レイヤーが担当し、
    /// このクラスは「構造と最低限の整合性」を管理する。
    /// </summary>
    public sealed class CursorPack
    {
        /// <summary>
        /// pack.json の "PackName"。
        /// 配布者がつけるパック名。
        /// </summary>
        [JsonPropertyName("PackName")]
        public string PackName { get; set; } = string.Empty;

        /// <summary>
        /// pack.json の "Author"。
        /// 作者名。空でもよいが、できれば入れてもらう。
        /// </summary>
        [JsonPropertyName("Author")]
        public string? Author { get; set; }

        /// <summary>
        /// pack.json の "Version"。
        /// "1.0.0" 形式などを想定しているが、特に形式は強制しない。
        /// </summary>
        [JsonPropertyName("Version")]
        public string? Version { get; set; }

        /// <summary>
        /// pack.json の "Description"。
        /// 任意説明。
        /// </summary>
        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        /// <summary>
        /// pack.json の "CursorMap"。
        /// JSON では { "Arrow": "arrow.cur", ... } という形になる。
        /// 
        /// シリアライズのために内部では Dictionary&lt;string, string&gt; を直接持ち、
        /// アプリ内部では <see cref="CursorMap"/> として扱う想定。
        /// </summary>
        [JsonPropertyName("CursorMap")]
        public Dictionary<string, string> RawCursorMap { get; set; } = new();

        /// <summary>
        /// アプリ内部で使いやすい形に変換したカーソルマップ。
        /// RawCursorMap との同期は外部で行う。
        /// </summary>
        [JsonIgnore]
        public CursorMap CursorMap { get; set; } = new();

        /// <summary>
        /// このパックがどこから読み込まれたかを表す種別。
        /// Builtin / Folder / Zip のいずれか。
        /// 
        /// pack.json 自体には含まれない内部情報。
        /// </summary>
        [JsonIgnore]
        public PackSourceType SourceType { get; set; } = PackSourceType.Folder;

        /// <summary>
        /// pack.json が存在するパス、あるいは ZIP ファイルのパス。
        /// Builtin の場合は null。
        /// </summary>
        [JsonIgnore]
        public string? SourcePath { get; set; }

        /// <summary>
        /// pack.json の基本的な妥当性をチェックする。
        /// ファイル存在確認などは行わない。
        /// </summary>
        /// <returns>エラーメッセージ一覧。空なら OK。</returns>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(PackName))
            {
                errors.Add("PackName is required.");
            }

            // RawCursorMap が無い・空でも pack としては有効だが、
            // 完全に空の場合は警告として扱ってもよい。
            if (RawCursorMap is null)
            {
                errors.Add("CursorMap is missing.");
            }

            // CursorMap 側の検証（拡張子、相対パスなど）
            if (CursorMap is not null)
            {
                errors.AddRange(CursorMap.Validate());
            }

            return errors;
        }

        /// <summary>
        /// JSON の RawCursorMap から CursorMap への変換を行う。
        /// JSON から読み込んだ直後などに呼び出すことを想定。
        /// </summary>
        public void SyncFromRawCursorMap()
        {
            var map = new Dictionary<CursorSlotId, string>();

            if (RawCursorMap != null)
            {
                foreach (var kv in RawCursorMap)
                {
                    if (Enum.TryParse<CursorSlotId>(kv.Key, ignoreCase: true, out var slot))
                    {
                        map[slot] = kv.Value;
                    }
                    else
                    {
                        // 未知のキー名は無視する（将来拡張の余地として緩く扱う）
                        // ここでログを出したければ ILogger などを組み合わせる。
                    }
                }
            }

            CursorMap = new CursorMap(map);
        }

        /// <summary>
        /// CursorMap の内容を RawCursorMap に反映する。
        /// pack.json へ保存する前に呼び出すことを想定。
        /// </summary>
        public void SyncToRawCursorMap()
        {
            var dict = new Dictionary<string, string>();

            foreach (var pair in CursorMap.Paths)
            {
                dict[pair.Key.ToString()] = pair.Value;
            }

            RawCursorMap = dict;
        }
    }
}
