namespace PhpUpgrader;

public readonly struct DbLoginParser
{
    public string Database { get; }

    public string UserName { get; }

    public string Password { get; }

    public bool Success { get; }

    public DbLoginParser(string baseFolder, string webName)
    {
        var login = File.ReadAllText(Path.Join(baseFolder, "db_logins.txt"))
            .Split('\n')
            .Select(line => line.Split(':'))
            .Select(items =>
            {
                for (var i = 0; i < items.Length; i++)
                {
                    items[i] = items[i].Trim();
                }
                return items;
            })
            .FirstOrDefault(items => string.Equals(items[0], webName, StringComparison.Ordinal));

        Success = login is not null;
        if (Success)
        {
            Database = login[1];
            UserName = login[2];
            Password = login[3];
        }
    }
}
