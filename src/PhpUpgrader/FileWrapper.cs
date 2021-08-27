﻿using System;
using System.Collections.Generic;
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

        /// <summary> Symbol značící nemodifikovaný soubor (černá). </summary>
        public const string UnmodifiedSymbol = "⚫";

        /// <summary> Symbol značící modifikovaný soubor (modrá). </summary>
        public const string ModifiedSymbol = "🔵";

        /// <summary> Symbol varování o možné chybě. </summary>
        public const string WarningSymbol = "⚠️";

        /// <summary> Seznam varování o možných chybách. Zobrazí se za výpisem stavu o souboru. </summary>
        public List<string> Warnings { get; } = new();

        /// <summary> Přejmenování/přesunutí souboru na tuto cestu při ukládání, pokud není null. </summary>
        public string? MoveOnSavePath { get; set; } = null;

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
        /// <remarks> Přesune soubor na cestu <see cref="MoveOnSavePath"/>, pokud není null. </remarks>
        public void Save()
        {
            if (!IsModified)
                return;

            File.WriteAllText(Path, Content);

            if (MoveOnSavePath is not null)
                File.Move(Path, MoveOnSavePath);
        }

        /// <summary> Vypíše název souboru a stav modifikace. </summary>
        public void WriteStatus()
        {
            string displayName = Path.Contains(@"\weby\") ? Path[(Path.IndexOf(@"\weby\") + 6)..] : Path;

            string symbol = IsModified ? ModifiedSymbol : UnmodifiedSymbol;

            Console.WriteLine($"{symbol} {displayName}");
            foreach (var warning in Warnings)
            {
                var defaultColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Error.WriteLine($"{WarningSymbol} {warning}");
                Console.ForegroundColor = defaultColor;
            }
        }

        internal bool OverwriteModificationFlag(bool newValue) => IsModified = newValue;
    }
}
