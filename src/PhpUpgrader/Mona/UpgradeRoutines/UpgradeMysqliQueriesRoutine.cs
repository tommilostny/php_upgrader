namespace PhpUpgrader.Mona.UpgradeRoutines;

public static class UpgradeMysqliQueriesRoutine
{
    /// <summary>
    /// po nahrazeni resp. preskupeni $beta hledat „$this->db“ a upravit mysqli na $beta
    /// (napr. mysqli_query($beta, "SET CHARACTER SET utf8", $this->db);
    /// predelat na mysqli_query($this->db, "SET CHARACTER SET utf8"); …. atd .. )
    /// </summary>
    public static FileWrapper UpgradeMysqliQueries(this FileWrapper file, MonaUpgrader upgrader)
    {
        const string thisDB = "$this->db";
        if (file.Content.Contains(thisDB))
        {
            file.Content.Replace($"mysqli_query($beta, \"SET CHARACTER SET utf8\", {thisDB});", $"mysqli_query({thisDB}, \"SET CHARACTER SET utf8\");");
            upgrader.RenameVar(file.Content, thisDB);
        }
        return file;
    }
}
