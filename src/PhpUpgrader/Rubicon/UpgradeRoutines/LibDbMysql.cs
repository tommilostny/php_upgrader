namespace PhpUpgrader.Rubicon.UpgradeRoutines;

public static class LibDbMysql
{
    public static FileWrapper UpgradeLibDbMysql(this FileWrapper file)
    {
        if (file.Path.EndsWith(Path.Join("lib", "db", "mysql.inc.php")))
        {
            //zakomentovat blok if else kolem mysqli_connect/mysql_pconnect
            //(vždycky se bude používat mysqli_connect).

            var ifIndex = file.Content.IndexOf("if($this->conn_type == 0){");
            file.Content.Insert(ifIndex, "//");

            var elseIfIndex = file.Content.IndexOf("}elseif($this->conn_type == 1){");
            file.Content.Insert(elseIfIndex, "//");

            var commentedCount = 0;
            for (var i = elseIfIndex; commentedCount < 2; i++)
            {
                if (file.Content[i] == '\n')
                {
                    while (char.IsWhiteSpace(file.Content[++i])) ;
                    file.Content.Insert(i, "//");
                    commentedCount++;
                }
            }
            //aktualizace select, insert_id, typ spojení = 0 (mysqli_connect), ne 1 (mysql_pconnect).
            file.Content.Replace("mysql_select_db($this->db_name)", "mysqli_select_db($this->sql_link, $this->db_name)")
                        .Replace("mysql_insert_id($qid)", "mysqli_insert_id($qid)")
                        .Replace("mysql_insert_id()", "mysqli_insert_id($this->sql_link)")
                        .Replace("var $conn_type = \"1\";", "var $conn_type = \"0\";");
        }
        return file;
    }
}
