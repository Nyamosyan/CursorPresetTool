using CursorPresetTool.Core.Cursors;
using CursorPresetTool.Core.Models;
using CursorPresetTool.Gui.Imaging;
using CursorPresetTool.Gui.Services;
using CursorPresetTool.Gui.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clipboard = System.Windows.Clipboard;
using ContextMenu = System.Windows.Controls.ContextMenu;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;

namespace CursorPresetTool.Gui
{
    public partial class PresetEditorWindow : Window
    {
        private readonly PresetEditorViewModel _viewModel;
        private readonly PresetService _presetService;
        private readonly string _presetRootDirectory;

        // スロット編集用のコマンド
        public static readonly RoutedUICommand ClearSlotCommand =
            new RoutedUICommand("ClearSlot", nameof(ClearSlotCommand), typeof(PresetEditorWindow));

        public static readonly RoutedUICommand UseCurrentSlotCommand =
            new RoutedUICommand("UseCurrentSlot", nameof(UseCurrentSlotCommand), typeof(PresetEditorWindow));

        public static readonly RoutedUICommand OpenSlotFileCommand =
            new RoutedUICommand("OpenSlotFile", nameof(OpenSlotFileCommand), typeof(PresetEditorWindow));

        public static readonly RoutedUICommand CopyRelativePathCommand =
            new RoutedUICommand("CopyRelativePath", nameof(CopyRelativePathCommand), typeof(PresetEditorWindow));


        /// <summary>
        /// 既存プリセット編集用コンストラクタ。
        /// </summary>
        public PresetEditorWindow(PresetService presetService,
                                  string presetRootDirectory,
                                  PresetEditorViewModel viewModel)
        {
            InitializeComponent();
            _presetService = presetService;
            _presetRootDirectory = presetRootDirectory;
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        /// <summary>
        /// pack.json 内の相対パス → 実ファイルのフルパス の対応表を作る。
        /// </summary>
        private IReadOnlyDictionary<string, string?> BuildRelativeToFullPathMap()
        {
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var slot in _viewModel.Slots)
            {
                // pack.json に書く相対パス（ファイル名等）
                var relative = slot.RelativePath;
                // 実際にコピー元になる絶対パス
                var full = slot.FullPath;

                if (string.IsNullOrWhiteSpace(relative))
                    continue;

                if (string.IsNullOrWhiteSpace(full))
                    continue;

                // 同じ相対パスを複数スロットで使っていても 1 回コピーすればOK
                if (!dict.ContainsKey(relative))
                {
                    dict[relative] = full;
                }
            }

            return dict;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 変更無しで閉じる場合
                if (!_viewModel.IsDirty)
                {
                    DialogResult = false;
                    Close();
                    return;
                }

                // pack.jsonに書く CursorPack を生成
                var pack = _viewModel.ToCursorPack();

                // relativePath → fullPath のコピー元マップを構築
                var fileMap = BuildRelativeToFullPathMap();

                if (_viewModel.IsNew)
                {
                    // 新規作成
                    var folderName = _viewModel.FolderName?.Trim();
                    if (string.IsNullOrEmpty(folderName))
                    {
                        MessageBox.Show(
                            this,
                            "フォルダ名を入力してください。",
                            "エラー",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    var targetFolder = Path.Combine(_presetRootDirectory, folderName);

                    // フォルダ存在 → 上書き確認
                    if (Directory.Exists(targetFolder))
                    {
                        var r = MessageBox.Show(
                            this,
                            "同名のプリセットフォルダが既に存在します。\n上書きしますか？",
                            "確認",
                            MessageBoxButton.OKCancel,
                            MessageBoxImage.Warning);

                        if (r != MessageBoxResult.OK)
                            return;
                    }

                    // 新規プリセット保存
                    var newPresetInfo =
                        _presetService.SaveNewPreset(
                            targetFolder,
                            pack,
                            fileMap);

                    DialogResult = true;
                    Close();
                }
                else
                {
                    // 既存プリセット編集
                    if (string.IsNullOrEmpty(_viewModel.OriginalFolderPath) ||
                        !Directory.Exists(_viewModel.OriginalFolderPath))
                    {
                        MessageBox.Show(
                            this,
                            "プリセットフォルダが見つかりません。",
                            "エラー",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    // 既存編集の保存（上書き）
                    var updatedPresetInfo =
                        _presetService.SaveExistingPreset(
                            _viewModel.OriginalFolderPath,
                            pack,
                            fileMap);

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "保存中にエラーが発生しました。\n\n" + ex.Message,
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 変更がある場合、本当は確認ダイアログを出す
            if (_viewModel.IsDirty)
            {
                var r = MessageBox.Show(this,
                    "変更が保存されていません。破棄して閉じますか？",
                    "確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (r != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            DialogResult = false;
            Close();
        }

        private void ExportZipButton_Click(object sender, RoutedEventArgs e)
        {
            // ここでは一旦ダミー。あとで MainWindow の Export と同じロジックに繋げる。
            MessageBox.Show(this,
                "ZIPエクスポートは後で PresetService 経由で実装します。",
                "情報",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void SlotListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (SlotListView.View is not GridView gv)
                return;

            if (gv.Columns.Count < 2)
                return;

            // ListView 全体の実際の幅
            double totalWidth = SlotListView.ActualWidth;

            // 左右の余白やボーダー分を少し引く（環境に合わせて調整してOK）
            const double marginOffset = 4.0;

            // 縦スクロールバーが出たときのぶんを引く
            double scrollBarWidth = SystemParameters.VerticalScrollBarWidth;

            double usable = totalWidth - scrollBarWidth - marginOffset;
            if (usable <= 0)
                return;

            // 1:1 の比率で列幅を割り振る
            gv.Columns[0].Width = usable / 2.0;
            gv.Columns[1].Width = usable / 2.0;
        }

        private void CursorPreview_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is not CursorSlotEditorItemViewModel slot) return;

            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "カーソルファイル (*.cur;*.ani)|*.cur;*.ani"
            };

            if (dlg.ShowDialog() == true)
            {
                slot.FullPath = dlg.FileName;
                slot.RelativePath = System.IO.Path.GetFileName(dlg.FileName);

                slot.PreviewImage = CursorPreviewFactory.CreatePreview(dlg.FileName);
                slot.IsMissing = slot.PreviewImage is null;
            }
        }

        private void CursorPreview_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && IsCursorFile(files[0]))
                {
                    e.Effects = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effects = DragDropEffects.None;
        }

        private void CursorPreview_Drop(object sender, DragEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is not CursorSlotEditorItemViewModel slot) return;

            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0) return;

            var path = files[0];
            if (!IsCursorFile(path)) return;

            slot.FullPath = path;
            slot.RelativePath = Path.GetFileName(path);

            slot.PreviewImage = CursorPreviewFactory.CreatePreview(path);
            slot.IsMissing = slot.PreviewImage is null;
        }

        private static bool IsCursorFile(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".cur" || ext == ".ani";
        }

        private void SlotCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is CursorSlotEditorItemViewModel;
        }

        private static CursorSlotEditorItemViewModel? GetSlotFromParameter(object? parameter)
            => parameter as CursorSlotEditorItemViewModel;

        private void ClearSlotCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var slot = GetSlotFromParameter(e.Parameter);
            if (slot is null) return;

            slot.FullPath = null;
            slot.RelativePath = null;
            slot.PreviewImage = null;
            slot.IsMissing = true;
        }

