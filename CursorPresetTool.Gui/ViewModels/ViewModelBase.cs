using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CursorPresetTool.Gui.ViewModels
{
    /// <summary>
    /// WPF用の基本ViewModelベースクラス。
    /// INotifyPropertyChanged + SetProperty を提供する。
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// プロパティ値を設定し、変更があれば PropertyChanged を発火する共通ヘルパー。
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 明示的に PropertyChanged を発火したい場合用。
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (propertyName is null)
                return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
