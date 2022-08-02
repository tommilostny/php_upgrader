namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class ArrayMissingKeyValue
{
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
        if (file.Path.EndsWith(Path.Join("rubicon", "modules", "search", "main2.php")))
        {
            var lines = file.Content.Split();
            foreach (var line in lines)
            {
                var trimmed = line.ToString().Trim();
                if (!trimmed.StartsWith("//") && trimmed.EndsWith("=>"))
                {
                    var i = 0;
                    for (; i < line.Length && char.IsWhiteSpace(line[i]); i++) ;
                    line.Insert(i, "//");
                }
            }
            lines.JoinInto(file.Content);
        }
        return file;
    }
}
