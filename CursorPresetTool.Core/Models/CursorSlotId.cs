using System;

namespace CursorPresetTool.Core.Models
{
    /// <summary>
    /// Windows のカーソル種別を表す列挙。
    /// pack.json の CursorMap と 1 対 1 で対応させる。
    /// 
    /// JSON では string で扱う想定なので、
    /// シリアライズ時は文字列変換を別レイヤーで行う。
    /// </summary>
    public enum CursorSlotId
    {
        /// <summary>通常の選択</summary>
        Arrow,

        /// <summary>ヘルプの選択</summary>
        Help,

        /// <summary>バックグラウンドで作業中</summary>
        AppStarting,

        /// <summary>待ち状態</summary>
        Wait,

        /// <summary>領域選択</summary>
        Crosshair,

        /// <summary>テキスト選択</summary>
        IBeam,

        /// <summary>手書き</summary>
        NWPen,

        /// <summary>利用不可</summary>
        No,

        /// <summary>上下に拡大/縮小</summary>
        SizeNS,

        /// <summary>左右に拡大/縮小</summary>
        SizeWE,

        /// <summary>斜めに拡大/縮小 1</summary>
        SizeNWSE,

        /// <summary>斜めに拡大/縮小 2</summary>
        SizeNESW,

        /// <summary>移動</summary>
        SizeAll,

        /// <summary>代替選択</summary>
        UpArrow,

        /// <summary>リンクの選択</summary>
        Hand,

        /// <summary>場所の選択</summary>
        Pin,

        /// <summary>人の選択</summary>
        Person,
    }
}
