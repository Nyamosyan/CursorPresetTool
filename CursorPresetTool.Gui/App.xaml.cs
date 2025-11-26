using CursorPresetTool.Gui.Config;
using CursorPresetTool.Gui.Localization;
using System.Configuration;
using System.Data;
using System.Windows;

namespace CursorPresetTool.Gui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public ConfigService ConfigService { get; private set; } = null!;
        public LocalizationService Localization { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ConfigService = new ConfigService();

            var langCode = ConfigService.Current.Lang ?? "ja_jp";
            Localization = new LocalizationService(langCode);

            var mainWindow = new MainWindow(ConfigService, Localization);
            mainWindow.Show();
        }
    }

}
