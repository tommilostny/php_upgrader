namespace PhpUpgrader.Mona;

public partial class MonaUpgrader
{
    /// <summary>
    /// upravit soubor funkce/strankovani.php
    ///     >>>  function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)
    /// </summary>
    public static void UpgradeStrankovani(FileWrapper file)
    {
        const string pdFunc = "function predchozi_dalsi";
        switch (file)
        {
            case { Path: var p } when !p.Contains(Path.Join("funkce", "strankovani.php")):
            case { Content: var c } when !c.Contains(pdFunc):
                return;
        }
        foreach (var (old, updated) in _PredchoziDalsiVariants())
        {
            file.Content.Replace(old, updated);

            if (file.Content.Contains(updated))
                return;
        }
        //zahlásit chybu při nalezení další varianty funkce predchozi_dalsi
        file.Warnings.Add("Nalezena neznámá varianta funkce predchozi_dalsi.");

        //iterátor dvojic 'co hledat?', 'čím to nahradit?' pro varianty funkce predchozi_dalsi
        static IEnumerable<(string old, string updated)> _PredchoziDalsiVariants()
        {
            yield return ($"{pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext)",
                          $"{pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)"
            );
            yield return ($"{pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext, $prenext_2)",
                          $"{pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null, $prenext_2 = null)"
            );
            yield return ($"{pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta, $pre, $next)",
                          $"{pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta = null, $pre = null, $next = null)"
            );
            yield return ($"{pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext, $filter)",
                          $"{pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null, $filter = null)"
            );
        }
    }
}