        private void UseCurrentSlotCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var slot = GetSlotFromParameter(e.Parameter);
            if (slot is null) return;

            var provider = new SystemCursorProvider();
            var snapshot = provider.CaptureSnapshot();

            var info = snapshot.GetByKey(slot.Key);
            var fullPath = info?.FullPath;

            if (string.IsNullOrWhiteSpace(fullPath))
            {
                MessageBox.Show(this,
                    "このスロットの現在のカーソルパスを取得できませんでした。",
                    "情報",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                slot.FullPath = null;
                slot.RelativePath = null;
                slot.PreviewImage = null;
                slot.IsMissing = true;
                return;
            }

            slot.FullPath = fullPath;
            slot.RelativePath = System.IO.Path.GetFileName(fullPath);

            var preview = CursorPreviewFactory.CreatePreview(fullPath);
            slot.PreviewImage = preview;
            slot.IsMissing = preview is null;
        }

        private void OpenSlotFileCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var slot = GetSlotFromParameter(e.Parameter);
            if (slot is null) return;

            string? path = slot.FullPath;

            // FullPath がない場合、既存プリセットならフォルダから推測
            if (string.IsNullOrWhiteSpace(path) &&
                !string.IsNullOrWhiteSpace(_viewModel.OriginalFolderPath) &&
                !string.IsNullOrWhiteSpace(slot.RelativePath))
            {
                path = System.IO.Path.Combine(_viewModel.OriginalFolderPath, slot.RelativePath);
            }

            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                MessageBox.Show(this,
                    "カーソルファイルが見つかりません。",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            try
            {
                // フォルダを開く
                var folder = System.IO.Path.GetDirectoryName(path);
                if (folder == null)
                {
                    MessageBox.Show(this,
                        "フォルダーを取得できませんでした。",
                        "エラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"フォルダーを開けませんでした。\n\n{ex.Message}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CopyRelativePathCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var slot = GetSlotFromParameter(e.Parameter);
            if (slot is null) return;

            if (string.IsNullOrWhiteSpace(slot.RelativePath))
            {
                MessageBox.Show(this,
                    "このスロットには相対パスが設定されていません。",
                    "情報",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            Clipboard.SetText(slot.RelativePath);
        }

    }
}
