# CursorPresetTool
Windows のマウスカーソル設定（.cur / .ani）をプリセットとして保存・切り替えできるツールです。

## 機能
- カーソル設定の保存（export）
- プリセット一覧表示（list）
- 名前または番号で適用（apply）
- Windows 既定カーソルへ戻す（reset）
- ファイル存在チェックつき（欠損時は y/n 確認）
- presets フォルダでプリセット管理

## ダウンロード
[Releases](https://github.com/Nyamosyan/CursorPresetTool/releases)から `CursorPresetTool.zip` をダウンロードしてください。  
[こちら](https://github.com/Nyamosyan/CursorPresetTool/releases/latest/CursorPresetTool.zip)から最新版を直接ダウンロードできます。

## フォルダ構成
解凍すると次の構成になっています。  
```
CursorPresetTool/  
 ├ CursorPresetTool.exe  
 ├ CursorPresetTool.bat        ← プリセット選択・適用  
 ├ AddPreset.bat               ← 現在の設定を新規プリセットとして保存  
 └ presets/                    ← プリセット（JSON）が保存される場所
```

## 使い方
ダウンロードしたZIPファイルを解凍し、好きなディレクトリに配置してください。  
1. プリセットの適用（切り替え）： `CursorPresetTool.bat` を実行  
  プリセット一覧が表示されるので、番号または名前を入力します。  
    - `0` → Windows 既定カーソルにリセット  
    - `1` → 1番目のプリセット適用  
    - `<名前>` → 名前指定で適用  

2. 新しいプリセットを追加（現在の設定を保存）： `AddPreset.bat` を実行  
  現在のカーソル設定が `presets/<名前>.json` として保存されます。

3. 直接コマンドで操作（CLI）
   `CursorPresetTool.exe` （引数なし）でヘルプを確認できます。  
    ```
      CursorPresetTool.exe list
      CursorPresetTool.exe export <名前>
      CursorPresetTool.exe apply 1
      CursorPresetTool.exe apply <名前>
      CursorPresetTool.exe apply 0     ← reset
    ```

## プリセットの保存場所
すべてのプリセットは `presets/<名前>.json` に保存されます。  
JSONを編集することで、カーソルパスを手動調整することもできます。

## 対応するカーソル項目（17種類）
レジストリ名（プロパティ名）で記載しています。  
```
AppStarting（バックグランドで作業中）
Arrow      （通常の選択）
Crosshair  （領域選択）
Hand       （リンクの選択）
Help       （ヘルプの選択）
IBeam      （テキスト選択）
No         （利用不可）
NWPen      （手書き）
Person     （人の選択）
Pin        （場所の選択）
SizeAll    （移動）
SizeNESW   （斜めに拡大/縮小 2）
SizeNS     （上下に拡大/縮小）
SizeNWSE   （斜めに拡大/縮小 1）
SizeWE     （左右に拡大/縮小）
UpArrow    （代替選択）
Wait       （待ち状態）
```

## 対応環境
Windows 10 / Windows 11
