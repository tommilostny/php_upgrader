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
        public bool IsModified { get; private set; }

        /// <summary> Inicializace, načtení obsahu souboru. </summary>
        /// <param name="path"> Cesta k souboru. </param>
        /// <param name="content"> Obsah souboru (prázdné => načíst ze souboru zadaného cestou). </param>
        public FileWrapper(string path, string? content = null)
        {
            Content = content ?? File.ReadAllText(path);
            IsModified = false;
            Path = path;
        }

        /// <summary> Uložit modifikovaný obsah souboru. </summary>
        public void Save()
        {
            if (IsModified) File.WriteAllText(Path, Content);
        }
    }
}
