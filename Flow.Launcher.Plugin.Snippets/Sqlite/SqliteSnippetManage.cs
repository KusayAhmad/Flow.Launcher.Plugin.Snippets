using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace Flow.Launcher.Plugin.Snippets.Sqlite;

public class SqliteSnippetManage : SnippetManage
{
    private const string TABLE_NAME = "snippets";


    private const string _tableDDL =
        $"create table {TABLE_NAME} (key varchar(200) not null primary key, value text not null, score int not null default 0, update_time datetime not null DEFAULT CURRENT_TIMESTAMP, create_time datetime not null DEFAULT CURRENT_TIMESTAMP)";

    private readonly string _connectionString;

    public SqliteSnippetManage(string dbDir)
    {
        var dbPath = Path.Combine(dbDir, TABLE_NAME + ".db");
        _connectionString = $"Data Source={dbPath};Version=3;";
        _initCheckTable();
    }

    private void _initCheckTable()
    {
        const string query = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{TABLE_NAME}';";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(query, connection);
        var result = command.ExecuteScalar();
        if (result != null) return;

        // create table
        using var createTableCommand = new SQLiteCommand(_tableDDL, connection);
        createTableCommand.ExecuteNonQuery();
    }

    private SnippetModel _readSnippetModel(SQLiteDataReader reader)
    {
        return new SnippetModel
        {
            Key = reader.GetString(0),
            Value = reader.GetString(1),
            Score = reader.GetInt32(2),
            UpdateTime = reader.GetDateTime(3)
        };
    }


    public SnippetModel GetByKey(string key)
    {
        const string sql = $"select * from {TABLE_NAME} where key = @key";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@key", key);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            return _readSnippetModel(reader);
        }

        return null;
    }

    public List<SnippetModel> List(string key = null, string value = null)
    {
        var sql = $"select * from {TABLE_NAME} where 1=1";
        if (key != null)
        {
            sql += " and key like @key";
        }

        if (value != null)
        {
            sql += " and value like @value";
        }

        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        if (key != null)
        {
            command.Parameters.AddWithValue("@key", $"%{key}%");
        }

        if (value != null)
        {
            command.Parameters.AddWithValue("@value", $"%{value}%");
        }

        using var reader = command.ExecuteReader();
        var result = new List<SnippetModel>();
        while (reader.Read())
        {
            result.Add(_readSnippetModel(reader));
        }

        return result;
    }

    public bool Add(SnippetModel sm)
    {
        const string sql =
            $"replace into {TABLE_NAME} (key, value, score, update_time) values (@key, @value, @score, @update_time)";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@key", sm.Key);
        command.Parameters.AddWithValue("@value", sm.Value);
        command.Parameters.AddWithValue("@score", sm.Score);
        command.Parameters.AddWithValue("@update_time", sm.UpdateTime ?? DateTime.Now);
        return command.ExecuteNonQuery() > 0;
    }

    public bool RemoveByKey(string key)
    {
        const string sql = $"delete from {TABLE_NAME} where key = @key";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@key", key);
        return command.ExecuteNonQuery() > 0;
    }

    public bool UpdateByKey(SnippetModel sm)
    {
        const string sql =
            $"update {TABLE_NAME} set value = @value, score = @score, update_time = @update_time where key = @key";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@key", sm.Key);
        command.Parameters.AddWithValue("@value", sm.Value);
        command.Parameters.AddWithValue("@score", sm.Score);
        command.Parameters.AddWithValue("@update_time", sm.UpdateTime ?? DateTime.Now);
        return command.ExecuteNonQuery() > 0;
    }

    public void Clear()
    {
        const string sql = $"delete from {TABLE_NAME}";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.ExecuteNonQuery();
    }
}