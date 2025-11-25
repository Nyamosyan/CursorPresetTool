using CursorPresetTool.Core.Models;

namespace CursorPresetTool.Core.Packs
{
    /// <summary>
    /// カーソルパック（pack.json）を読み込むためのインターフェース。
    /// 
    /// フォルダベースのパック（<dir>/pack.json）や
    /// ZIP ベースのパック（pack.zip 内の pack.json）を
    /// 共通の <see cref="CursorPack"/> として扱えるようにする。
    /// </summary>
    public interface IPackReader
    {
        /// <summary>
        /// 指定されたフォルダパスからカーソルパックを読み込む。
        /// フォルダ内に pack.json が存在することを前提とする。
        /// </summary>
        /// <param name="folderPath">pack.json を含むフォルダのフルパス。</param>
        /// <returns>読み込んだ <see cref="CursorPack"/>。</returns>
        /// <exception cref="PackReadException">pack.json が見つからない、または内容が不正な場合。</exception>
        CursorPack LoadFromFolder(string folderPath);

        /// <summary>
        /// 指定された ZIP ファイルからカーソルパックを読み込む。
        /// ZIP 内に pack.json が含まれていることを前提とする。
        /// </summary>
        /// <param name="zipFilePath">パック ZIP ファイルのフルパス。</param>
        /// <returns>読み込んだ <see cref="CursorPack"/>。</returns>
        /// <exception cref="PackReadException">ZIP が開けない、pack.json がない、または内容が不正な場合。</exception>
        CursorPack LoadFromZip(string zipFilePath);
    }
}
