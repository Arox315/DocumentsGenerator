using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DocumentsGenerator.MVVM.Model
{
    class DataSheetItemModel : INotifyPropertyChanged
    {
        private string? _key;
        private string? _value;

        public string? Key
        {
            get => _key;
            set { if (_key != value) { _key = value; OnPropertyChanged(); } }
        }

        public string? Value
        {
            get => _value;
            set { if (_value != value) { _value = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
