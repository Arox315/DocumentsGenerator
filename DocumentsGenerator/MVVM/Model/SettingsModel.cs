using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentsGenerator.MVVM.Model
{
    public class SettingsModel
    {
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Value { get; set; }
        public string? Type { get; set; } = "Text";
    }
}
