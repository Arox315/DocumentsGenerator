using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DocumentsGenerator.MVVM.Model
{
    class DataSheetModel
    {
        public ObservableCollection<LoadedFileNameModel>? LoadedFileNames { get; set; }
        public void MergeDataSheets(string outputDictionaryPath, XNamespace? ns = null)
        {
            // HashSet to keep track of seen elements
            HashSet<string> seenElements = new HashSet<string>();

            // Create output file in given namespace (defaults to template-data)
            if (ns == null)
            {
                ns = "template-data";
            }
            XDocument outputFile = new XDocument(new XElement(ns + "root"));

            // Iterate through all files in a directory
            foreach(var file in LoadedFileNames!)
            {
                //Load file
                XDocument doc = XDocument.Load(file.FilePath!);
                // Iterate through all elements in loaded file
                foreach (var element in doc.Root!.Elements())
                {
                    // Add element to output file if its unique
                    if (seenElements.Add(element.ToString()))
                    {
                        outputFile.Root!.Add(element);
                    }
                }
            }
            string generationDate = DateTime.Now.ToString("dd'-'MM'-'yyyy'T'HH'-'mm'-'ss");
            string outputFileName = $"{generationDate}_arkusz_danych.xml";
            string savePath = $@"{outputDictionaryPath}\{outputFileName}";
            outputFile.Save(savePath);
        }
        public DataSheetModel() { }
    }
}
