using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DocumentsGenerator.MVVM.Model
{
    public class AllKeysModel : INotifyPropertyChanged
    {
        private string? _value;
        public string? Value
        {
            get => _value;
            set { if (_value != value) { _value = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
