using DocumentFormat.OpenXml.Wordprocessing;
using DocumentsGenerator.MVVM.View;
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
            try
            {
                Directory.GetFiles(outputDictionaryPath);
            }
            catch (DirectoryNotFoundException)
            {
                DialogWindow.ShowError($"Wybrany folder docelowy: {outputDictionaryPath} nie istnieje.", "Folder nie istnieje");
                return;
            }
            catch (IOException)
            {
                DialogWindow.ShowError($"Wybrany folder docelowy: {outputDictionaryPath} jest nieosiągalny.", "Folder nieosiągalny");
                return;
            }
            catch (UnauthorizedAccessException)
            {
                DialogWindow.ShowError($"Brak uprawnień do otworzenia wybranego folderu docelowego: {outputDictionaryPath}", "Odmowa dostępu");
                return;
            }

            // HashSet to keep track of seen elements
            HashSet<string> seenElements = [];

            DateTime now = DateTime.Now;
            string modDate = now.ToString("f");

            // Create output file in given namespace, defaults to template-data
            if (ns == null)
            {
                ns = "template-data";
            }
            XDocument outputFile = new(new XElement(ns + "root"));

            try
            {
                // Iterate through all files in a directory
                foreach (var file in LoadedFileNames!)
                {
                    //Load file
                    XDocument doc = XDocument.Load(file.FilePath!);
                    // Iterate through all elements in loaded file
                    foreach (var element in doc.Root!.Elements())
                    {
                        // Add element to output file if its unique
                        if (seenElements.Add(element.Name.ToString()))
                        {
                            // Add modification-date attribute to an element if it doesn't have it
                            if (element.Attribute("modification-date") == null)
                            {
                                element.SetAttributeValue("modification-date", modDate);
                            }
                            outputFile.Root!.Add(element);
                        }
                    }
                }
                string generationDate = now.ToString("dd'-'MM'-'yyyy'T'HH'-'mm'-'ss");
                string outputFileName = $"{generationDate}_arkusz_danych.xml";
                string savePath = $@"{outputDictionaryPath}\{outputFileName}";
                outputFile.Save(savePath);
            }
            catch (Exception ex) {
                DialogWindow.ShowError($"Błąd podczas scalania arkuszy danych.\nBłąd: {ex.Message}", "Błąd!");
                return;
            }

            DialogWindow.ShowInfo($"Generowanie zakończone pomyślnie. Arkusz został wygenerowany w:\n{outputDictionaryPath}", "Generowanie zakończone");
        }
        public DataSheetModel() { }
    }
}
