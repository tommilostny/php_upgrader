# [PhpUpgraderBase](PhpUpgraderBase.cs)
Abstraktní třída, která obsahuje společné prvky pro RS Mona i Rubicon, jako společné atributy a základní konstruktor, který inicializuje potřebné atributy a nastaví velikost cache *.NET Regex* enginu (regulární výrazy jsou hojně využívány aktualizačními metodami).

Veřejná metoda této třídy ``UpgradeAllFilesRecursively`` prochází zadanou složku se soubory aktualizovaného webu **rekurzivně** a volá abstraktní metodu ``UpgradeProcedure``, která provádí aktualizační rutinu pro jeden soubor. Implementace této metody je na obsažena ve specializované třídě.

---
## [FileWrapper](FileWrapper.cs)
Třída, která udržuje informace o zpracovávaném souboru (obsah, cesta, příznak modifikace). Umožňuje výpis stavu souboru do konzole během aktualizace (společně s kolekcí varování pro uživatele programu) a uložit aktualizovaný obsah souboru, pouze pokud byl upraven.

Pro atribut obsahu souboru je využita třída [StringBuilder](https://docs.microsoft.com/cs-cz/dotnet/api/system.text.stringbuilder?view=net-6.0), pro kterou byly napsány rozšiřující metody [StringBuilderExtensions](StringBuilderExtensions.cs) umožňující jednodušší práci podobnou běžným operacím s řetězci (*Contains*, *Split*, *JoinInto*, ...). Přístup přes StringBuilder byl zvolen z důvodu, že program často přepisuje obsah textového řetězce (obsah souboru) a jelikož běžné řetězce typu string nelze mutovat (tvoří upravené kopie v paměti), tak to může vést k vyšším nárokům na operační paměť (zvláště pro rozsáhlejší soubory) a tlak na *Garbage Collection* (může mít vliv na výkon). StringBuilder tedy přispívá ke snaze nároky na paměť minimalizovat. Nelze použít všude a v některých situacích je nutné stále pracovat s typem string (např. Regex).

---
### [BackupManager](BackupManager.cs)
Pomocná statická třída, která zajišťuje uchování původních verzí programem upravených souborů. Také umožňuje načtění těchto zálohovaných souborů, které lze poté opět upravit nanečisto.

---
### [UnmodifiedMysql_File](UnmodifiedMysql_File.cs)
Záznam o souboru, ve kterém se vyskytuje neaktualizovaná funkce **``mysql_``** (všechny jsou zastaralé a je potřeba nahradit za nové varianty *mysqli_*). K rozpoznání neaktualizované funkce využívá regulárního výrazu (může ignorovat výskyt v komentářích, proměnných, PDO::, ...).


---
## [IConnectHandler](IConnectHandler.cs)
Rozhraní obsahující metodu ``UpgradeConnect``. Implementace PhpUpgraderBase má referenci na specializovanou třídu implementující toto rozhraní ([MonaConnectHandler](Mona/UpgradeHandlers/MonaConnectHandler.cs) a [RubiconConnectHandler](Rubicon/UpgradeHandlers/RubiconConnectHandler.cs)).

Oba systémy mají rozdílný způsob připojení k databázi. Implementace přes rozhraní umožní je jednoduše rozlišit a provést správné kroky pro daný systém.

---
## [IFindReplaceHandler](IFindReplaceHandler.cs)
Rozhraní pro funkcionalitu **Hledat** >> **Nahradit**. Obsahuje kolekci ``Replacements``, kde je definováno co čím nahradit, a metodu ``UpgradeFindReplace``, která prochází soubor a provádí definovaná nahrazení. 

Specializované třídy [MonaFindReplaceHandler](Mona/UpgradeHandlers/MonaFindReplaceHandler.cs) a [RubiconFindReplaceHandler](Rubicon/UpgradeHandlers/RubiconFindReplaceHandler.cs) definují nahrazující pravidla specifická pro daný systém (implementace pro Rubicon tedy rozšiřuje kolekci pravidel pro RS Mona).

---
# [Mona](Mona/MonaUpgrader.cs)
Třída **``MonaUpgrader``** specializuje funkcionalizu ``PhpUpgraderBase`` pro systém Mona. Metoda ``UpgradeProcedure`` používá metody rozšíření pro **``FileWrapper``** definované ve statických třídách ve jmenném prostoru [PhpUpgrader.Mona.UpgradeExtensions](Mona/UpgradeExtensions/).

Činnost této třídy byla částečně rozšířena v rámci aktualizace systémů Rubicon o další obecnější případy týkající se syntaxe PHP.

Provádí svou činnost voláním metod následujících tříd:

1. [TinyAjaxBehavior](Mona/UpgradeExtensions/TinyAjaxBehavior.cs): Jedná se o soubor *admin/include/TinyAjaxBehavior.php*? Pokud ano, překopírovat vzorový soubor ze složky *important*, jinak pokračovat.
1. Pokud se jedná o soubor ze složky *tiny_mce*, provést pouze nahrazení ``IFindReplaceHandler.UpgradeFindReplace`` a speciální případ [TinyMceUploaded](Mona/UpgradeExtensions/TinyMceUploaded.cs), ve kterém PHP parser hlásí chybu.
1. Pro ostatní se zavolá i ``IConnectHandler.UpgradeConnect``, která upraví soubor *connect/connection.php* dle vzoru ve složce *important*.
1. [ResultFunction](Mona/UpgradeExtensions/ResultFunction.cs): Úprava volání funkce *mysql_result* na *mysqli_num_rows* (případně pokud je voláno v rámci aktualizace Rubicon upravuje *pg_result* na *pg_num_rows* (použito stejně, jen jiná DB)).
1. [MysqliQueries](Mona/UpgradeExtensions/MysqliQueries.cs): Po nahrazení hledá *„$this->db“* a upraví funkce mysqli s proměnnou *$beta*.
1. [CloseIndex](Mona/UpgradeExtensions/CloseIndex.cs): Přidá mysqli_close nebo pg_close na konec soubor index.php.
1. [Anketa](Mona/UpgradeExtensions/Anketa.cs): Úprava souboru *anketa/anketa.php*. Odmaže ``../`` v ``include_once`` výrazech.
1. [ClanekVypis](Mona/UpgradeExtensions/ClanekVypis.cs): Úprava souborů *system/clanek.php* a *system/vypis.php*. Přidává kód ``$p_sf = array();`` nad určitou podmínku.
1. [Chdir](Mona/UpgradeExtensions/Chdir.cs): Zakomentuje řádky s funckí ``chdir`` v souboru *admin/funkce/vytvoreni_adr.php*.
1. [TableXAddEdit](Mona/UpgradeExtensions/TableXAddEdit.cs): Potlačení chybové hlášky znakem „@“ v souborech *admin/table_x_add.php* a *admin/table_x_edit.php*.
1. [Strankovani](Mona/UpgradeExtensions/Strankovani.cs): Úprava variant funkce ``predchozi_dalsi`` v souboru *funkce/strankovani.php*.
1. [XmlFeeds](Mona/UpgradeExtensions/XmlFeeds.cs): Úprava podmínky v souborech *xml_feeds_*.
1. [SitemapSave](Mona/UpgradeExtensions/SitemapSave.cs): Přidání podmínky nad určitý cyklus v souboru *admin/sitemap_save.php*.
1. [GlobalBeta](Mona/UpgradeExtensions/GlobalBeta.cs): Přidání **``global $beta;``** do kódu funkce, která má v sobě mysqli dotaz na databázi.
1. [RenameVariable](Mona/UpgradeExtensions/RenameVariable.cs): Metody na přejmenování proměnné v souboru. Aktualizační procedura RS Mona volá ``RenameBeta`` (může být nastaveno parametrem programu ``--beta``, pokud je to u upravovaného webu vyžadováno), která přejmenuje proměnnou *$beta*.
1. [FloatExplodeConversions](Mona/UpgradeExtensions/FloatExplodeConversions.cs): Nalezeno pomocí nástroje [PHPStan](https://github.com/phpstan/phpstan). Nastává případ, kdy funkce ``explode`` očekává parametr typu string, ale dostává float. Aktualizovaný způsob použití pouze rozdělí string reprezentaci float na celou a desetinnou část a pracuje pouze s celou částí. Stejného výsledku lze docílit přetypováním na int.
1. [Unlink](Mona/UpgradeExtensions/Unlink.cs): Úprava ``unlink >>> @unlink``, která se neprovádí v kódu externích aplikací (*tiny_mce*, *swiper*, *fancybox*, *piwika*).
1. [RegexFunctions](Mona/UpgradeExtensions/RegexFunctions.cs): Úprava funkcí ``ereg``, ``eregi``, ``ereg_replace`` a ``split`` na příslušné moderní alternativy **``preg_match``**, **``preg_replace``** a **``preg_split``**.
1. [TrailingWhiteSpace](Mona/UpgradeExtensions/TrailingWhiteSpace.cs): Na doporučení nástroje **PHPStan** jsou z konce souboru odstraněny nepotřebné bílé znaky (mezery, prázdné řádky, ...).
1. [IfEmpty](Mona/UpgradeExtensions/IfEmpty.cs): Na doporučení nástroje **PHPStan** upravena podmínka, kdy pravá strana || je vždy nepravda. Ve skutešnosti stačí nahradit voláním vestavěné funkce ``empty``.
1. [GetMagicQuotesGpc](Mona/UpgradeExtensions/GetMagicQuotesGpc.cs): Funkce [``get_magic_quotes_gpc()``](https://www.php.net/manual/en/function.get-magic-quotes-gpc) je zastaralá a nepodporovaná novými verzemi PHP (ve verzi 7 navíc vždy vrací false). Tato funkce se obvykle používá v podmínkách - nechat pouze větev else (nebo jaká část vede na výsledek false), zbytek zakomentovat.
1. [WhileListEach](Mona/UpgradeExtensions/WhileListEach.cs): Funkce [``each``](https://www.php.net/manual/en/function.each) je zastaralá (v PHP 8 je navíc odstraněna). Kód ``reset(...);...while(list(...)=each(...))`` je nahrazen za ``foreach(...)``.
1. [CreateFunction](Mona/UpgradeExtensions/CreateFunction.cs): Funkce [``create_function``](https://www.php.net/manual/en/function.create-function.php) je zastaralá (v PHP 8 také odstraněna). Je možné nahradit za [anonymní funkce](https://www.php.net/manual/en/functions.anonymous.php), kdy je kód ``create_function('args', 'code')`` nahrazen za **``function (args) { code }``**. Jelikož je kód uložen ve stringu, může vzniknout chyba, proto je při nahrazení *create_function* během výpisu zobrazeno varování do konzole.

---
# [Rubicon](Rubicon/RubiconUpgrader.cs)
Třída **``RubiconUpgrader``** rozšiřuje funkcionalitu ``MonaUpgrader`` přetížením metody ``UpgradeProcedure``. Kdy po dokončení volání bázové metody (*UpgradeProcedure* pro RS Mona, která díky tomu, že je volána pro Rubicon, má v některých metodách jiné chování již dříve) volá další rozšiřující metody statických tříd specifické pouze pro systém Rubicon:

1. [ObjectClass](Rubicon/UpgradeExtensions/ObjectClass.cs): Název 'Object' nelze v novějších verzích PHP použít jako název třídy, protože je to rezervované slovo. Proto pokud existuje soubor *classes/Object.php* je tento soubor i všechny reference na třídu ``Object`` budou přejmenovány na **``ObjectBase``**.
1. [ClassConstructors](Rubicon/UpgradeExtensions/ClassConstructors.cs): Způsob zápisu konstruktou jako metody se stejným názvem jako její třída nejsou konstruktory v novějších verzích PHP. Nahrazení za funkci **``__construct``**. Dále přidání funkce se stejným názvem jako třída, který volá ``self::__construct``, kvůli zpětné kompatibilitě.
1. [ScriptLanguagePhp](Rubicon/UpgradeExtensions/ScriptLanguagePhp.cs): HTML značka ``<script language="PHP"> ... </script>`` je zastaralá. Nahrazení za ``<?php ... ?>``.
1. [IncludesInHtmlComments](Rubicon/UpgradeExtensions/IncludesInHtmlComments.cs): Soubory *templates/.../product_detail.php* obsahují kód ``<?php include...`` v HTML blokovém komentáři. Tento PHP kód je spuštěn a vkládané soubory mohou způsobovat chyby. Zakomentuje v PHP - ``<?php //include...``.
1. [AegisxDetail](Rubicon/UpgradeExtensions/AegisxDetail.cs): Soubor *aegisx\detail.php* obsahuje ``break`` mimo cyklus nebo switch (záměrem je ukončit vykonávaný skript), nahradit za **``return``**.
1. [AegisxImportLoadData](Rubicon/UpgradeExtensions/AegisxImportLoadData.cs): Úprava mysql a proměnné $beta v souboru *aegisx/import/load_data.php*.
1. [AegisxHomeTopProducts](Rubicon/UpgradeExtensions/AegisxHomeTopProducts.cs): Úprava SQL dotazu na top produkty v souboru *aegisx/home.php*.
1. [UrlPromenne](Rubicon/UpgradeExtensions/UrlPromenne.cs): Opravuje chybně zapsanou proměnnou $modul v souboru *funkce/url_promenne.php*.
1. [DuplicateArrayKeys](Rubicon/UpgradeExtensions/DuplicateArrayKeys.cs): Na doporučení nástroje **PHPStan**, který nahlásil kód obsahující definici pole s duplicitními klíči. Odstranění dvojího výskytu procházením pole a ponecháním pouze posledního výskytu (chování, které by provedl interpret PHP, jen bez této zbytečné chybky).
1. [OldUnparsableAlmostEmptyFile](Rubicon/UpgradeExtensions/OldUnparsableAlmostEmptyFile.cs): Soubor *money/old/Compare_XML.php* je téměř prázdný a obsahuje kód, který neprojde PHP parserem a nedává smysl.
1. [HodnoceniConnect](Rubicon/UpgradeExtensions/HodnoceniConnect.cs): Soubory *pdf/p_listina.php* a *rss/hodnoceni.php* obsahují stejný kód využívající mysqli s proměnnou $beta_hod nebo $hodnoceni_conn.
1. [LibDbMysql](Rubicon/UpgradeExtensions/LibDbMysql.cs): Soubor *lib/db/mysql.inc.php* obsahuje podmínku, zda použít ``mysqli_connect`` nebo ``mysql_pconnect``. Jelikož je v nových verzích PHP možné použít pouze ``mysqli_connect``, tak může být podmínka zakomentována a rovnou použít správnou funkci.
1. [ArrayMissingKeyValue](Rubicon/UpgradeExtensions/ArrayMissingKeyValue.cs): Syntaktická chyba nalezena na webu hokejova-vyzbroj v souboru *rubicon/modules/search/main2.php*. Při inicializaci pole chybí hodnota pro klíč "darek_zdarma_info". Soubor se nepoužívá. Stačí zakomentovat.
1. [PiwikaLibsPearRaiseError](Rubicon/UpgradeExtensions/PiwikaLibsPearRaiseError.cs): V souboru *piwika/libs/PEAR.php* projít třídu **``PEAR``** a aktualizovat její (statickou?) metodu ``&raiseError``, aby neobsahovala referenci na ``$this``. Místo toho doplnit parametr **``$pear_object``** (inspirováno novější verzí této knihovny).
1. [RequiredParameterFollowsOptional](Rubicon/UpgradeExtensions/RequiredParameterFollowsOptional.cs): Na doporučení nástroje **PHPStan** jsou upraveny soubory jako *classes/McBalikovna.php*, kde funkce mají "volitelný" parametr před povinnými parametry. Tu nelze bez zadání hodnoty tohoto parametru volat, takže se chová jako povinný a stačí pouze odstranit jeho "přednastavenou" hodnotu.