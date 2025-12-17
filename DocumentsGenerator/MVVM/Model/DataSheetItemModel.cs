using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DocumentsGenerator.MVVM.Model
{
    class DataSheetItemModel : INotifyPropertyChanged
    {
        private string? _key;
        private string? _displayKey;
        private string? _value;
        private string? _textBoxVisibility;
        private string? _comboBoxVisibility;
        private string? _feedbackHelperValue;
        private string? _modificationDate;

        public string? Key
        {
            get => _key;
            set { if (_key != value) { _key = value; OnPropertyChanged(); } }
        }

        public string? DisplayKey
        {
            get => _displayKey;
            set { if (_displayKey != value) { _displayKey = value; OnPropertyChanged(); } }
        }

        public string? Value
        {
            get => _value;
            set { if (_value != value) { _value = value; OnPropertyChanged(); } }
        }

        public string? TextBoxVisibility
        {
            get => _textBoxVisibility;
            set { if (_textBoxVisibility != value) { _textBoxVisibility = value; OnPropertyChanged(); } }
        }

        public string? ComboBoxVisibility
        {
            get => _comboBoxVisibility;
            set { if (_comboBoxVisibility != value) { _comboBoxVisibility = value; OnPropertyChanged(); } }
        }

        public string? FeedbackHelperValue
        {
            get => _feedbackHelperValue;
            set { if (_feedbackHelperValue != value) {_feedbackHelperValue = value; OnPropertyChanged(nameof(FeedbackHelperValue)); }}
        }

        public string? ModificationDate
        {
            get => _modificationDate;
            set { if (_modificationDate != value) { _modificationDate = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<string>? Values { get; set; }
        private string? _selectedValue;
        public string SelectedValue
        {
            get => _selectedValue!;
            set 
            { 
                if (_selectedValue != value) 
                {
                    _selectedValue = value;
                    Value = value;
                    OnPropertyChanged(nameof(SelectedValue)); 
                } 
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    }
}
