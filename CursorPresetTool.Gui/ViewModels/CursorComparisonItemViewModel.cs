using System.Windows.Media;

namespace CursorPresetTool.Gui.ViewModels
{
    public sealed class CursorComparisonItemViewModel
    {
        // 例: "通常選択"
        public string DisplayName { get; }

        // 例: "Arrow"（レジストリキー名）
        public string KeyName { get; }

        // 現在適用中
        public ImageSource? CurrentImage { get; }
        public string? CurrentFileName { get; }
        public string? CurrentFullPath { get; }
        public bool IsCurrentMissing { get; }

        // プリセット側
        public ImageSource? PresetImage { get; }
        public string? PresetFileName { get; }
        public string? PresetFullPath { get; }
        public bool IsPresetMissing { get; }

        public CursorComparisonItemViewModel(
            string displayName,
            string keyName,
            ImageSource? currentImage,
            string? currentFileName,
            string? currentFullPath,
            bool isCurrentMissing,
            ImageSource? presetImage,
            string? presetFileName,
            string? presetFullPath,
            bool isPresetMissing)
        {
            DisplayName = displayName;
            KeyName = keyName;
            CurrentImage = currentImage;
            CurrentFileName = currentFileName;
            CurrentFullPath = currentFullPath;
            IsCurrentMissing = isCurrentMissing;
            PresetImage = presetImage;
            PresetFileName = presetFileName;
            PresetFullPath = presetFullPath;
            IsPresetMissing = isPresetMissing;
        }
    }
}
