using CursorPresetTool.Core.Cursors;
using CursorPresetTool.Gui.Config;
using CursorPresetTool.Gui.Localization;
using CursorPresetTool.Gui.Models;
using CursorPresetTool.Gui.Services;
using CursorPresetTool.Gui.ViewModels;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using ContextMenu = System.Windows.Controls.ContextMenu;
using Forms = System.Windows.Forms;
using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace CursorPresetTool.Gui
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;

        private readonly string _presetRootDirectory;
        private readonly PresetService _presetService;
        private readonly SystemCursorProvider _systemCursorProvider;
        private readonly LocalizationService _localization;

        public MainWindow(ConfigService configService, LocalizationService localization)
        {
            InitializeComponent();

            _presetRootDirectory = Path.Combine(AppContext.BaseDirectory, "presets");
            _presetService = new PresetService(_presetRootDirectory);
            _systemCursorProvider = new SystemCursorProvider();
            _localization = localization;
            var vm = new MainViewModel(_presetService, _systemCursorProvider, configService, _localization);

            DataContext = vm;

            // 起動時にプリセット一覧を読み込む
            vm.LoadPresets();
        }

        private void AddPresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu is ContextMenu cm)
            {
                cm.PlacementTarget = btn;
                cm.IsOpen = true;
            }
        }

        private void NewPresetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            bool hasSelectedPreset = ViewModel.SelectedPreset != null;

            var dialog = new NewPresetDialog(hasSelectedPreset)
            {
                Owner = this
            };

            var dialogResult = dialog.ShowDialog();
            if (dialogResult != true)
            {
                return; // キャンセル
            }

            PresetEditorViewModel editorVm;

            switch (dialog.SelectedSourceKind)
            {
                case NewPresetSourceKind.FromSystem:
                    {
                        var snapshot = _systemCursorProvider.CaptureSnapshot();
                        editorVm = PresetEditorViewModel.CreateNewFromSystem(snapshot);
                        break;
                    }

                case NewPresetSourceKind.FromSelectedPreset:
                    {
                        if (ViewModel.SelectedPreset is null)
                        {
                            // 万が一無選択なら空から
                            editorVm = PresetEditorViewModel.CreateNewEmpty();
                        }
                        else
                        {
                            editorVm = PresetEditorViewModel.CreateNewFromPreset(
                                ViewModel.SelectedPreset.PresetInfo);
                        }
                        break;
                    }

                case NewPresetSourceKind.Empty:
                default:
                    {
                        editorVm = PresetEditorViewModel.CreateNewEmpty();
                        break;
                    }
            }

            var editorWindow = new PresetEditorWindow(_presetService, _presetRootDirectory, editorVm, _localization)
            {
                Owner = this
            };

            var result = editorWindow.ShowDialog();
            if (result == true)
            {
                // 保存されたのでプリセット一覧を再読み込み
                ViewModel.ReloadPresets(null);

                // ここで、もし編集側から「保存したフォルダ名」を返す仕組みを
                // 追加すれば、そのプリセットを自動選択することも可能。
                // まずは一覧更新だけでOKでもいい。
            }
        }

        private void ImportFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new Forms.FolderBrowserDialog
            {
                Description = "pack.json を含むフォルダを選択してください。"
            };

            var result = dialog.ShowDialog();
            if (result != Forms.DialogResult.OK)
                return;

            try
            {
                ViewModel.ImportFromFolder(dialog.SelectedPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"フォルダからのインポートに失敗しました。\n\n{ex.Message}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ImportZipButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "ZIP パックを選択してください",
                Filter = "ZIP パック (*.zip)|*.zip|すべてのファイル (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            var result = dialog.ShowDialog(this);
            if (result != true)
                return;

            try
            {
                ViewModel.ImportFromZip(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"ZIP からのインポートに失敗しました。\n\n{ex.Message}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ViewModel.LoadPresets();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"プリセット一覧の再読み込みに失敗しました。\n\n{ex.Message}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedPreset is null)
            {
                MessageBox.Show(this,
                    "適用するプリセットが選択されていません。",
                    "情報",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                ViewModel.ApplySelectedPreset();
                MessageBox.Show(this,
                    "プリセットを適用しました。",
                    "完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"プリセットの適用に失敗しました。\n\n{ex.Message}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedPreset is null)
            {
                MessageBox.Show(this,
                    "エクスポートするプリセットが選択されていません。",
                    "情報",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "プリセットを ZIP としてエクスポート",
                Filter = "ZIP ファイル (*.zip)|*.zip|すべてのファイル (*.*)|*.*",
                FileName = ViewModel.SelectedPreset.Name + ".zip"
            };

            var result = dialog.ShowDialog(this);
            if (result != true)
                return;

            try
            {
                ViewModel.ExportSelectedPreset(dialog.FileName, overwrite: true);
                MessageBox.Show(this,
                    "プリセットを ZIP としてエクスポートしました。",
                    "完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"プリセットのエクスポートに失敗しました。\n\n{ex.Message}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedPreset is null)
            {
                MessageBox.Show(this,
                    "編集するプリセットを選択してください。",
                    "情報",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var presetInfo = ViewModel.SelectedPreset.PresetInfo;
            var editorVm = PresetEditorViewModel.FromExistingPreset(presetInfo);

            var editorWindow = new PresetEditorWindow(_presetService, _presetRootDirectory, editorVm, _localization)
            {
                Owner = this
            };

            var result = editorWindow.ShowDialog();
            if (result == true)
            {
                // 保存されたので一覧＆プレビューを再読み込み
                ViewModel.ReloadPresets(null);  // 既にあるメソッドに合わせて
                                                // できれば元のプリセットを再選択する or フォルダ名から再選択
                                                // ViewModel.SelectPresetByFolder(presetInfo.FolderPath); みたいなメソッドを作ってもよい
            }
        }

        private void OpenFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedPreset is null)
                return;

            var folder = ViewModel.SelectedPreset.FolderPath;
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                MessageBox.Show(this,
                    "フォルダが見つかりません。",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"エクスプローラーでフォルダを開けませんでした。\n\n{ex.Message}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void RenamePresetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedPreset is null)
                return;

            var currentName = ViewModel.SelectedPreset.Name;

            // シンプルに InputBox で新しい名前を聞く
            var input = Interaction.InputBox(
                "新しいプリセット名を入力してください。",
                "プリセット名の変更",
                currentName);

            if (string.IsNullOrWhiteSpace(input) || input == currentName)
            {
                // キャンセル or 変更なし
                return;
            }

            try
            {
                ViewModel.RenameSelectedPreset(input.Trim());
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"プリセット名の変更に失敗しました。\n\n{ex.Message}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DuplicatePresetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedPreset is null)
                return;

            try
            {
                ViewModel.DuplicateSelectedPreset();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"プリセットの複製に失敗しました。\n\n{ex.Message}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void DeletePresetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedPreset is null)
                return;

            var name = ViewModel.SelectedPreset.Name;

            var result = MessageBox.Show(this,
                $"プリセット「{name}」を削除しますか？\n" +
                "フォルダとファイルは元に戻せません。",
                "プリセットの削除確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                ViewModel.DeleteSelectedPreset();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"プリセットの削除に失敗しました。\n\n{ex.Message}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void PresetListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 右クリック位置にある ListBoxItem をヒットテストで探す
            var listBox = (ListBox)sender;
            var point = e.GetPosition(listBox);

            var result = VisualTreeHelper.HitTest(listBox, point);
            if (result == null)
                return;

            var item = FindAncestor<ListBoxItem>(result.VisualHit);
            if (item != null && item.DataContext is PresetItemViewModel vm)
            {
                listBox.SelectedItem = vm;
            }
        }

        // VisualTree をさかのぼって指定タイプの親を探すユーティリティ
        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T target)
                    return target;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        // カーソル項目一覧の列幅の調整
        private void CursorListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (CursorListView.View is not GridView gridView)
                return;

            if (gridView.Columns.Count < 3)
                return;

            // 実効幅
            double totalWidth = CursorListView.ActualWidth;

            // 縦スクロールバーぶんを引く
            totalWidth -= SystemParameters.VerticalScrollBarWidth;

            // ListViewのPaddingやBorder、ヘッダー余白ぶんのマージンを少し多めに引いておく
            const double safetyMargin = 4.0;
            totalWidth -= safetyMargin;

            if (totalWidth <= 0)
                return;

            // 比率: 項目 : 適用中 : プリセット = 2 : 1 : 1 （合計 4）
            double unit = totalWidth / 4.0;

            gridView.Columns[0].Width = unit * 2; // 項目
            gridView.Columns[1].Width = unit * 1; // 適用中
            gridView.Columns[2].Width = unit * 1; // プリセット
        }

        private void PinPresetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedPreset is null)
                return;

            ViewModel.SelectedPreset.IsPinned = true;
            ViewModel.ResortPresets();
        }

        private void UnpinPresetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedPreset is null)
                return;

            ViewModel.SelectedPreset.IsPinned = false;
            ViewModel.ResortPresets();
        }
    }
}
