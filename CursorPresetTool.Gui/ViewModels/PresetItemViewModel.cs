using CursorPresetTool.Core.Packs;

namespace CursorPresetTool.Gui.ViewModels
{
    /// <summary>
    /// プリセット一覧の1行分を表す ViewModel。
    /// </summary>
    public sealed class PresetItemViewModel
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

        public PresetItemViewModel(PresetInfo presetInfo)
        {
            PresetInfo = presetInfo;
        }
    }
}
