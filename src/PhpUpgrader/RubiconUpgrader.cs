using System;

namespace PhpUpgrader
{
    /// <summary> PHP upgrader pro systém Rubicon, založený na upgraderu pro systém Mona. </summary>
    public class RubiconUpgrader : MonaUpgrader
    {
        /// <summary>  </summary>
        public RubiconUpgrader(string baseFolder, string webName) : base(baseFolder, webName)
        {
        }

        /// <summary> Procedura aktualizace Rubicon souborů. </summary>
        /// <remarks> Použita ve volání metody <see cref="MonaUpgrader.UpgradeAllFilesRecursively"/>. </remarks>
        /// <returns> Upravený soubor. </returns>
        protected override FileWrapper UpgradeProcedure(string filePath)
        {
            if (UpgradeTinyAjaxBehavior(filePath))
                return null;

            var file = new FileWrapper(filePath);

            if (!filePath.Contains("tiny_mce"))
                UpgradeFindReplace(file);
            UpgradeConstructors(file);
            UpgradeRegexFunctions(file);
            return file;
        }

        /// <summary> Old style constructor function ClassName() => function __construct() </summary>
        public void UpgradeConstructors(FileWrapper file)
        {
            throw new NotImplementedException();
        }
    }
}
