using CursorPresetTool.Core.Cursors;
using CursorPresetTool.Core.Models;
using CursorPresetTool.Core.Packs;
using CursorPresetTool.Gui.Imaging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace CursorPresetTool.Gui.ViewModels
{
    /// <summary>
    /// 各カーソルスロットの編集用 VM。
    /// </summary>
    public sealed class CursorSlotEditorItemViewModel : INotifyPropertyChanged
    {
        private string? _relativePath;
        private string? _fullPath;
        private ImageSource? _previewImage;
        private bool _isMissing;

        /// <summary>カーソルスロット定義。</summary>
        public CursorSlot Slot { get; }

        /// <summary>UI 表示用の日本語名など。</summary>
        public string DisplayName => Slot.DisplayName;

        /// <summary>レジストリキー名 (Arrow / Hand / Wait ...)。</summary>
        public string Key => Slot.Key;

        /// <summary>
        /// pack.json に書き込まれる相対パス（またはファイル名）。
        /// 空または null の場合、そのスロットは未設定扱い。
        /// </summary>
        public string? RelativePath
        {
            get => _relativePath;
            set
            {
                if (_relativePath != value)
                {
                    _relativePath = value;
                    OnPropertyChanged();
                    Changed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// 実際のフルパス（新規プリセット作成時は「元ファイルの場所」）。
        /// Save のときに、このパスからプリセットフォルダへコピーする想定。
        /// </summary>
        public string? FullPath
        {
            get => _fullPath;
            set
            {
                if (_fullPath != value)
                {
                    _fullPath = value;
                    OnPropertyChanged();
                    Changed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// プレビュー用のカーソル画像。
        /// </summary>
        public ImageSource? PreviewImage
        {
            get => _previewImage;
            set
            {
                if (!Equals(_previewImage, value))
                {
                    _previewImage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsMissing)); // 見た目更新
                    Changed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// プレビューを表示できるかどうか。
        /// </summary>
        public bool IsMissing
        {
            get => _isMissing;
            set
            {
                if (_isMissing != value)
                {
                    _isMissing = value;
                    OnPropertyChanged();
                    Changed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// 親 VM に「変更されたよ」と伝えるためのイベント。
        /// </summary>
        public event EventHandler? Changed;

        public CursorSlotEditorItemViewModel(CursorSlot slot, string? relativePath)
        {
            Slot = slot;
            _relativePath = relativePath;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// プリセット編集ウィンドウ全体の VM。
    /// 既存プリセット編集 / 新規プリセット作成の両方を扱えるようにしておく。
    /// </summary>
    public sealed class PresetEditorViewModel : INotifyPropertyChanged
    {
        private string _packName = string.Empty;
        private string? _author;
        private string? _version;
        private string? _description;
        private bool _usePackNameAsFolderName;
        private string _folderName = string.Empty;
        private bool _isDirty;

        /// <summary>
        /// 新規作成モードかどうか。
        /// true: presets フォルダ未作成 / 保存時にフォルダ名決定
        /// false: 既存プリセット編集（フォルダパスは固定）
        /// </summary>
        public bool IsNew { get; }

        /// <summary>
        /// 既存プリセット編集の場合の元フォルダパス。
        /// 新規作成モードでは null。
        /// </summary>
        public string? OriginalFolderPath { get; }

        /// <summary>
        /// 表示名（pack.json の PackName）
        /// </summary>
        public string PackName
        {
            get => _packName;
            set
            {
                if (_packName != value)
                {
                    _packName = value;
                    OnPropertyChanged();
                    MarkDirty();

                    if (IsNew && UsePackNameAsFolderName)
                    {
                        FolderName = value;
                    }
                }
            }
        }

        public string? Author
        {
            get => _author;
            set
            {
                if (_author != value)
                {
                    _author = value;
                    OnPropertyChanged();
                    MarkDirty();
                }
            }
        }

        public string? Version
        {
            get => _version;
            set
            {
                if (_version != value)
                {
                    _version = value;
                    OnPropertyChanged();
                    MarkDirty();
                }
            }
        }

        public string? Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 新規作成時に PackName をそのままフォルダ名として使うか。
        /// IsNew=false の場合は無視（既存編集ではフォルダ名は固定）。
        /// </summary>
        public bool UsePackNameAsFolderName
        {
            get => _usePackNameAsFolderName;
            set
            {
                if (_usePackNameAsFolderName != value)
                {
                    _usePackNameAsFolderName = value;
                    OnPropertyChanged();
                    MarkDirty();

                    if (IsNew && _usePackNameAsFolderName)
                    {
                        FolderName = PackName;
                    }
                }
            }
        }

        /// <summary>
        /// presets 直下のフォルダ名。
        /// 新規作成時のみ編集可能。既存編集では表示専用。
        /// </summary>
        public string FolderName
        {
            get => _folderName;
            set
            {
                if (_folderName != value)
                {
                    _folderName = value;
                    OnPropertyChanged();
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 何かしら編集されたかどうか。
        /// </summary>
        public bool IsDirty
        {
            get => _isDirty;
            private set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 各カーソルスロットの編集行。
        /// </summary>
        public ObservableCollection<CursorSlotEditorItemViewModel> Slots { get; } = new();

        private PresetEditorViewModel(bool isNew, string? originalFolderPath)
        {
            IsNew = isNew;
            OriginalFolderPath = originalFolderPath;

            if (IsNew)
            {
                UsePackNameAsFolderName = true;
            }
        }

        /// <summary>
        /// 既存プリセット編集用の VM を生成。
        /// </summary>
        public static PresetEditorViewModel FromExistingPreset(PresetInfo preset)
        {
            if (preset is null) throw new ArgumentNullException(nameof(preset));

            var vm = new PresetEditorViewModel(isNew: false, originalFolderPath: preset.FolderPath)
            {
                UsePackNameAsFolderName = false,
                FolderName = Path.GetFileName(preset.FolderPath),
                PackName = preset.Pack.PackName,
                Author = preset.Pack.Author,
                Version = preset.Pack.Version,
                Description = preset.Pack.Description
            };

            // スロットを展開
            foreach (var slot in CursorSlot.All)
            {
                string? relativePath = null;
                string? fullPath = null;

                if (preset.Pack.CursorMap.TryGetPath(slot.Id, out var path))
                {
                    relativePath = path;

                    // ルート付きならそのまま、そうでなければプリセットフォルダ基準で解決
                    if (!string.IsNullOrWhiteSpace(relativePath))
                    {
                        if (Path.IsPathRooted(relativePath))
                        {
                            fullPath = relativePath;
                        }
                        else
                        {
                            fullPath = Path.Combine(preset.FolderPath, relativePath);
                        }
                    }
                }

                var preview = CursorPreviewFactory.CreatePreview(fullPath);
                bool isMissing = preview is null;

                var item = new CursorSlotEditorItemViewModel(slot, relativePath)
                {
                    FullPath = fullPath,
                    PreviewImage = preview,
                    IsMissing = isMissing,
                };

                item.Changed += (_, __) => vm.MarkDirty();
                vm.Slots.Add(item);
            }

            vm.IsDirty = false;
            return vm;
        }

        /// <summary>
        /// （後で実装）新規プリセット作成用 VM 生成。
        /// 作成オプション（空 / 現在設定 / 選択中プリセット）に応じて初期値を変える想定。
        /// </summary>
        public static PresetEditorViewModel CreateNewEmpty()
        {
            var vm = new PresetEditorViewModel(isNew: true, originalFolderPath: null)
            {
                UsePackNameAsFolderName = true,
                FolderName = string.Empty,
                PackName = "New Preset",
                Author = "Your Name",
                Version = "1.0.0",
                Description = "This is a new preset."
            };

            foreach (var slot in CursorSlot.All)
            {
                var item = new CursorSlotEditorItemViewModel(slot, relativePath: null)
                {
                    FullPath = null,
                    PreviewImage = null,
                    IsMissing = true,
                };

                item.Changed += (_, __) => vm.MarkDirty();
                vm.Slots.Add(item);
            }

            vm.IsDirty = false;
            return vm;
        }

        public static PresetEditorViewModel CreateNewFromSystem(SystemCursorSnapshot snapshot)
        {
            var vm = new PresetEditorViewModel(isNew: true, originalFolderPath: null)
            {
                UsePackNameAsFolderName = true,
                FolderName = string.Empty,
                PackName = "New Preset",
                Author = "Your Name",
                Version = "1.0.0",
                Description = "This is a new preset."
            };

            foreach (var slot in CursorSlot.All)
            {
                string? relativePath = null;
                string? fullPath = null;

                var info = snapshot.GetByKey(slot.Key);
                if (info?.FullPath is string currentFull)
                {
                    fullPath = currentFull;
                    // プリセットフォルダにコピーする時のファイル名だけ先に決めておく
                    relativePath = Path.GetFileName(currentFull);
                }

                var preview = CursorPreviewFactory.CreatePreview(fullPath);
                bool isMissing = preview is null;

                var item = new CursorSlotEditorItemViewModel(slot, relativePath)
                {
                    FullPath = fullPath,
                    PreviewImage = preview,
                    IsMissing = isMissing,
                };

                item.Changed += (_, __) => vm.MarkDirty();
                vm.Slots.Add(item);
            }

            vm.IsDirty = false;
            return vm;
        }

        public static PresetEditorViewModel CreateNewFromPreset(PresetInfo basePreset)
        {
            if (basePreset is null) throw new ArgumentNullException(nameof(basePreset));

            var vm = new PresetEditorViewModel(isNew: true, originalFolderPath: null)
            {
                UsePackNameAsFolderName = true,
                FolderName = string.Empty,
                PackName = basePreset.Pack.PackName + "_Copy",
                Author = basePreset.Pack.Author,
                Version = basePreset.Pack.Version,
                Description = basePreset.Pack.Description
            };

            foreach (var slot in CursorSlot.All)
            {
                string? relativePath = null;
                string? fullPath = null;

                if (basePreset.Pack.CursorMap.TryGetPath(slot.Id, out var path))
                {
                    relativePath = path;

                    if (!string.IsNullOrWhiteSpace(relativePath))
                    {
                        if (Path.IsPathRooted(relativePath))
                        {
                            fullPath = relativePath;
                        }
                        else
                        {
                            fullPath = Path.Combine(basePreset.FolderPath, relativePath);
                        }
                    }
                }

                var preview = CursorPreviewFactory.CreatePreview(fullPath);
                bool isMissing = preview is null;

                var item = new CursorSlotEditorItemViewModel(slot, relativePath)
                {
                    FullPath = fullPath,
                    PreviewImage = preview,
                    IsMissing = isMissing,
                };

                item.Changed += (_, __) => vm.MarkDirty();
                vm.Slots.Add(item);
            }

            vm.IsDirty = false;
            return vm;
        }

        /// <summary>
        /// 現在の編集内容から CursorPack を組み立てる。
        /// 保存時に PresetService に渡す用。
        /// </summary>
        public CursorPack ToCursorPack()
        {
            var map = new CursorMap();

            foreach (var slotItem in Slots)
            {
                // RelativePath が空ならそのスロットは未設定扱い
                map.SetPath(slotItem.Slot.Id, slotItem.RelativePath);
            }

            var pack = new CursorPack
            {
                PackName = PackName,
                Author = Author,
                Version = Version,
                Description = Description,
                CursorMap = map
            };

            return pack;
        }


        private void MarkDirty()
        {
            if (!IsDirty)
            {
                IsDirty = true;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
