# PHP upgrader tool (RS Mona, Rubicon)

Vytvořeno pro *McRAI* Tomášem Milostným jako nástroj aktualizace webových prezentací z PHP verze 5 na verzi 7.

### Nastavení a spuštění:

1. Ke spuštění je potřeba nainstalovat [.NET 5.0 SDK](https://dotnet.microsoft.com/download). 
2. Skript se spouští přes příkazovou řádku (viz **příklady spuštění**) a pracuje s následujícími argumenty:
  - **``--web-name``**, ``--admin-folders``, ``--base-folder``, ``--db``, ``--user``, ``--password``, ``--host``, ``--beta`` a ``--connection-file`` (pro podrobé informace spusťte s argumentem **``--help``**).
  - ``--rubicon`` přepíná upgrader do režimu pro Rubicon (pracuje pak s argumenty **``--web-name``**, ``--db``, ``--user``, ``--password``, ``--host``).

Název webu odpovídá složce v adresáři *"C:\McRAI\weby\"*, kde **C:\McRAI\\** je výchozím nastavením parametru *--base-folder* a **weby** je podadresářem *C:\McRAI\\*.

Archiv rovněž obsahuje složku "important", kterou je třeba umístit jako *"C:\McRAI\important"*.

Navrženo pro běh na OS *Windows* a **doporučeno** spusit v terminálu podporující *Unicode* (např. *Windows Terminal*).

---

### Příklady spuštění:

- Zobrazení nápovědy:
  - ``dotnet run -- --help``
- Další složky s administrací, údaje k databázi nezměněné:
  - ``dotnet run -- --web-name kalimera-greece --admin-folders slozka1 slozka2 slozka3``
- Výchozí 1 nepřejmenovaná složka *admin* + nové údaje k databázi na serveru mcrai2:
  - ``dotnet run -- --web-name smluvniservis --db smluvniservis_n --user smluvniservis_u --password 'heslo'``
- Uprade webu se systémem Rubicon:
  - ``dotnet run -- --rubicon --web-name olejemaziva --db olejemaziva_n --user olejemaziva_u --password 'heslo'``

Informace k ``dotnet run`` viz [https://docs.microsoft.com/cs-cz/dotnet/core/tools/dotnet-run](https://docs.microsoft.com/cs-cz/dotnet/core/tools/dotnet-run) (práce s ním a jak zadávat argumenty aplikace atd.).

---

# FTP update checker tool

Nástroj pro kontrolu nových souborů na FTP serveru po určitém datu.

Skript se spouští přes příkazovou řádku (**dotnet cli** stejně jako PhpUpgrader) a pracuje s následujícími argumenty:
  - **``--username``**, **``--password``**, ``--host``, ``--path``, ``--year``, ``--month``, ``--day``, ``--use-logins-file``, ``--base-folder`` a ``--web-name`` (pro podrobé informace spusťte s argumentem **``--help``**).

Pokud je zadán argument ``--web-name``, odpovídající složce v *C:\McRAI\weby\\* stejně jako u PhpUpgraderu, jako referenční datum bude použito datum vytvoření zadané složky.

### ftp_logins.txt

Soubor využívaný bezparametrovým argumentem **``--use-logins-file``** k načtení hesla dle jména zadaného argumentem *``--username``* nebo podle **``--web-name``** (nalezeno v *ftp_logins.txt* s prefixem **tom-**).

Nachází se ve složce zadané argumentem ``--base-folder`` (výchozí *C:\McRAI\ftp_logins.txt*).

Formát řádku: ``jméno : heslo : /složky_v_ftp_root,/oddělené,/čárkou``
