using Microsoft.Data.Sqlite;

public class Db
{
    private readonly string _connection = "Data Source=bot.db";

    public void Execute(string sql)
    {
        using var con = new SqliteConnection(_connection);
        con.Open();
        var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public List<long> GetUsers()
    {
        var list = new List<long>();

        using var con = new SqliteConnection(_connection);
        con.Open();

        var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Id FROM Users";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(reader.GetInt64(0));
        }

        return list;
    }

    public void AddUser(long id)
    {
        Execute($"INSERT OR IGNORE INTO Users(Id) VALUES({id})");
    }

    public void RemoveUser(long id)
    {
        Execute($"DELETE FROM Users WHERE Id = {id}");
    }
}
