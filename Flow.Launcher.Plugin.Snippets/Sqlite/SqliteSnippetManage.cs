using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using Flow.Launcher.Plugin.Snippets.Util;

namespace Flow.Launcher.Plugin.Snippets.Sqlite;

public class SqliteSnippetManage : SnippetManage
{
    private const string TABLE_NAME = "snippets";


    private const string _tableDDL =
        $"create table {TABLE_NAME} (key varchar(200) not null primary key, value text not null, score int not null default 0, usage_count int not null default 0, last_used_time datetime, is_favorite int not null default 0, update_time datetime not null DEFAULT CURRENT_TIMESTAMP, create_time datetime not null DEFAULT CURRENT_TIMESTAMP)";

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
        if (result == null)
        {
            // create table
            using var createTableCommand = new SQLiteCommand(_tableDDL, connection);
            createTableCommand.ExecuteNonQuery();
        }
        else
        {
            // Migrate existing table by adding new columns if they don't exist
            _migrateTable(connection);
        }
    }

    private void _migrateTable(SQLiteConnection connection)
    {
        // Check and add usage_count column
        var checkUsageCount = $"PRAGMA table_info({TABLE_NAME})";
        using var cmd = new SQLiteCommand(checkUsageCount, connection);
        using var reader = cmd.ExecuteReader();
        
        bool hasUsageCount = false;
        bool hasLastUsedTime = false;
        bool hasIsFavorite = false;
        
        while (reader.Read())
        {
            var colName = reader.GetString(1);
            if (colName == "usage_count") hasUsageCount = true;
            if (colName == "last_used_time") hasLastUsedTime = true;
            if (colName == "is_favorite") hasIsFavorite = true;
        }
        reader.Close();
        
        if (!hasUsageCount)
        {
            using var addCol = new SQLiteCommand($"ALTER TABLE {TABLE_NAME} ADD COLUMN usage_count int not null default 0", connection);
            addCol.ExecuteNonQuery();
        }
        
        if (!hasLastUsedTime)
        {
            using var addCol = new SQLiteCommand($"ALTER TABLE {TABLE_NAME} ADD COLUMN last_used_time datetime", connection);
            addCol.ExecuteNonQuery();
        }
        
        if (!hasIsFavorite)
        {
            using var addCol = new SQLiteCommand($"ALTER TABLE {TABLE_NAME} ADD COLUMN is_favorite int not null default 0", connection);
            addCol.ExecuteNonQuery();
        }
    }

    private SnippetModel _readSnippetModel(SQLiteDataReader reader)
    {
        return new SnippetModel
        {
            Key = reader.GetString(0),
            Value = reader.GetString(1),
            Score = reader.GetInt32(2),
            UsageCount = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            LastUsedTime = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            IsFavorite = reader.IsDBNull(5) ? false : reader.GetInt32(5) == 1,
            UpdateTime = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
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

        sql += " order by score desc";

        InnerLogger.Logger.Info($"List: {sql}");

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
            $"replace into {TABLE_NAME} (key, value, score, usage_count, last_used_time, is_favorite, update_time) values (@key, @value, @score, @usage_count, @last_used_time, @is_favorite, @update_time)";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@key", sm.Key);
        command.Parameters.AddWithValue("@value", sm.Value);
        command.Parameters.AddWithValue("@score", sm.Score);
        command.Parameters.AddWithValue("@usage_count", sm.UsageCount);
        command.Parameters.AddWithValue("@last_used_time", (object)sm.LastUsedTime ?? DBNull.Value);
        command.Parameters.AddWithValue("@is_favorite", sm.IsFavorite ? 1 : 0);
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
        if (sm.Key == null)
        {
            return false;
        }

        var updateSqls = new List<string> 
        { 
            "score = @score",
            "usage_count = @usage_count",
            "last_used_time = @last_used_time",
            "is_favorite = @is_favorite"
        };
        if (sm.Value != null)
            updateSqls.Add("value = @value");

        var updateSql = string.Join(", ", updateSqls);
        var sql = $"update {TABLE_NAME} set {updateSql}, update_time = @update_time where key = @key";

        InnerLogger.Logger.Info($"UpdateByKey: {sql}");

        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@key", sm.Key);
        command.Parameters.AddWithValue("@update_time", sm.UpdateTime ?? DateTime.Now);
        command.Parameters.AddWithValue("@score", sm.Score);
        command.Parameters.AddWithValue("@usage_count", sm.UsageCount);
        command.Parameters.AddWithValue("@last_used_time", (object)sm.LastUsedTime ?? DBNull.Value);
        command.Parameters.AddWithValue("@is_favorite", sm.IsFavorite ? 1 : 0);
        if (sm.Value != null)
            command.Parameters.AddWithValue("@value", sm.Value);
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

    public void ResetAllScore()
    {
        const string sql = $"update {TABLE_NAME} set score = 0, usage_count = 0, last_used_time = NULL";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.ExecuteNonQuery();
    }
}