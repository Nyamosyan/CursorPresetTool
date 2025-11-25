using System.IO;
using CursorPresetTool.Core.Models;

namespace CursorPresetTool.Core.Packs
{
    /// <summary>
    /// インポート済みプリセット（フォルダパック）の情報。
    /// </summary>
    public sealed class PresetInfo
    {
        public string Name { get; }

        /// <summary>
        /// プリセットフォルダの絶対パス。
        /// presets/Name のような形を想定。
        /// </summary>
        public string FolderPath { get; }

        /// <summary>
        /// 読み込んだカーソルパックの内容。
        /// </summary>
        public CursorPack Pack { get; }

        public PresetInfo(string name, string folderPath, CursorPack pack)
        {
            Name = name;
            FolderPath = folderPath;
            Pack = pack;
        }

        /// <summary>
        /// カーソルキー名（"Arrow" など）から、このプリセットで指定されている
        /// カーソルファイルのフルパスを解決する。
        /// 対応するエントリが無い / 空文字などの場合は null。
        /// </summary>
        public string? ResolveCursorPath(string key)
        {
            // key は "Arrow" などのレジストリ名
            // → CursorSlot から CursorSlotId を取得
            var slot = CursorSlot.FromKey(key);
            return slot is null ? null : ResolveCursorPath(slot);
        }

        /// <summary>
        /// CursorSlot からカーソルファイルパスを解決するヘルパ。
        /// </summary>
        public string? ResolveCursorPath(CursorSlot slot)
        {
            // CursorMap.Paths は CursorSlotId → "arrow.cur"
            if (!Pack.CursorMap.Paths.TryGetValue(slot.Id, out var relative))
                return null;

            if (string.IsNullOrWhiteSpace(relative))
                return null;

            // プリセットフォルダに結合してフルパスを得る
            var full = Path.Combine(FolderPath, relative);
            return File.Exists(full) ? full : full; // 存在チェックは GUI 側
        }
    }
}
