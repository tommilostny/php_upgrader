﻿using PhpUpgrader.Mona.UpgradeRoutines;

namespace PhpUpgrader.Tests;

public class UpgradeGlobalBetaTests : UnitTestWithOutputBase
{
    public UpgradeGlobalBetaTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("<?php\n\nfunction test()\n{\n\t$data = mysqli_query($beta, ...);\n}\n?>")]
    [InlineData("<?php\n\nfunction test() {\n\n\t$data = mysqli_query($beta, ...);\n}\n?>")]
    [InlineData("<?php\n\nfunction test() {\n\t$data = mysqli_query($beta, ...);\n}\n?>")]
    [InlineData("<?php\r\nfunction odkazy($tabulka, $id, $popisek, $adresa, $cesta, $sirka, $vyska, $file_odkazy_nahled, $max_velikost_odkazu, $odkazy_poradi, $adresa_nazev)\r\n  {// ([tabulka pro zapis odkazu],[id clanku ke kteremu patri odkaz],[text k adrese],[url adresa], [cesta kam se maji ulozit nahledy], [sirka nahledu], [vyska nahledu], [soubor nahledu], [maximalni velikost uploadovaneho souboru])\r\n    $end = mysqli_query($beta, \"SELECT max(id)+1 from \".$tabulka);\r\n    $pom = mysqli_fetch_row($end);\r\n    if($pom[0] == \"\") $id_odkazy = 1;\r\n    else $id_odkazy = $pom[0];  \r\n    \r\n    // vybrani jen zaznamu kde se bude menit poradi\r\n    $query_poradi_odkazy = \"SELECT * FROM \".$tabulka.\" where poradi >= \".$odkazy_poradi.\" and table_id=\".$id.\" ORDER BY poradi ASC\";\r\n    $data_poradi_odkazy = mysqli_query($beta, $query_poradi_odkazy) or die(mysqli_error($beta));\r\n    \r\n    $query = \"INSERT INTO \".$tabulka.\" VALUES ('\".$id_odkazy.\"','\".$id.\"','\".$adresa.\"','\".$popisek.\"','\".time().\"', '\".$odkazy_poradi.\"', '\".$adresa_nazev.\"')\";\r\n    mysqli_query($beta, $query) or die(\"<div class='radek_info info_odkazy'><span class='cervene'>Prenos dat se nezdařil !!! Kontaktujte prosím administrátora !!! (table_x_odkazy)</span></div>\");\r\n    echo \"<div class='radek_info info_odkazy'>Odkazy:<span class='zelene'> Data byla do databaze uložena v pořádku.</span></div>\";\r\n    \r\n    while ($row_data = mysqli_fetch_assoc($data_poradi_odkazy))\r\n      {\r\n        $poradi_new = $row_data[\"poradi\"] + 1;\r\n        $query_pom = \"UPDATE \".$tabulka.\" SET poradi='\".$poradi_new.\"' where id='\".$row_data[\"id\"].\"'\";\r\n        mysqli_query($beta, $query_pom) or die(\"<div class='radek_info info_odkazy'><span class='cervene'>Prenos dat se nezdařil !!! Kontaktujte prosím administrátora !!! (table_x_odkazy - poradi)</span></div>\");\r\n      }\r\n\r\n    \r\n    if(is_uploaded_file($file_odkazy_nahled[\"tmp_name\"]))\r\n      {\r\n        nahled($file_odkazy_nahled, $sirka, $vyska, $cesta, $id_odkazy, $max_velikost_odkazu, \"odkazy\");\r\n        // ([soubor], [sirka pro vytvoreni mini nahledu], [vyska pro vytvoreni mini nahledu], [cesta kam se ma soubor ulozit], [id zaznamu], [maximalni velikost uploadovaneho souboru]) \r\n      }\r\n  }\r\n?>")]
    [InlineData("function dalsi_foto($files_dalsi_foto, $i, $tabulka, $sirka_mini, $vyska_mini, $sirka_normal, $vyska_normal, $cesta, $id, $popisek, $max_velikost_dalsi_foto, $fotogalerie_poradi) \r\n  { // ([soubor], [cislo fotky], [tabulka pro zapis jpg souboru], [sirka pro vytvoreni mini nahledu], [vyska pro vytvoreni mini nahledu], [sirka pro vytvoreni normalni fotky], [vyska pro vytvoreni normalni fotky], [cesta kam se ma soubor ulozit], [id clanku ke kteremu patri fotky], [popisek fotky(array)], [maximalni velikost souboru])\r\n    // pomer sirka - vyska by mel zustat 1,33333 (160*120 rozmery pro mini nahled, standartni pomer 800*600 pro normalni foto)\r\n    if($files_dalsi_foto[\"size\"][$i] <= $max_velikost_dalsi_foto)\r\n      {\r\n        $typ = pathinfo($files_dalsi_foto[\"name\"][$i], PATHINFO_EXTENSION);\r\n        $typ = str_replace(\" \", \"\", $typ);\r\n        $typ = strtolower($typ);\r\n\r\n        $end_obr=mysqli_query($beta, \"SELECT max(id)+1 from \".$tabulka.\"\");\r\n        $pom_obr=mysqli_fetch_row($end_obr);\r\n        if($pom_obr[0]==\"\") $id_obr=1;\r\n        else $id_obr=$pom_obr[0];\r\n\r\n        if(($typ == \"jpg\")||($typ == \"jpeg\"))\r\n          {\r\n            list($width, $height) = getimagesize($files_dalsi_foto['tmp_name'][$i]);\r\n\r\n            $pomer = $width/$height;\r\n            $pomer_ramecku = $sirka_mini/$vyska_mini;\r\n            $pomer_ramecku_normal = $sirka_normal/$vyska_normal;\r\n\r\n            if($pomer > $pomer_ramecku)\r\n              {\r\n                if(($width >= $sirka_mini)&&($height < $vyska_mini))\r\n                  {\r\n                    $newwidth = $sirka_mini;\r\n                    $pom1 = $width - $sirka_mini;\r\n                    $pom2 = $pom1/$pomer;\r\n                    $newheight = $height - $pom2;\r\n                  }\r\n                elseif(($width >= $sirka_mini ) && ($height >= $vyska_mini))\r\n                  {\r\n                    $newwidth = $sirka_mini;\r\n                    $cislo_pom2 = $width/$sirka_mini;\r\n                    $newheight = $height/$cislo_pom2;\r\n                  }\r\n                elseif(($width < $sirka_mini ) && ($height < $vyska_mini))\r\n                  {\r\n                    $newwidth = $width;\r\n                    $newheight = $height;\r\n                  }\r\n              }\r\n            elseif($pomer < $pomer_ramecku)\r\n              {\r\n                if(($width < $sirka_mini ) && ($height >= $vyska_mini))\r\n                  {\r\n                    $newheight = $vyska_mini;\r\n                    $pom1 = $height - $vyska_mini;\r\n                    $pom2 = $pom1*$pomer;\r\n                    $newwidth = $width - $pom2;\r\n                  }\r\n                elseif(($width >= $sirka_mini ) && ($height >= $vyska_mini))\r\n                  {\r\n                    $newheight = $vyska_mini;\r\n                    $cislo_pom2 = $height/$vyska_mini;\r\n                    $newwidth = $width/$cislo_pom2;\r\n                  }\r\n                elseif(($width < $sirka_mini ) && ($height < $vyska_mini))\r\n                  {\r\n                    $newwidth = $width;\r\n                    $newheight = $height;\r\n                  }\r\n              }\r\n            elseif($pomer == $pomer_ramecku)\r\n              {\r\n                if(($width <= $sirka_mini)&&($height <= $vyska_mini))\r\n                  {\r\n                    $newwidth = $width;\r\n                    $newheight = $height;\r\n                  }\r\n                else\r\n                  {\r\n                    $newwidth = $sirka_mini;\r\n                    $newheight = $vyska_mini;\r\n                  }\r\n              }\r\n\r\n            if($pomer > $pomer_ramecku_normal)\r\n              {\r\n                if(($width >= $sirka_normal ) && ($height < $vyska_normal))\r\n                  {\r\n                    $newwidth2 = $sirka_normal;\r\n                    $pom1 = $width - $sirka_normal;\r\n                    $pom2 = $pom1/$pomer;\r\n                    $newheight2 = $height - $pom2;\r\n                  }\r\n                elseif(($width >= $sirka_normal ) && ($height >= $vyska_normal))\r\n                  {\r\n                    $newwidth2 = $sirka_normal;\r\n                    $cislo_pom2 = $width/$sirka_normal;\r\n                    $newheight2 = $height/$cislo_pom2;\r\n                  }\r\n                elseif(($width < $sirka_normal ) && ($height < $vyska_normal))\r\n                  {\r\n                    $newwidth2 = $width;\r\n                    $newheight2 = $height;\r\n                  }\r\n              }\r\n            elseif($pomer < $pomer_ramecku_normal)\r\n              {\r\n                if(($width < $sirka_normal ) && ($height >= $vyska_normal))\r\n                  {\r\n                    $newheight2 = $vyska_normal;\r\n                    $pom1 = $height - $vyska_normal;\r\n                    $pom2 = $pom1*$pomer;\r\n                    $newwidth2 = $width - $pom2;\r\n                  }\r\n                elseif(($width >= $sirka_normal ) && ($height >= $vyska_normal))\r\n                  {\r\n                    $newheight2 = $vyska_normal;\r\n                    $cislo_pom2 = $height/$vyska_normal;\r\n                    $newwidth2 = $width/$cislo_pom2;\r\n                  }\r\n                elseif(($width < $sirka_normal ) && ($height < $vyska_normal))\r\n                  {\r\n                    $newwidth2 = $width;\r\n                    $newheight2 = $height;\r\n                  }\r\n              }\r\n            elseif($pomer == $pomer_ramecku_normal)\r\n              {\r\n                if(($width <= $sirka_normal)&&($height <= $vyska_normal))\r\n                  {\r\n                    $newwidth2 = $width;\r\n                    $newheight2 = $height;\r\n                  }\r\n                else\r\n                  {\r\n                    $newwidth2 = $sirka_normal;\r\n                    $newheight2 = $vyska_normal;\r\n                  }\r\n              }\r\n\r\n            $thumb=imagecreatetruecolor($newwidth, $newheight); // imagecreatetruecolor - vytvoreni nahledu o novych velikostech. Pouze cerny\r\n            $thumb2=imagecreatetruecolor($newwidth2, $newheight2);\r\n\r\n            if((!$thumb)||(!$thumb2))\r\n              {\r\n                echo \"<div class='radek_info info_foto'>Fotogalerie:\";\r\n                echo \"<span class='cervene'> Nepodařilo se vytvořit nový obrázek !!!</span>\";\r\n                echo \"<span class='fw_normal'>Problém je na naší straně<br />Kontaktujte prosím administrátora</span>\";\r\n                echo \"</div>\";\r\n              }\r\n\r\n            $source = imagecreatefromjpeg($files_dalsi_foto['tmp_name'][$i]); // vytvoreni zdroj pro dalsi upravy\r\n            $source2 = imagecreatefromjpeg($files_dalsi_foto['tmp_name'][$i]);\r\n\r\n            if((!$source)||(!$source2))\r\n              {\r\n                echo \"<div class='radek_info info_foto'>Fotogalerie:\";\r\n                echo \"<span class='cervene'> Nepodařilo se upravit originální obrázek !!!</span>\";\r\n                echo \"<span class='fw_normal'>Problém je na naší straně<br />Kontaktujte prosím administrátora</span>\";\r\n                echo \"</div>\";\r\n              }\r\n\r\n            imagecopyresampled($thumb, $source, 0, 0, 0, 0, $newwidth, $newheight, $width, $height); // dokonceni obrazku\r\n            imagejpeg($thumb,$cesta.\"\".$id_obr.\"mini.jpg\", 90); // ulozeni\r\n            imagedestroy($thumb);\r\n            imagedestroy($source);\r\n\r\n            imagecopyresampled($thumb2, $source2, 0, 0, 0, 0, $newwidth2, $newheight2, $width, $height);\r\n            imagejpeg($thumb2,$cesta.\"\".$id_obr.\"normal.jpg\", 100);\r\n            imagedestroy($thumb2);\r\n            imagedestroy($source2);\r\n\r\n            // vybrani jen zaznamu kde se bude menit poradi\r\n            $query_poradi_fotogalerie = \"SELECT * FROM \".$tabulka.\" where poradi >= \".$fotogalerie_poradi.\" and table_id=\".$id.\" ORDER BY poradi ASC\";\r\n            $data_poradi_fotogalerie = mysqli_query($beta, $query_poradi_fotogalerie) or die(mysqli_error($beta));\r\n            \r\n            $query_obr = \"INSERT INTO \".$tabulka.\" VALUES ('\".$id_obr.\"','\".$id.\"','\".$popisek.\"','\".time().\"','\".$id_obr.\"mini.jpg-$-\".$id_obr.\"normal.jpg\".\"', '\".$fotogalerie_poradi.\"')\";\r\n            mysqli_query($beta, $query_obr) or die(\"<div class='radek_info info_foto'><span class='cervene'>Prenos dat se nezdařil !!! Kontaktujte prosím administrátora !!! (table_x_fotogalerie)</span></div>\");\r\n            echo \"<div class='radek_info info_foto'>Fotogalerie:<span class='zelene'> Soubor \".$files_dalsi_foto['name'][$i].\" byl nahrán.</span></div>\";\r\n\r\n            \r\n            while ($row_data = mysqli_fetch_assoc($data_poradi_fotogalerie))\r\n              {\r\n                $poradi_new = $row_data[\"poradi\"] + 1;\r\n                $query_pom = \"UPDATE \".$tabulka.\" SET poradi='\".$poradi_new.\"' where id='\".$row_data[\"id\"].\"'\";\r\n                mysqli_query($beta, $query_pom) or die(\"<div class='radek_info info_foto'><span class='cervene'>Prenos dat se nezdařil !!! Kontaktujte prosím administrátora !!! (table_x_fotogalerie - poradi)</span></div>\");\r\n              }\r\n          }\r\n        else\r\n          {\r\n            echo \"<div class='radek_info info_foto'>Fotogalerie:\";\r\n            echo \"<span class='cervene'> Obrázek \".$files_dalsi_foto['name'][$i].\" nebyl nahrán !!!</span>\";\r\n            echo \"<span class='fw_normal'>Zadali jste nesprávný formát obrázku, dovoleny jsou formáty jpg, jpeg !</span>\";\r\n            echo \"<span class='fw_normal'>Byl zadán formát typu - \".$typ.\"</span>\";\r\n            echo \"</div>\";\r\n          }\r\n      }\r\n    else\r\n      {\r\n        echo \"<div class='radek_info info_foto'>Fotogalerie:\";\r\n        echo \"<span class='cervene'> Soubor nebyl nahrán !!!</span>\";\r\n        echo \"<span class='fw_normal'>Zadali jste příliš velký soubor, maximální velikost souboru je \".($max_velikost_dalsi_foto/1000000).\" MB !</span>\";\r\n        echo \"<span class='fw_normal'>Velikost zadaného souboru - \".($files_dalsi_foto[\"size\"][$i]/1000000).\" MB</span>\";\r\n        echo \"</div>\";\r\n      }\r\n  }")]
    public void UpgradesValidFile(string content)
    {
        //Arrange
        var file = new FileWrapper("somefile.php", content);

        //Act
        file.UpgradeGlobalBeta();

        //Assert
        var updatedContent = file.Content.ToString();
        _output.WriteLine(file.Path);
        _output.WriteLine(updatedContent);
        Assert.True(file.IsModified);
        Assert.NotEqual(content, updatedContent);
        Assert.Contains("global $beta;", updatedContent);
    }

    [Theory]
    [InlineData("<?php\nif($query_text_all !== FALSE) {\n\twhile($data_stranky_text_all = mysqli_fetch_array($query_text_all))\n}\n?>")]
    [InlineData("<?php\n\n$beta = mysqli_connect(...)\n\n?>")]
    [InlineData("<?php\n\nfunction test() {\n\tglobal $beta;\n\t$data = mysqli_query($beta, ...);\n}\n?>")]
    [InlineData("<?php\n\nfunction test() {\n\t$data = mysqli_query($this->link, ...);\n}\n?>")]
    [InlineData("<?php\n\nfunction test()\n{\n\t$data = other_function($varecka, ...);\n}\n?>")]
    [InlineData("<?php\r\nfunction hl_foto($files_hl_foto, $sirka_mini, $vyska_mini, $sirka_middle, $vyska_middle, $sirka_normal, $vyska_normal, $cesta, $id, $max_velikost_hl_foto) \r\n  { \r\n    // ([soubor], [sirka pro vytvoreni mini nahledu], [vyska pro vytvoreni mini nahledu], [sirka pro vytvoreni middle nahledu], [vyska pro vytvoreni middle nahledu], [sirka pro vytvoreni normalni fotky], [vyska pro vytvoreni normalni fotky], [cesta kam se ma soubor ulozit], [id zaznamu], [maximalni velikost souboru]) \r\n    // pomer sirka - vyska by mel zustat 1,33333 (160*120 rozmery pro mini nahled, standartni pomer 800*600 pro normalni foto)\r\n    if($files_hl_foto[\"size\"] <= $max_velikost_hl_foto)\r\n      {\r\n        $typ = pathinfo($files_hl_foto[\"name\"], PATHINFO_EXTENSION);\r\n        $typ = str_replace(\" \", \"\", $typ);\r\n        $typ = strtolower($typ);\r\n\r\n        if(($typ == \"jpg\")||($typ == \"jpeg\"))\r\n          {\r\n            list($width, $height) = getimagesize($files_hl_foto['tmp_name']);\r\n\r\n            $pomer = $width/$height;\r\n            $pomer_ramecku = $sirka_mini/$vyska_mini;\r\n            $pomer_ramecku_middle = $sirka_middle/$vyska_middle;\r\n            $pomer_ramecku_normal = $sirka_normal/$vyska_normal;\r\n            \r\n            if($pomer > $pomer_ramecku)\r\n              {\r\n                if(($width >= $sirka_mini)&&($height < $vyska_mini))\r\n                  {\r\n                    $newwidth = $sirka_mini;\r\n                    $pom1 = $width - $sirka_mini;\r\n                    $pom2 = $pom1/$pomer;\r\n                    $newheight = $height - $pom2;\r\n                  }\r\n                elseif(($width >= $sirka_mini ) && ($height >= $vyska_mini))\r\n                  {\r\n                    $newwidth = $sirka_mini;\r\n                    $cislo_pom2 = $width/$sirka_mini;\r\n                    $newheight = $height/$cislo_pom2;\r\n                  }\r\n                elseif(($width < $sirka_mini ) && ($height < $vyska_mini))\r\n                  {\r\n                    $newwidth = $width;\r\n                    $newheight = $height;\r\n                  }\r\n              }\r\n            elseif($pomer < $pomer_ramecku)\r\n              {\r\n                if(($width < $sirka_mini ) && ($height >= $vyska_mini))\r\n                  {\r\n                    $newheight = $vyska_mini;\r\n                    $pom1 = $height - $vyska_mini;\r\n                    $pom2 = $pom1*$pomer;\r\n                    $newwidth = $width - $pom2;\r\n                  }\r\n                elseif(($width >= $sirka_mini ) && ($height >= $vyska_mini))\r\n                  {\r\n                    $newheight = $vyska_mini;\r\n                    $cislo_pom2 = $height/$vyska_mini;\r\n                    $newwidth = $width/$cislo_pom2;\r\n                  }\r\n                elseif(($width < $sirka_mini ) && ($height < $vyska_mini))\r\n                  {\r\n                    $newwidth = $width;\r\n                    $newheight = $height;\r\n                  }\r\n              }\r\n            elseif($pomer == $pomer_ramecku)\r\n              {\r\n                if(($width <= $sirka_mini)&&($height <= $vyska_mini))\r\n                  {\r\n                    $newwidth = $width;\r\n                    $newheight = $height;\r\n                  }\r\n                else\r\n                  {\r\n                    $newwidth = $sirka_mini;\r\n                    $newheight = $vyska_mini;\r\n                  }\r\n              }\r\n\r\n            if($pomer > $pomer_ramecku_normal)\r\n              {\r\n                if(($width >= $sirka_normal ) && ($height < $vyska_normal))\r\n                  {\r\n                    $newwidth2 = $sirka_normal;\r\n                    $pom1 = $width - $sirka_normal;\r\n                    $pom2 = $pom1/$pomer;\r\n                    $newheight2 = $height - $pom2;\r\n                  }\r\n                elseif(($width >= $sirka_normal ) && ($height >= $vyska_normal))\r\n                  {\r\n                    $newwidth2 = $sirka_normal;\r\n                    $cislo_pom2 = $width/$sirka_normal;\r\n                    $newheight2 = $height/$cislo_pom2;\r\n                  }\r\n                elseif(($width < $sirka_normal ) && ($height < $vyska_normal))\r\n                  {\r\n                    $newwidth2 = $width;\r\n                    $newheight2 = $height;\r\n                  }\r\n              }\r\n            elseif($pomer < $pomer_ramecku_normal)\r\n              {\r\n                if(($width < $sirka_normal ) && ($height >= $vyska_normal))\r\n                  {\r\n                    $newheight2 = $vyska_normal;\r\n                    $pom1 = $height - $vyska_normal;\r\n                    $pom2 = $pom1*$pomer;\r\n                    $newwidth2 = $width - $pom2;\r\n                  }\r\n                elseif(($width >= $sirka_normal ) && ($height >= $vyska_normal))\r\n                  {\r\n                    $newheight2 = $vyska_normal;\r\n                    $cislo_pom2 = $height/$vyska_normal;\r\n                    $newwidth2 = $width/$cislo_pom2;\r\n                  }\r\n                elseif(($width < $sirka_normal ) && ($height < $vyska_normal))\r\n                  {\r\n                    $newwidth2 = $width;\r\n                    $newheight2 = $height;\r\n                  }\r\n              }\r\n            elseif($pomer == $pomer_ramecku_normal)\r\n              {\r\n                if(($width <= $sirka_normal)&&($height <= $vyska_normal))\r\n                  {\r\n                    $newwidth2 = $width;\r\n                    $newheight2 = $height;\r\n                  }\r\n                else\r\n                  {\r\n                    $newwidth2 = $sirka_normal;\r\n                    $newheight2 = $vyska_normal;\r\n                  }\r\n              }\r\n              \r\n            if($pomer > $pomer_ramecku_middle)\r\n              {\r\n                if(($width >= $sirka_middle ) && ($height < $vyska_middle))\r\n                  {\r\n                    $newwidth3 = $sirka_middle;\r\n                    $pom1 = $width - $sirka_middle;\r\n                    $pom2 = $pom1/$pomer;\r\n                    $newheight3 = $height - $pom2;\r\n                  }\r\n                elseif(($width >= $sirka_middle ) && ($height >= $vyska_middle))\r\n                  {\r\n                    $newwidth3 = $sirka_middle;\r\n                    $cislo_pom2 = $width/$sirka_middle;\r\n                    $newheight3 = $height/$cislo_pom2;\r\n                  }\r\n                elseif(($width < $sirka_middle ) && ($height < $vyska_middle))\r\n                  {\r\n                    $newwidth3 = $width;\r\n                    $newheight3 = $height;\r\n                  }\r\n              }\r\n            elseif($pomer < $pomer_ramecku_middle)\r\n              {\r\n                if(($width < $sirka_middle ) && ($height >= $vyska_middle))\r\n                  {\r\n                    $newheight3 = $vyska_middle;\r\n                    $pom1 = $height - $vyska_middle;\r\n                    $pom2 = $pom1*$pomer;\r\n                    $newwidth3 = $width - $pom2;\r\n                  }\r\n                elseif(($width >= $sirka_middle ) && ($height >= $vyska_middle))\r\n                  {\r\n                    $newheight3 = $vyska_middle;\r\n                    $cislo_pom2 = $height/$vyska_middle;\r\n                    $newwidth3 = $width/$cislo_pom2;\r\n                  }\r\n                elseif(($width < $sirka_middle ) && ($height < $vyska_middle))\r\n                  {\r\n                    $newwidth3 = $width;\r\n                    $newheight3 = $height;\r\n                  }\r\n              }\r\n            elseif($pomer == $pomer_ramecku_middle)\r\n              {\r\n                if(($width <= $sirka_middle)&&($height <= $vyska_middle))\r\n                  {\r\n                    $newwidth3 = $width;\r\n                    $newheight3 = $height;\r\n                  }\r\n                else\r\n                  {\r\n                    $newwidth3 = $sirka_middle;\r\n                    $newheight3 = $vyska_middle;\r\n                  }\r\n              }\r\n            \r\n            $thumb=imagecreatetruecolor($newwidth, $newheight); // imagecreatetruecolor - vytvoreni nahledu o novych velikostech. Pouze cerny\r\n\r\n            if(!$thumb)\r\n              {\r\n                echo \"<div class='radek_info info_hlfoto'>Hlavní foto:\";\r\n                echo \"<span class='cervene'> Nepodařilo se vytvořit nový obrázek (mini)!!!</span>\";\r\n                echo \"<span class='fw_normal'>Problém je na naší straně<br />Kontaktujte prosím administrátora</span>\";\r\n                echo \"</div>\";\r\n              }\r\n\r\n            $source = imagecreatefromjpeg($files_hl_foto['tmp_name']); // vytvoreni zdroj pro dalsi upravy\r\n\r\n            if(!$source)\r\n              {\r\n                echo \"<div class='radek_info info_hlfoto'>Hlavní foto:\";\r\n                echo \"<span class='cervene'> Nepodařilo se upravit originální obrázek (mini)!!!</span>\";\r\n                echo \"<span class='fw_normal'>Problém je na naší straně<br />Kontaktujte prosím administrátora</span>\";\r\n                echo \"</div>\";\r\n              }\r\n\r\n            imagecopyresampled($thumb, $source, 0, 0, 0, 0, $newwidth, $newheight, $width, $height); // dokonceni obrazku\r\n            $hl_foto_hotovo_mini = imagejpeg($thumb,$cesta.\"\".$id.\"mini.jpg\", 90); // ulozeni mini nahledu\r\n            imagedestroy($thumb);\r\n            imagedestroy($source);\r\n\r\n            $thumb2=imagecreatetruecolor($newwidth2, $newheight2);\r\n\r\n            if(!$thumb2)\r\n              {\r\n                echo \"<div class='radek_info info_hlfoto'>Hlavní foto:\";\r\n                echo \"<span class='cervene'> Nepodařilo se vytvořit nový obrázek (normal)!!!</span>\";\r\n                echo \"<span class='fw_normal'>Problém je na naší straně<br />Kontaktujte prosím administrátora</span>\";\r\n                echo \"</div>\";\r\n              }\r\n\r\n            $source2 = imagecreatefromjpeg($files_hl_foto['tmp_name']);\r\n\r\n            if(!$source2)\r\n              {\r\n                echo \"<div class='radek_info info_hlfoto'>Hlavní foto:\";\r\n                echo \"<span class='cervene'> Nepodařilo se upravit originální obrázek (normal)!!!</span>\";\r\n                echo \"<span class='fw_normal'>Problém je na naší straně<br />Kontaktujte prosím administrátora</span>\";\r\n                echo \"</div>\";\r\n              }\r\n\r\n            imagecopyresampled($thumb2, $source2, 0, 0, 0, 0, $newwidth2, $newheight2, $width, $height);\r\n            $hl_foto_hotovo_normal = imagejpeg($thumb2,$cesta.\"\".$id.\"normal.jpg\", 100); // ulozeni normalni fotky\r\n            imagedestroy($thumb2);\r\n            imagedestroy($source2);\r\n\r\n            $thumb3=imagecreatetruecolor($newwidth3, $newheight3);\r\n\r\n            if(!$thumb3)\r\n              {\r\n                echo \"<div class='radek_info info_hlfoto'>Hlavní foto:\";\r\n                echo \"<span class='cervene'> Nepodařilo se vytvořit nový obrázek (middle)!!!</span>\";\r\n                echo \"<span class='fw_normal'>Problém je na naší straně<br />Kontaktujte prosím administrátora</span>\";\r\n                echo \"</div>\";\r\n              }\r\n\r\n            $source3 = imagecreatefromjpeg($files_hl_foto['tmp_name']);\r\n\r\n            if(!$source3)\r\n              {\r\n                echo \"<div class='radek_info info_hlfoto'>Hlavní foto:\";\r\n                echo \"<span class='cervene'> Nepodařilo se upravit originální obrázek (middle)!!!</span>\";\r\n                echo \"<span class='fw_normal'>Problém je na naší straně<br />Kontaktujte prosím administrátora</span>\";\r\n                echo \"</div>\";\r\n              }\r\n\r\n            imagecopyresampled($thumb3, $source3, 0, 0, 0, 0, $newwidth3, $newheight3, $width, $height);\r\n            $hl_foto_hotovo_middle = imagejpeg($thumb3,$cesta.\"\".$id.\"middle.jpg\", 100); // ulozeni middle fotky\r\n            imagedestroy($thumb3);\r\n            imagedestroy($source3);\r\n\r\n            if((!$hl_foto_hotovo_mini)||(!$hl_foto_hotovo_normal)||(!$hl_foto_hotovo_middle))\r\n              {\r\n                echo \"<div class='radek_info info_hlfoto'>Hlavní foto:\";\r\n                echo \"<span class='cervene'> Nepodařilo se uložit originální obrázek (hlavní foto)!!!</span>\";\r\n                echo \"<span class='fw_normal'>Problém je na naší straně<br />Kontaktujte prosím administrátora</span>\";\r\n                echo \"</div>\";\r\n              }\r\n            else\r\n              {\r\n                echo \"<div class='radek_info info_hlfoto'>Hlavní foto: <span class='zelene'> Soubory byly uloženy v pořádku.</span></div>\"; //OK hlaska\r\n              }\r\n          }\r\n        else\r\n          {\r\n            echo \"<div class='radek_info info_hlfoto'>Hlavní foto:\";\r\n            echo \"<span class='cervene'> Obrázek nebyl nahrán !!!</span>\";\r\n            echo \"<span class='fw_normal'>Zadali jste nesprávný formát obrázku, dovoleny jsou formáty jpg, jpeg !</span>\";\r\n            echo \"<span class='fw_normal'>Byl zadán formát typu - \".$typ.\"</span>\";\r\n            echo \"</div>\";\r\n          }\r\n      }\r\n    else\r\n      {\r\n        echo \"<div class='radek_info info_hlfoto'>Hlavní foto:\";\r\n        echo \"<span class='cervene'> Soubor nebyl nahrán !!!</span>\";\r\n        echo \"<span class='fw_normal'>Zadali jste příliš velký soubor, maximální velikost souboru je \".($max_velikost_hl_foto/1000000).\" MB !</span>\";\r\n        echo \"<span class='fw_normal'>Velikost zadaného souboru - \".($files_hl_foto[\"size\"]/1000000).\" MB</span>\";\r\n        echo \"</div>\";\r\n      }\r\n  }")]
    public void DoesNotUpgradeInvalidFile(string content)
    {
        //Arrange
        var file = new FileWrapper("somefile.php", content);

        //Act
        file.UpgradeGlobalBeta();

        //Assert
        _output.WriteLine(file.Path);
        _output.WriteLine(file.Content.ToString());
        Assert.False(file.IsModified);
        Assert.Equal(content, file.Content.ToString());
    }
}
