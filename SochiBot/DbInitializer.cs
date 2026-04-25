using Microsoft.Data.Sqlite;

public static class DbInitializer
{
    static HashSet<string> sent = new();

    public static void Init(Db db)
    {
        db.Execute("CREATE TABLE IF NOT EXISTS Users(Id INTEGER PRIMARY KEY);");
        db.Execute("CREATE TABLE IF NOT EXISTS Posts(Id TEXT PRIMARY KEY);");
    }

    public static bool IsDuplicate(string id)
    {
        return sent.Contains(id);
    }

    public static void SavePost(string id)
    {
        sent.Add(id);

        using var con = new SqliteConnection("Data Source=bot.db");
        con.Open();

        var cmd = con.CreateCommand();
        cmd.CommandText = $"INSERT OR IGNORE INTO Posts(Id) VALUES('{id}')";
        cmd.ExecuteNonQuery();
    }
}