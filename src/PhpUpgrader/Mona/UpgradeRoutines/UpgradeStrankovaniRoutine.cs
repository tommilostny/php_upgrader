namespace PhpUpgrader.Mona.UpgradeRoutines;

public static class UpgradeStrankovaniRoutine
{
    private const string _pdFunc = "function predchozi_dalsi";

    /// <summary>
    /// upravit soubor funkce/strankovani.php
    ///     >>>  function predchozi_dalsi($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)
    /// </summary>
    public static void UpgradeStrankovani(this FileWrapper file)
    {
        switch (file)
        {
            case { Path: var p } when !p.Contains(Path.Join("funkce", "strankovani.php")):
            case { Content: var c } when !c.Contains(_pdFunc):
                return;
        }
        foreach (var (old, updated) in PredchoziDalsiVariants())
        {
            file.Content.Replace(old, updated);

            if (file.Content.Contains(updated))
                return;
        }
        //zahlásit chybu při nalezení další varianty funkce predchozi_dalsi
        file.Warnings.Add("Nalezena neznámá varianta funkce predchozi_dalsi.");
    }

    private static IEnumerable<(string old, string updated)> PredchoziDalsiVariants()
    {
        yield return ($"{_pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext)",
                      $"{_pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null)"
        );
        yield return ($"{_pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext, $prenext_2)",
                      $"{_pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null, $prenext_2 = null)"
        );
        yield return ($"{_pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta, $pre, $next)",
                      $"{_pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta = null, $pre = null, $next = null)"
        );
        yield return ($"{_pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta, $prenext, $filter)",
                      $"{_pdFunc}($zobrazena_strana, $pocet_stran, $textact, $texta = null, $prenext = null, $filter = null)"
        );
    }
}
