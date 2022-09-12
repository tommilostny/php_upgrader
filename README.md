# McRAI PHP upgrader tool (RS Mona, Rubicon)

Vytvořeno pro *McRAI* Tomášem Milostným jako nástroj aktualizace webových prezentací z PHP verze 5 na verzi 7.

Podrobný popis činnosti programu, viz **[src/PhpUpgrader/README.md](src/PhpUpgrader/README.md)**.

### Prerekvizity:

1. Pro možnost spuštění ze zdrojového kódu přes ``dotnet run`` je potřeba nainstalovat [runtime .NET 6.0](https://dotnet.microsoft.com/download).
1. Pro weby, které obsahují minifikovaný soubor **adminer.php** je potřeba nainstalovat [PHP 7.4](https://windows.php.net/download#php-7.4) a [composer](https://getcomposer.org/download/) (ověřit instalaci, je možné spustit příkaz **``composer -v``** v příkazové řádce). Vedle hlavního programu je spuštěn externí skript [PHP-CS-Fixer](https://github.com/FriendsOfPHP/PHP-CS-Fixer), který formátuje kód PHP do lépe upravitelné podoby.
1. Program se spouští přes příkazovou řádku (viz **příklady spuštění**). Nápovědu ke všem argumentům lze zobrazit spuštěním programu s argumentem ``--help``).

Název webu odpovídá složce v adresáři *"C:\McRAI\weby\"*, kde **C:\McRAI\\** je výchozím nastavením parametru *--base-folder* a **weby** je podadresářem *C:\McRAI\\*.

Archiv rovněž obsahuje složku "important", kterou je třeba umístit jako *"C:\McRAI\important"*.

Do složky *C:\McRAI\\* umístit soubor **ftp_logins.txt** (o formátu viz dále), který obsahuje přihlašovací údaje k FTP pro upravované weby.

Navrženo pro multiplatformní běh (*Windows* i *Unix*, testováno pouze na OS *Windows*). **Doporučeno** spustit v terminálu podporující *Unicode* (např. *Windows Terminal*).

---
## Postup práce s programem
Spuštění ze zdrojového kódu (``dotnet run``) nebo z poskytnutého binárního spustitelného souboru.

Zadání důležitých argumentů dle právě aktualizovaného webu (přidat **``--rubicon``**, pokud se jedná o Rubicon web). Hlavní je argument **``--web-name``**, který udává název složky webu v *"C:\McRAI\weby\"* a vyhledává se podle něj v *ftp_logins.txt*.

Využití **vestavěné podpory FTP** pomocí knihovny [WinSCP](https://winscp.net/eng/docs/library_install#nuget) (pro stahování a nahrávání již není potřeba využívat jiné manuální nástroje):
- Složka webu v *"C:\McRAI\weby\"*, se kterou se bude pracovat, **nemusí existovat** (pokud jsou k tomuto webu zadány údaje k FTP ve **ftp_logins.txt**). To automaticky stáhne ze serveru *mcrai1* pouze PHP soubory.
- Složka webu už existuje a chceme ji projít znovu. Program se zeptá, zda zkusit zkontrolovat vůči FTP *mcrai1*. Nebo můžeme zadat argument **``--check-ftp``**, pokud chceme aktualitu zkonrolovat, nebo **``--ignore-ftp``**, pokud chceme kontrolu přeskočit a pouze projít lokální soubory webu.
- Po aktualizaci se program může zeptat, jestli nahrát soubory na *mcrai-upgrade*, pokud nedošlo k chybě během aktualizace (např. nalezení použití dalších zastaralých funkcí ``mysql_``). Přímo lze obdobně povolit argumentem **``--upload``** nebo zakázat **``--dont-upload``**.

Program automaticky vytváří zálohy aktualizovaných souborů. Při opakovaném spuštění se zeptá, jestli tuto zálohu nahrát. Program pak aktualizuje soubory v původním stavu, jako byly na *mcrai1*. Zálohu lze načíst automaticky vždy zadáním argumentu **``--use-backup``** nebo ji přeskočit **``--ignore-backup``**.

Pokud je potřeba změnit údaje k databázi, použít argumenty **``--db``**, **``--user``**, **``--password``** a **``--hostname``** (všechny ve výchozím stavu prázdné a údaje zůstávají původní, kromě *hostname*, které je nastaveno na ``localhost``).

Dalši specifické argumenty, které nejsou pro většinu webů důležité, jsou popsány pod spuštěním programu s argumentem **``--help``**.

---
### Příklady spuštění:

- Zobrazení nápovědy:
  - ``dotnet run -- --help``
- Další složky s administrací, údaje k databázi nezměněné:
  - ``dotnet run -- --web-name kalimera-greece --admin-folders slozka1 slozka2 slozka3``
- Výchozí 1 nepřejmenovaná složka *admin* + nové údaje k databázi na serveru mcrai2:
  - ``dotnet run -- --web-name smluvniservis --db smluvniservis_n --user smluvniservis_u --password 'heslo'``
- Upgrade webu se systémem Rubicon:
  - ``dotnet run -- --rubicon --web-name olejemaziva --db olejemaziva_n --user olejemaziva_u --password 'heslo'``
- Načtení zálohovaných souborů bez nutnosti tázání uživatele programem (naopak lze použít ``--ignore-backup`` pro ignorování zálohy a použití aktuálních souborů webu):
  - ``dotnet run -c Release -- --rubicon --use-backup --web-name hokejova-vystroj``
- Kompletní proces: načtení zálohy, kontrola souborů na FTP původního serveru *mcrai* a také rovnou nahrání na *mcrai-upgrade*:
  - `` dotnet run -c Release -- --rubicon --use-backup --check-ftp --upload --web-name botaska``
- Spustit pouze aktualizaci lokálně, bez FTP:
  - `` dotnet run -c Release -- --rubicon --use-backup --ignore-ftp --dont-upload --web-name botaska``

Informace k ``dotnet run`` viz [https://docs.microsoft.com/cs-cz/dotnet/core/tools/dotnet-run](https://docs.microsoft.com/cs-cz/dotnet/core/tools/dotnet-run) (práce s ním a jak zadávat argumenty aplikace atd.).

---

# FTP update checker tool

Původně nástroj pro kontrolu nových souborů na FTP serveru po určitém datu (zůstává, pořád obsahuje *Program.cs*), nyní integrován do procesu **PHP upgraderu**.

Tento program se spouští přes příkazovou řádku (**dotnet cli** stejně jako PhpUpgrader) a pracuje s následujícími argumenty:
  - ``--username``, ``--password``, ``--host``, ``--path``, ``--year``, ``--month``, ``--day``, ``--base-folder`` a **``--web-name``** (pro podrobé informace spusťte s argumentem **``--help``**).

Pokud je zadán argument ``--web-name``, odpovídající složce v *C:\McRAI\weby\\* stejně jako u PhpUpgraderu, jako referenční datum bude použito datum vytvoření zadané složky.

## ftp_logins.txt

Nachází se ve složce zadané argumentem ``--base-folder`` (výchozí *C:\McRAI\ftp_logins.txt*).

Soubor využívaný k načtení uživatelského jména, hesla a složky na serveru dle zadaného argumentu **``--web-name``** (v souboru *ftp_logins.txt* se hledá řádek označený tímto názvem webu).

Formát řádku: **``web : jméno : heslo : složka_v_ftp_root``**

Přihlašovací údaje se používají pro připojení jak k *mcrai.vshosting.cz*, tak k *mcrai-upgrade.vshosting.cz*, proto pokud na těchto serverech zakládáme nové FTP přístupové údaje, je nutné aby měly stejné jméno a heslo (soubory webu by se pochopitelně měly nacházet na serveru ve stejné složce (např. *httpdocs*)).