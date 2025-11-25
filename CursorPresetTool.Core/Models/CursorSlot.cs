namespace CursorPresetTool.Core.Models
{
    /// <summary>
    /// カーソルスロットの定義。
    /// enum の Id + レジストリキー名 + 表示名をまとめて持つ。
    /// </summary>
    public sealed class CursorSlot
    {
        public CursorSlotId Id { get; }
        public string Key { get; }          // レジストリ値名 (Arrow, Hand, ...)
        public string DisplayName { get; }  // UI 表示名

        private CursorSlot(CursorSlotId id, string key, string displayName)
        {
            Id = id;
            Key = key;
            DisplayName = displayName;
        }

        /// <summary>全カーソルスロット一覧。</summary>
        public static readonly CursorSlot[] All =
        {
            new(CursorSlotId.Arrow,       "Arrow",       "通常の選択"),
            new(CursorSlotId.Help,        "Help",        "ヘルプの選択"),
            new(CursorSlotId.AppStarting, "AppStarting", "バックグラウンドで作業中"),
            new(CursorSlotId.Wait,        "Wait",        "待ち状態"),
            new(CursorSlotId.Crosshair,   "Crosshair",   "領域選択"),
            new(CursorSlotId.IBeam,       "IBeam",       "テキスト選択"),
            new(CursorSlotId.NWPen,       "NWPen",       "手書き"),
            new(CursorSlotId.No,          "No",          "利用不可"),
            new(CursorSlotId.SizeNS,      "SizeNS",      "上下に拡大/縮小"),
            new(CursorSlotId.SizeWE,      "SizeWE",      "左右に拡大/縮小"),
            new(CursorSlotId.SizeNWSE,    "SizeNWSE",    "斜めに拡大/縮小 1"),
            new(CursorSlotId.SizeNESW,    "SizeNESW",    "斜めに拡大/縮小 2"),
            new(CursorSlotId.SizeAll,     "SizeAll",     "移動"),
            new(CursorSlotId.UpArrow,     "UpArrow",     "代替選択"),
            new(CursorSlotId.Hand,        "Hand",        "リンクの選択"),
            new(CursorSlotId.Pin,         "Pin",         "場所の選択"),
            new(CursorSlotId.Person,      "Person",      "人の選択"),
        };

        // よく使う検索を helper で用意
        public static CursorSlot? FromId(CursorSlotId id)
            => All.FirstOrDefault(s => s.Id == id);

        public static CursorSlot? FromKey(string key)
            => All.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));
    }
}
