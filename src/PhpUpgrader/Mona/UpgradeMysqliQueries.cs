namespace PhpUpgrader.Mona;

public partial class MonaUpgrader
{
    /// <summary>
    /// po nahrazeni resp. preskupeni $beta hledat „$this->db“ a upravit mysqli na $beta
    /// (napr. mysqli_query($beta, "SET CHARACTER SET utf8", $this->db);
    /// predelat na mysqli_query($this->db, "SET CHARACTER SET utf8"); …. atd .. )
    /// </summary>
    public void UpgradeMysqliQueries(FileWrapper file)
    {
        const string thisDB = "$this->db";
        if (file.Content.Contains(thisDB))
        {
            file.Content.Replace($"mysqli_query($beta, \"SET CHARACTER SET utf8\", {thisDB});", $"mysqli_query({thisDB}, \"SET CHARACTER SET utf8\");");
            RenameVar(file.Content, thisDB);
        }
    }
}
