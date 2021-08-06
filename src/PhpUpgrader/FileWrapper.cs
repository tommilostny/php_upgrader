using System;
using System.IO;

namespace PhpUpgrader
{
    /// <summary> Třída udržující informace o souboru (obsah, cesta, příznak modifikace). </summary>
    public class FileWrapper
    {
        /// <summary> Cesta k souboru. </summary>
        public string Path { get; }

        /// <summary> Obsah souboru. </summary>
        public string Content
        {
            get => _content;
            set
            {
                if (value == _content)
                    return;

                _content = value;
                IsModified = true;
            }
        }
        private string _content;

        /// <summary> Příznak modifikace obsahu souboru. </summary>
        public bool IsModified { get; set; }

        /// <summary> Symbol značící nemodifikovaný soubor (černá). </summary>
        public const string UnmodifiedSymbol = "⚫";

        /// <summary> Symbol značící modifikovaný soubor (žlutá). </summary>
        public const string ModifiedSymbol = "🟡";

        /// <summary> Obsah souboru je zadán parametrem. </summary>
        /// <param name="path"> Cesta k souboru. </param>
        /// <param name="content"> Obsah souboru. </param>
        public FileWrapper(string path, string content)
        {
            Path = path;
            Content = content;
            IsModified = false;
        }

        /// <summary> Obsah souboru je načten z disku na zadané cestě. </summary>
        /// <param name="path"> Cesta k souboru. </param>
        public FileWrapper(string path) : this(path, File.ReadAllText(path))
        {
        }

        /// <summary> Uložit modifikovaný obsah souboru. </summary>
        public void Save()
        {
            if (IsModified) File.WriteAllText(Path, Content);
        }

        /// <summary> Vypíše název souboru a stav modifikace. </summary>
        public void WriteStatus()
        {
            string displayName = Path.Contains(@"\weby\") ? Path[(Path.IndexOf(@"\weby\") + 6)..] : Path;

            string symbol = IsModified ? ModifiedSymbol : UnmodifiedSymbol;

            Console.WriteLine($"\r{symbol} {displayName}");
        }
    }
}
