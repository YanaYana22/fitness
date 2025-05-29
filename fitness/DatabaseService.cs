using Npgsql;
using System.Data;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string host, string db, string user, string password)
    {
        _connectionString = $"Host={host}; Database={db};Username={user};Password={password}";
    }

    public DataTable ExecuteQuery(string query)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var cmd = new NpgsqlCommand(query, connection);
        using var adapter = new NpgsqlDataAdapter(cmd);

        DataTable dt = new DataTable();
        adapter.Fill(dt);

        return dt;
    }

    public int ExecuteNonQuery(string command)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var cmd = new NpgsqlCommand(command, connection);
        return cmd.ExecuteNonQuery();
    }
}