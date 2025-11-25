using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using CursorPresetTool.Core.Cursors;
using CursorPresetTool.Core.Models;
using CursorPresetTool.Core.Packs;
using CursorPresetTool.Gui.Imaging;
using CursorPresetTool.Gui.Services;

namespace CursorPresetTool.Gui.ViewModels
{
    /// <summary>
    /// メイン画面用 ViewModel。
    /// プリセット一覧・選択・インポート・適用などの操作をまとめる。
    /// </summary>
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private readonly PresetService _presetService;
        private readonly SystemCursorProvider _systemCursorProvider;

        public ObservableCollection<PresetItemViewModel> Presets { get; } = new();
        public ObservableCollection<CursorComparisonItemViewModel> CursorItems { get; } = new();

        private PresetItemViewModel? _selectedPreset;
        public PresetItemViewModel? SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                if (_selectedPreset != value)
                {
                    _selectedPreset = value;
                    OnPropertyChanged();
                    RebuildCursorItems();
                }
            }
        }

        // MainWindow から呼びやすいように、PresetService だけ受け取るコンストラクタも用意
        public MainViewModel(PresetService presetService)
            : this(presetService, new SystemCursorProvider())
        {
        }

        public MainViewModel(PresetService presetService, SystemCursorProvider systemCursorProvider)
        {
            _presetService = presetService;
            _systemCursorProvider = systemCursorProvider;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// 現在のシステムカーソルと、選択中プリセットのカーソルを比較一覧として組み立てる。
        /// </summary>
        public void RebuildCursorItems()
        {
            CursorItems.Clear();

            if (SelectedPreset is null)
            {
                return;
            }

            var presetInfo = SelectedPreset.PresetInfo;

            // 現在のシステムカーソル設定を取得
            var snapshot = _systemCursorProvider.CaptureSnapshot();

            // 定義されている全スロット分について行を作成
            foreach (var slot in CursorSlot.All)
            {
                // ---- 現在側 ----
                var currentInfo = snapshot.GetByKey(slot.Key);
                var currentFullPath = currentInfo?.FullPath;
                var currentFileName = currentFullPath is null ? null : Path.GetFileName(currentFullPath);
                ImageSource? currentImage = CursorPreviewFactory.CreatePreview(currentFullPath);
                bool isCurrentMissing = currentImage is null;

                // ---- プリセット側 ----
                var presetFullPath = presetInfo.ResolveCursorPath(slot);
                var presetFileName = presetFullPath is null ? null : Path.GetFileName(presetFullPath);
                ImageSource? presetImage = CursorPreviewFactory.CreatePreview(presetFullPath);
                bool isPresetMissing = presetImage is null;

                var item = new CursorComparisonItemViewModel(
                    displayName: slot.DisplayName,
                    keyName: slot.Key,
                    currentImage: currentImage,
                    currentFileName: currentFileName,
                    currentFullPath: currentFullPath,
                    isCurrentMissing: isCurrentMissing,
                    presetImage: presetImage,
                    presetFileName: presetFileName,
                    presetFullPath: presetFullPath,
                    isPresetMissing: isPresetMissing);

                CursorItems.Add(item);
            }
        }

        /// <summary>
        /// presets フォルダから全プリセットを読み込んで一覧を更新する。
        /// </summary>
        public void LoadPresets()
        {
            ReloadPresets(null);
        }

        /// <summary>
        /// フォルダから新規インポートし、一覧に追加する。
        /// 戻り値として追加されたアイテムを返す。
        /// </summary>
        public PresetItemViewModel ImportFromFolder(string folderPath)
        {
            var preset = _presetService.ImportFromFolder(folderPath);
            var vm = new PresetItemViewModel(preset);
            Presets.Add(vm);

            SelectedPreset = vm;
            return vm;
        }

        /// <summary>
        /// ZIP から新規インポートし、一覧に追加する。
        /// 戻り値として追加されたアイテムを返す。
        /// </summary>
        public PresetItemViewModel ImportFromZip(string zipPath)
        {
            var preset = _presetService.ImportFromZip(zipPath);
            var vm = new PresetItemViewModel(preset);
            Presets.Add(vm);

            SelectedPreset = vm;
            return vm;
        }

        /// <summary>
        /// presets フォルダからの再読み込み。
        /// </summary>
        /// <param name="preferFolderPath">
        /// 再読み込み後に優先的に選択したいプリセットのフォルダパス。
        /// null の場合は、できるだけ以前の選択状態を維持する。
        /// </param>
        public void ReloadPresets(string? preferFolderPath)
        {
            var previousFolder = preferFolderPath ?? SelectedPreset?.FolderPath;

            Presets.Clear();

            var presets = _presetService
                .LoadAllPresets()
                .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase);

            foreach (var preset in presets)
            {
                Presets.Add(new PresetItemViewModel(preset));
            }

            if (!string.IsNullOrEmpty(previousFolder))
            {
                SelectedPreset = Presets.FirstOrDefault(p => p.FolderPath == previousFolder)
                                 ?? Presets.FirstOrDefault(p => p.Name == previousFolder);
            }
            else if (Presets.Count > 0 && SelectedPreset is null)
            {
                SelectedPreset = Presets[0];
            }
        }

        /// <summary>
        /// 選択中プリセットを ZIP にエクスポートする。
        /// </summary>
        public void ExportSelectedPreset(string exportZipPath, bool overwrite)
        {
            if (SelectedPreset is null)
                return;

            _presetService.ExportPresetToZip(SelectedPreset.PresetInfo, exportZipPath, overwrite);
        }

        /// <summary>
        /// 選択中のプリセットを削除し、一覧から取り除く。
        /// </summary>
        public void DeleteSelectedPreset()
        {
            if (SelectedPreset is null)
                return;

            var folderPath = SelectedPreset.FolderPath;
            _presetService.DeletePreset(SelectedPreset.PresetInfo);

            // 削除対象を記録しておき、ReloadPresets で次の選択を決める
            ReloadPresets(folderPath);
        }

        /// <summary>
        /// 選択中プリセットのフォルダ名を変更する。
        /// </summary>
        public void RenameSelectedPreset(string newFolderName)
        {
            if (SelectedPreset is null)
                return;

            var renamed = _presetService.RenamePreset(SelectedPreset.PresetInfo, newFolderName);

            // フォルダ名が変わったので一覧をリロードし、リネーム後のプリセットを再選択
            ReloadPresets(renamed.FolderPath);
        }

        /// <summary>
        /// 選択中のプリセットを適用する。
        /// </summary>
        public void ApplySelectedPreset()
        {
            if (SelectedPreset is null)
                return;

            _presetService.ApplyPreset(SelectedPreset.PresetInfo);
        }

        /// <summary>
        /// 選択中プリセットを複製し、一覧に追加する。
        /// </summary>
        public void DuplicateSelectedPreset()
        {
            if (SelectedPreset is null)
                return;

            var duplicated = _presetService.DuplicatePreset(SelectedPreset.PresetInfo);

            // 生成されたプリセットを選択した状態で一覧をリロード
            ReloadPresets(duplicated.FolderPath);
        }
    }
}
