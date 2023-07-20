namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class ArrayMissingKeyValue
{
    private static readonly string _main2Php = Path.Join("rubicon", "modules", "search", "main2.php");

    /// <summary>
    /// Syntaktická chyba nalezena na webu hokejova-vyzbroj v souboru <b>rubicon/modules/search/main2.php</b>.
    /// Při inicializaci pole chybí hodnota pro klíč "darek_zdarma_info".
    /// Soubor se nepoužívá. Stačí zakomentovat.
    /// </summary>
    /// <remarks>
    /// 'cena_sleva_castka_bez_DPH'	=> $r->cena_bez_dph ,
	///	'cena_sleva_castka_s_DPH'	=> $r->cena_bez_dph ,
	///	"darek_zdarma_info"			=> 
	///	'store'						=> $r->units ,
	///	'varianty_info'				=> '' ,
	/// ];
    /// </remarks>
    public static FileWrapper UpgradeArrayMissingKeyValue(this FileWrapper file)
    {
        if (file.Path.EndsWith(_main2Php, StringComparison.Ordinal))
        {
            var lines = file.Content.Split();
            foreach (var line in lines)
            {
                var trimmed = line.ToString().Trim();
                if (!trimmed.StartsWith("//", StringComparison.Ordinal) && trimmed.EndsWith("=>", StringComparison.Ordinal))
                {
                    var i = 0;
                    while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
                    line.Insert(i, "//");
                }
            }
            lines.JoinInto(file.Content);
        }
        return file;
    }
}
