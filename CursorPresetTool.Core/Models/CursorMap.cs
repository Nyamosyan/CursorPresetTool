using System;
using System.Collections.Generic;
using System.Linq;

namespace CursorPresetTool.Core.Models
{
    /// <summary>
    /// カーソルスロット（<see cref="CursorSlotId"/>）と
    /// それに対応するカーソルファイルの相対パスを保持するクラス。
    /// 
    /// pack.json の "CursorMap" 部分を抽象化したもの。
    /// 値は pack.json のあるディレクトリからの相対パスを前提とする。
    /// </summary>
    public sealed class CursorMap
    {
        private readonly Dictionary<CursorSlotId, string> _paths;

        /// <summary>
        /// 読み取り専用の内部ディクショナリ。
        /// 主にシリアライズ・デバッグ用。
        /// </summary>
        public IReadOnlyDictionary<CursorSlotId, string> Paths => _paths;

        /// <summary>
        /// 空のマップを作成する。
        /// </summary>
        public CursorMap()
        {
            _paths = new Dictionary<CursorSlotId, string>();
        }

        /// <summary>
        /// 既存のディクショナリからマップを生成する。
        /// </summary>
        public CursorMap(IDictionary<CursorSlotId, string> initial)
        {
            _paths = initial != null
                ? new Dictionary<CursorSlotId, string>(initial)
                : new Dictionary<CursorSlotId, string>();
        }

        /// <summary>
        /// 指定スロットに対応するカーソルパスを取得する。
        /// 見つからない場合は false を返す。
        /// </summary>
        public bool TryGetPath(CursorSlotId slot, out string? relativePath)
        {
            if (_paths.TryGetValue(slot, out var value))
            {
                relativePath = value;
                return true;
            }

            relativePath = null;
            return false;
        }

        /// <summary>
        /// 指定スロットのカーソルパスを設定する。
        /// null または空文字列を渡した場合は、そのスロットを削除する。
        /// </summary>
        public void SetPath(CursorSlotId slot, string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                _paths.Remove(slot);
            }
            else
            {
                _paths[slot] = relativePath;
            }
        }

        /// <summary>
        /// 登録されているスロット一覧を列挙する。
        /// </summary>
        public IEnumerable<CursorSlotId> GetDefinedSlots()
        {
            return _paths.Keys.ToArray();
        }

        /// <summary>
        /// 全 CursorSlotId のうち、未定義のスロットを列挙する。
        /// 「どのカーソルが pack.json で定義されていないか」を知りたいときに使う。
        /// </summary>
        public IEnumerable<CursorSlotId> GetMissingSlots()
        {
            var all = Enum.GetValues(typeof(CursorSlotId)).Cast<CursorSlotId>();
            return all.Where(slot => !_paths.ContainsKey(slot));
        }

        /// <summary>
        /// パスの妥当性を軽くチェックする。
        /// ファイルの存在までは確認しない（存在確認は別レイヤーで行う）。
        /// </summary>
        public IEnumerable<string> Validate()
        {
            var errors = new List<string>();

            foreach (var (slot, path) in _paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    errors.Add($"Cursor path for slot '{slot}' is empty.");
                    continue;
                }

                // pack.json は相対パス推奨なので、絶対パスが来たら警告。
                if (System.IO.Path.IsPathRooted(path))
                {
                    errors.Add($"Cursor path for slot '{slot}' must be relative, but got absolute: '{path}'.");
                }

                // 拡張子が .cur / .ani 以外の場合は警告。
                var ext = System.IO.Path.GetExtension(path);
                if (!string.Equals(ext, ".cur", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(ext, ".ani", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Cursor path for slot '{slot}' has unexpected extension: '{ext}'. Expected .cur or .ani.");
                }
            }

            return errors;
        }
    }
}
