using CursorPresetTool.Gui.Models;
using System.Windows;

namespace CursorPresetTool.Gui
{
    public partial class NewPresetDialog : Window
    {
        public NewPresetSourceKind SelectedSourceKind { get; private set; } = NewPresetSourceKind.Empty;

        public NewPresetDialog(bool hasSelectedPreset)
        {
            InitializeComponent();

            // 選択中プリセットがない場合は無効化
            if (!hasSelectedPreset)
            {
                FromSelectedRadio.IsEnabled = false;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (FromSystemRadio.IsChecked == true)
            {
                SelectedSourceKind = NewPresetSourceKind.FromSystem;
            }
            else if (FromSelectedRadio.IsChecked == true)
            {
                SelectedSourceKind = NewPresetSourceKind.FromSelectedPreset;
            }
            else
            {
                SelectedSourceKind = NewPresetSourceKind.Empty;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
