# RS Mona PHP upgrader tool

Vytvořeno pro *McRAI* Tomášem Milostným jako nástroj aktualizace webových prezentací z PHP verze 5 na verzi 7, běžící na redakčním systému Mona.

### Nastavení a spuštění:

Skript se spouští přes příkazovou řádku. Pracuje s následujícími argumenty:

- ``--web-name``, ``--admin-folders``, ``--base-folder``, ``--db``, ``--user``, ``--password`` a ``--host`` (pro podrobé informace spusťte s argumentem **``--help``**).

Název webu odpovídá složce v adresáři *"C:\McRAI\weby\"*, kde **C:\McRAI\** je výchozím nastavením parametru *--base-folder* a **weby** je podadresářem *C:\McRAI\*.

Archiv rovněž obsahuje složku "important", kterou je třeba umístit jako *"C:\McRAI\important"*.
