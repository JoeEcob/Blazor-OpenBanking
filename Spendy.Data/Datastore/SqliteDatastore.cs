namespace Spendy.Data.Datastore
{
    using Microsoft.Data.Sqlite;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text.Json;

    /// <summary>
    /// SQLite-backed datastore using Microsoft.Data.Sqlite.
    /// Entities are stored as JSON blobs with a TEXT primary key (Guid).
    /// Each type gets its own table named after the type (e.g. "Transaction", "Account").
    ///
    /// NuGet: Microsoft.Data.Sqlite
    /// </summary>
    public class SqliteDatastore : IDatastore
    {
        private static readonly string DbPath = @"AppData/Spendy-Sqlite.db";

        private static SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Ensures a table exists for type <typeparamref name="T"/>.
        /// Table name matches the type name. Rows are stored as JSON with an integer PK.
        /// </summary>
        private static void EnsureTable<T>(SqliteConnection connection)
        {
            var tableName = typeof(T).Name;
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"""
                CREATE TABLE IF NOT EXISTS "{tableName}" (
                    Id    TEXT PRIMARY KEY NOT NULL,
                    Data  TEXT NOT NULL
                )
                """;
            cmd.ExecuteNonQuery();
        }

        private static List<T> ReadAll<T>(SqliteConnection connection)
        {
            var tableName = typeof(T).Name;
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"""SELECT Data FROM "{tableName}" """;
            using var reader = cmd.ExecuteReader();
            var results = new List<T>();
            while (reader.Read())
                results.Add(JsonSerializer.Deserialize<T>(reader.GetString(0))!);
            return results;
        }

        // -------------------------------------------------------------------------
        // Public API — implements IDatastore
        // -------------------------------------------------------------------------

        public T[] FindAll<T>()
        {
            using var connection = OpenConnection();
            EnsureTable<T>(connection);
            return ReadAll<T>(connection).ToArray();
        }

        /// <remarks>
        /// Filtering is performed in-process after fetching all rows, since LINQ
        /// expression predicates cannot be translated directly to SQL.
        /// </remarks>
        public T[] Find<T>(Expression<Func<T, bool>> predicate)
        {
            using var connection = OpenConnection();
            EnsureTable<T>(connection);
            return ReadAll<T>(connection).Where(predicate.Compile()).ToArray();
        }

        public T? FindOne<T>(Expression<Func<T, bool>> predicate)
        {
            using var connection = OpenConnection();
            EnsureTable<T>(connection);
            return ReadAll<T>(connection).FirstOrDefault(predicate.Compile());
        }

        /// <summary>
        /// Generates a new <see cref="Guid"/>, assigns it to the entity's Id property,
        /// inserts the entity, and returns the Guid.
        /// </summary>
        public Guid Insert<T>(T itemToInsert)
        {
            var id = Guid.NewGuid();

            // Write the Id back onto the entity if it exposes a settable Id property.
            var idProp = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
            if (idProp?.CanWrite == true)
                idProp.SetValue(itemToInsert, id);

            using var connection = OpenConnection();
            EnsureTable<T>(connection);
            var tableName = typeof(T).Name;

            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"""INSERT INTO "{tableName}" (Id, Data) VALUES ($id, $data)""";
            cmd.Parameters.AddWithValue("$id", id.ToString());
            cmd.Parameters.AddWithValue("$data", JsonSerializer.Serialize(itemToInsert));
            cmd.ExecuteNonQuery();

            return id;
        }

        public void InsertMany<T>(T[] itemsToInsert)
        {
            var idProp = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);

            using var connection = OpenConnection();
            EnsureTable<T>(connection);
            var tableName = typeof(T).Name;

            using var transaction = connection.BeginTransaction();
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = $"""INSERT INTO "{tableName}" (Id, Data) VALUES ($id, $data)""";
            var idParam = cmd.Parameters.Add("$id", SqliteType.Text);
            var dataParam = cmd.Parameters.Add("$data", SqliteType.Text);

            foreach (var item in itemsToInsert)
            {
                var id = Guid.NewGuid();
                if (idProp?.CanWrite == true)
                    idProp.SetValue(item, id);

                idParam.Value = id.ToString();
                dataParam.Value = JsonSerializer.Serialize(item);
                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        public void Update<T>(Guid id, T itemToUpdate)
        {
            using var connection = OpenConnection();
            EnsureTable<T>(connection);
            var tableName = typeof(T).Name;

            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"""UPDATE "{tableName}" SET Data = $data WHERE Id = $id""";
            cmd.Parameters.AddWithValue("$data", JsonSerializer.Serialize(itemToUpdate));
            cmd.Parameters.AddWithValue("$id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        public void DeleteMany<T>(Expression<Func<T, bool>> predicate)
        {
            using var connection = OpenConnection();
            EnsureTable<T>(connection);
            var tableName = typeof(T).Name;

            using var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = $"""SELECT Id, Data FROM "{tableName}" """;
            using var reader = selectCmd.ExecuteReader();

            var toDelete = new List<string>();
            var compiled = predicate.Compile();
            while (reader.Read())
            {
                var item = JsonSerializer.Deserialize<T>(reader.GetString(1))!;
                if (compiled(item))
                    toDelete.Add(reader.GetString(0));
            }
            reader.Close();

            if (toDelete.Count == 0) return;

            using var transaction = connection.BeginTransaction();
            using var deleteCmd = connection.CreateCommand();
            deleteCmd.Transaction = transaction;
            deleteCmd.CommandText = $"""DELETE FROM "{tableName}" WHERE Id = $id""";
            var param = deleteCmd.Parameters.Add("$id", SqliteType.Text);

            foreach (var id in toDelete)
            {
                param.Value = id;
                deleteCmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }
}
