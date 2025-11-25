using CursorPresetTool.Core.Packs;

namespace CursorPresetTool.Gui.ViewModels
{
    /// <summary>
    /// プリセット一覧の1行分を表す ViewModel。
    /// </summary>
    public sealed class PresetItemViewModel : ViewModelBase
    {
        public PresetInfo PresetInfo { get; }

        public string Name => PresetInfo.Name;
        public string FolderPath => PresetInfo.FolderPath;

        public string PackName => string.IsNullOrWhiteSpace(PresetInfo.Pack.PackName)
            ? "(名称未設定)"
            : PresetInfo.Pack.PackName;

        public string? Author => PresetInfo.Pack.Author;
        public string? Version => PresetInfo.Pack.Version;
        public string? Description => PresetInfo.Pack.Description;

        public string DisplayName => $"{Name} ({PackName})";

        private bool _isApplied;
        public bool IsApplied
        {
            get => _isApplied;
            set => SetProperty(ref _isApplied, value);
        }

        private bool _isPinned;
        public bool IsPinned
        {
            get => _isPinned;
            set => SetProperty(ref _isPinned, value);
        }

        public PresetItemViewModel(PresetInfo presetInfo)
        {
            PresetInfo = presetInfo;
            _isApplied = false;
            _isPinned = false;
        }
    }
}
