using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentsGenerator.MVVM.Model
{
    class TemplateModel
    {
        public ObservableCollection<LoadedFileNameModel>? LoadedFileNames { get; set; }

        public TemplateModel() 
        {
           
        }
    }
}
