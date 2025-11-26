using CursorPresetTool.Core.Cursors;
using CursorPresetTool.Core.Models;
using CursorPresetTool.Core.Packs;
using CursorPresetTool.Gui.Config;
using CursorPresetTool.Gui.Imaging;
using CursorPresetTool.Gui.Localization;
using CursorPresetTool.Gui.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;

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
        private readonly ConfigService _configService;
        private readonly LocalizationService _localizationService;


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

        public MainViewModel(
            PresetService presetService,
            SystemCursorProvider systemCursorProvider,
            ConfigService configService,
            LocalizationService localizationService)
        {
            _presetService = presetService;
            _systemCursorProvider = systemCursorProvider;
            _configService = configService;
            _localizationService = localizationService;

            Presets = new ObservableCollection<PresetItemViewModel>();

            LoadPresets();
            RestorePinnedFromConfig();
        }

        private void RestorePinnedFromConfig()
        {
            var pinnedSet = new HashSet<string>(_configService.Current.PinnedPresets,
                                                StringComparer.OrdinalIgnoreCase);

            foreach (var p in Presets)
            {
                // PresetInfo.Name をキーにする（＝フォルダ名）
                p.IsPinned = pinnedSet.Contains(p.Name);
            }

            ResortPresets();
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

            // リロード後にピン状態を復元
            RestorePinnedFromConfig();

            // ↓最後に選択処理
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

            // 実際にカーソルを適用
            _presetService.ApplyPreset(SelectedPreset.PresetInfo);

            // ★ 一覧の「適用中」フラグを更新
            foreach (var preset in Presets)
            {
                preset.IsApplied = ReferenceEquals(preset, SelectedPreset);
            }

            // ★ 右下の比較リストも現在カーソルを取り直して再構築
            RebuildCursorItems();
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

        public void ResortPresets()
        {
            // ピン留め → 非ピン留め の順で並べ替える
            var sorted = Presets
                .OrderByDescending(p => p.IsPinned)
                .ThenBy(p => p.PackName)
                .ToList();

            // コレクションをクリアして並べ替えた順に追加
            Presets.Clear();
            foreach (var p in sorted)
                Presets.Add(p);

            var pinnedFolders = Presets
                .Where(p => p.IsPinned)
                .Select(p => p.Name);

            _configService.UpdatePinnedPresets(pinnedFolders);
        }

    }
}
