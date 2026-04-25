using Microsoft.Data.Sqlite;

public class Db
{
    private string _conn;

    public Db(string conn)
    {
        _conn = conn;
    }

    public void Init()
    {
        using var con = new SqliteConnection(_conn);
        con.Open();

        var cmd = con.CreateCommand();
        cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS Users(Id INTEGER);
        CREATE TABLE IF NOT EXISTS Sources(Id INTEGER PRIMARY KEY AUTOINCREMENT, Channel TEXT, Category TEXT);
        ";

        cmd.ExecuteNonQuery();

        // дефолтные каналы
        AddSource("https://t.me/s/sochi_today", "city");
        AddSource("https://t.me/s/sochi_news", "news");
    }

    public void AddUser(long id)
    {
        using var con = new SqliteConnection(_conn);
        con.Open();

        var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT INTO Users VALUES ($id)";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public List<long> GetUsers()
    {
        var list = new List<long>();

        using var con = new SqliteConnection(_conn);
        con.Open();

        var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Id FROM Users";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(reader.GetInt64(0));

        return list;
    }

    public void AddSource(string url, string cat)
    {
        using var con = new SqliteConnection(_conn);
        con.Open();

        var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT INTO Sources(Channel, Category) VALUES ($c, $cat)";
        cmd.Parameters.AddWithValue("$c", url);
        cmd.Parameters.AddWithValue("$cat", cat);
        cmd.ExecuteNonQuery();
    }

    public List<Source> GetSources()
    {
        var list = new List<Source>();

        using var con = new SqliteConnection(_conn);
        con.Open();

        var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Channel, Category FROM Sources";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Source
            {
                Channel = reader.GetString(0),
                Category = reader.GetString(1)
            });
        }

        return list;
    }
}