using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentsGenerator
{
    
    public class SubPair
    {
        public string SubKey { get; set; } = "";
        public string SubValue { get; set; } = "";
    }

    public class ValueItem
    {
        public string Name { get; set; } = "";
        public ObservableCollection<SubPair> SubPairs { get; set; } = new();
    }

    public class KeyItem
    {
        public string Name { get; set; } = "";
        public ObservableCollection<ValueItem> Values { get; set; } = new();
    }
}
