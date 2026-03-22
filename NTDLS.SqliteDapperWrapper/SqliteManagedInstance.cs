using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Text;

namespace NTDLS.SqliteDapperWrapper
{
    /// <summary>
    /// A disposable database connection wrapper that functions as an ephemeral instance.
    /// One instance of this class is generally created per query.
    /// </summary>
    public class SqliteManagedInstance : IDisposable
    {
        /// <summary>
        /// The directory in which the database resides.
        /// </summary>
        public string Directory { get; private set; }

        /// <summary>
        /// The native underlying SQLite connection.
        /// </summary>
        public SqliteConnection NativeConnection { get; private set; }

        /// <summary>
        /// Delegate used for ephemeral operations.
        /// </summary>
        public delegate void EphemeralProc(SqliteManagedInstance connection);

        /// <summary>
        /// Delegate used for ephemeral operations.
        /// </summary>
        public delegate T EphemeralProc<T>(SqliteManagedInstance connection);

        /// <summary>
        /// Creases a new instance of ManagedDataStorageInstance.
        /// </summary>
        public SqliteManagedInstance(string connectionString)
        {
            if(!connectionString.StartsWith("Data Source", StringComparison.InvariantCultureIgnoreCase))
            {
                connectionString = @$"Data Source={connectionString}";
            }

            NativeConnection = new SqliteConnection(connectionString);

            if (!SqlMapper.HasTypeHandler(typeof(GuidTypeHandler)))
                SqlMapper.AddTypeHandler(new GuidTypeHandler());

            if (!SqlMapper.HasTypeHandler(typeof(NullableGuidTypeHandler)))
                SqlMapper.AddTypeHandler(new NullableGuidTypeHandler());

            Directory = Path.GetFullPath(Path.GetDirectoryName(NativeConnection.DataSource) ?? string.Empty);

            NativeConnection.Open();
        }

        /// <summary>
        /// Begins an atomic transaction.
        /// </summary>
        public SqliteTransaction BeginTransaction()
            => NativeConnection.BeginTransaction();

        /// <summary>
        /// Begins an atomic transaction.
        /// </summary>
        public SqliteTransaction BeginTransaction(IsolationLevel isolationLevel)
            => NativeConnection.BeginTransaction(isolationLevel);

        /// <summary>
        /// Creates a temporary table with the given name from an enumerable set of strings. The strings are stored in the column Value.
        /// </summary>
        public DisposableValueListTable CreateTempTableFrom(string tableName, IEnumerable<string> values, SqliteTransaction transaction)
        {
            var result = new DisposableValueListTable(NativeConnection, tableName);

            using var createTempTableCommand = new SqliteCommand($"CREATE TEMP TABLE {tableName} (Value TEXT COLLATE NOCASE);", NativeConnection, transaction);
            createTempTableCommand.ExecuteNonQuery();

            using var insertTagCommand = new SqliteCommand($"INSERT INTO {tableName} (Value) VALUES (@Tag);", NativeConnection, transaction);

            foreach (var tag in values)
            {
                insertTagCommand.Parameters.Clear();
                insertTagCommand.Parameters.AddWithValue("@Tag", tag);
                insertTagCommand.ExecuteNonQuery();
            }

            return result;
        }

        /// <summary>
        /// Creates a temporary table with the given name from an enumerable set of strings. The strings are stored in the column Value.
        /// </summary>
        public DisposableValueListTable CreateTempTableFrom(string tableName, IEnumerable<string> values)
        {
            var result = new DisposableValueListTable(NativeConnection, tableName);

            using var transaction = NativeConnection.BeginTransaction();

            using var createTempTableCommand = new SqliteCommand($"CREATE TEMP TABLE {tableName} (Value TEXT COLLATE NOCASE);", NativeConnection, transaction);
            createTempTableCommand.ExecuteNonQuery();

            using var insertTagCommand = new SqliteCommand($"INSERT INTO {tableName} (Value) VALUES (@Tag);", NativeConnection, transaction);

            foreach (var tag in values)
            {
                insertTagCommand.Parameters.Clear();
                insertTagCommand.Parameters.AddWithValue("@Tag", tag);
                insertTagCommand.ExecuteNonQuery();
            }

            transaction.Commit();

            return result;
        }

        /// <summary>
        /// Creates a temporary table with the given name from an enumerable set of integers. The integers are stored in the column Value.
        /// </summary>
        public DisposableValueListTable CreateTempTableFrom(string tableName, IEnumerable<int> values)
        {
            var result = new DisposableValueListTable(NativeConnection, tableName);

            using var transaction = NativeConnection.BeginTransaction();

            using var createTempTableCommand = new SqliteCommand($"CREATE TEMP TABLE {tableName} (Value TEXT COLLATE NOCASE);", NativeConnection, transaction);
            createTempTableCommand.ExecuteNonQuery();

            using var insertTagCommand = new SqliteCommand($"INSERT INTO {tableName} (Value) VALUES (@Tag);", NativeConnection, transaction);

            foreach (var tag in values)
            {
                insertTagCommand.Parameters.Clear();
                insertTagCommand.Parameters.AddWithValue("@Tag", tag);
                insertTagCommand.ExecuteNonQuery();
            }

            transaction.Commit();

            return result;
        }

        /// <summary>
        /// Creates a temporary table with the given name from an enumerable set of values, the temp table schema matches the properties of the given objects.
        /// </summary>
        public DisposableValueListTable CreateTempTableFrom<T>(string tableName, IEnumerable<T> values)
        {
            var result = new DisposableValueListTable(NativeConnection, tableName);
            using var transaction = NativeConnection.BeginTransaction();

            // Use reflection to get property names and types of T
            var props = typeof(T).GetProperties();
            var columns = new StringBuilder();
            foreach (var prop in props)
            {
                // Creating columns for each property of the class
                // This example assumes all properties are of type string for simplicity
                // Adjust the type based on your specific needs or data types
                columns.Append($"{prop.Name} TEXT COLLATE NOCASE,");
            }

            // Remove the last comma
            columns.Length--;

            // Create the temporary table with dynamic columns
            using var createTempTableCommand = new SqliteCommand($"CREATE TEMP TABLE {tableName} ({columns});", NativeConnection, transaction);
            createTempTableCommand.ExecuteNonQuery();

            // Prepare the insert command
            var columnNames = new StringBuilder();
            var valuePlaceholders = new StringBuilder();
            foreach (var prop in props)
            {
                columnNames.Append($"{prop.Name},");
                valuePlaceholders.Append($"@{prop.Name},");
            }
            columnNames.Length--;
            valuePlaceholders.Length--;

            var insertCommandText = $"INSERT INTO {tableName} ({columnNames}) VALUES ({valuePlaceholders});";
            using var insertCommand = new SqliteCommand(insertCommandText, NativeConnection, transaction);

            // Insert all values
            foreach (var item in values)
            {
                insertCommand.Parameters.Clear();
                foreach (var prop in props)
                {
                    var val = prop.GetValue(item);
                    insertCommand.Parameters.AddWithValue($"@{prop.Name}", val ?? DBNull.Value); // Handle NULL values
                }
                insertCommand.ExecuteNonQuery();
            }

            transaction.Commit();
            return result;
        }

        /// <summary>
        /// Closes and disposes of the native SQLite connection.
        /// </summary>
        public void Dispose()
        {
            NativeConnection.Close();
            NativeConnection.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Attaches another SQLite database to the current native connection. The attached database is removed when this object is disposed.
        /// </summary>
        public DisposableAttachment Attach(string databaseFileName, string alias)
        {
            NativeConnection.Execute($"ATTACH DATABASE '{Directory}\\{databaseFileName}' AS {alias};");
            return new DisposableAttachment(NativeConnection, alias);
        }

        /// <summary>
        /// Attaches another SQLite database to the current native connection. The attached database is removed when this object is disposed.
        /// </summary>
        public async Task<DisposableAttachment> AttachAsync(string databaseFileName, string alias)
        {
            await NativeConnection.ExecuteAsync($"ATTACH DATABASE '{Directory}\\{databaseFileName}' AS {alias};");
            return new DisposableAttachment(NativeConnection, alias);
        }

        #region Native passthrough.

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the results.
        /// </summary>
        public List<T> Query<T>(string textOrScriptName)
            => NativeConnection.Query<T>(EmbeddedResource.Load(textOrScriptName), buffered: true).ToList();

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the results.
        /// </summary>
        public List<T> Query<T>(string textOrScriptName, object param)
            => NativeConnection.Query<T>(EmbeddedResource.Load(textOrScriptName), param, buffered: true).ToList();

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the scalar result.
        /// </summary>
        public T ExecuteScalar<T>(string textOrScriptName, T defaultValue)
            => NativeConnection.ExecuteScalar<T>(EmbeddedResource.Load(textOrScriptName)) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the scalar result.
        /// </summary>
        public T ExecuteScalar<T>(string textOrScriptName, object param, T defaultValue)
            => NativeConnection.ExecuteScalar<T>(EmbeddedResource.Load(textOrScriptName), param) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or throws an exception.
        /// </summary>
        public T QueryFirst<T>(string textOrScriptName)
            => NativeConnection.QueryFirst<T>(EmbeddedResource.Load(textOrScriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or throws an exception.
        /// </summary>
        public T QueryFirst<T>(string textOrScriptName, object param)
            => NativeConnection.QueryFirst<T>(EmbeddedResource.Load(textOrScriptName), param);

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or a default value.
        /// </summary>
        public T QueryFirstOrDefault<T>(string textOrScriptName, T defaultValue)
            => NativeConnection.QueryFirstOrDefault<T>(EmbeddedResource.Load(textOrScriptName)) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or a default value.
        /// </summary>
        public T QueryFirstOrDefault<T>(string textOrScriptName, object param, T defaultValue)
            => NativeConnection.QueryFirstOrDefault<T>(EmbeddedResource.Load(textOrScriptName), param) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or throws an exception.
        /// </summary>
        public T QuerySingle<T>(string textOrScriptName)
            => NativeConnection.QuerySingle<T>(EmbeddedResource.Load(textOrScriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or throws an exception.
        /// </summary>
        public T QuerySingle<T>(string textOrScriptName, object param)
            => NativeConnection.QuerySingle<T>(EmbeddedResource.Load(textOrScriptName), param);

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public T QuerySingleOrDefault<T>(string textOrScriptName, T defaultValue)
            => NativeConnection.QuerySingleOrDefault<T>(EmbeddedResource.Load(textOrScriptName)) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public T QuerySingleOrDefault<T>(string textOrScriptName, object param, T defaultValue)
            => NativeConnection.QuerySingleOrDefault<T>(EmbeddedResource.Load(textOrScriptName), param) ?? defaultValue;

        /// <summary>
        /// /// Queries the database using the given script name or SQL text and returns a scalar value throws an exception.
        /// </summary>
        public T? ExecuteScalar<T>(string textOrScriptName)
            => NativeConnection.ExecuteScalar<T>(EmbeddedResource.Load(textOrScriptName));

        /// <summary>
        /// /// Queries the database using the given script name or SQL text and returns a scalar value throws an exception.
        /// </summary>
        public T? ExecuteScalar<T>(string textOrScriptName, object param)
            => NativeConnection.ExecuteScalar<T>(EmbeddedResource.Load(textOrScriptName), param);

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or a default value.
        /// </summary>
        public T? QueryFirstOrDefault<T>(string textOrScriptName)
            => NativeConnection.QueryFirstOrDefault<T>(EmbeddedResource.Load(textOrScriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or a default value.
        /// </summary>
        public T? QueryFirstOrDefault<T>(string textOrScriptName, object param)
            => NativeConnection.QueryFirstOrDefault<T>(EmbeddedResource.Load(textOrScriptName), param);

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public T? QuerySingleOrDefault<T>(string textOrScriptName)
            => NativeConnection.QuerySingleOrDefault<T>(EmbeddedResource.Load(textOrScriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public T? QuerySingleOrDefault<T>(string textOrScriptName, object param)
            => NativeConnection.QuerySingleOrDefault<T>(EmbeddedResource.Load(textOrScriptName), param);

        /// <summary>
        /// Executes the given script name or SQL text on the database and does not return a result.
        /// </summary>
        public void Execute(string textOrScriptName)
            => NativeConnection.Execute(EmbeddedResource.Load(textOrScriptName));

        /// <summary>
        /// Executes the given script name or SQL text on the database and does not return a result.
        /// </summary>
        public void Execute(string textOrScriptName, object param)
            => NativeConnection.Execute(EmbeddedResource.Load(textOrScriptName), param);

        #endregion

        #region Native passthrough (async).

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the results.
        /// </summary>
        public async Task<List<T>> QueryAsync<T>(string textOrScriptName)
        {
            var result = await NativeConnection.QueryAsync<T>(EmbeddedResource.Load(textOrScriptName));
            return result.AsList();
        }

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the results.
        /// </summary>
        public async Task<List<T>> QueryAsync<T>(string textOrScriptName, object param)
        {
            var result = await NativeConnection.QueryAsync<T>(EmbeddedResource.Load(textOrScriptName), param);
            return result.AsList();
        }

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the scalar result.
        /// </summary>
        public async Task<T> ExecuteScalarAsync<T>(string textOrScriptName, T defaultValue)
            => await NativeConnection.ExecuteScalarAsync<T>(EmbeddedResource.Load(textOrScriptName)) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the scalar result.
        /// </summary>
        public async Task<T> ExecuteScalarAsync<T>(string textOrScriptName, object param, T defaultValue)
            => await NativeConnection.ExecuteScalarAsync<T>(EmbeddedResource.Load(textOrScriptName), param) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or throws an exception.
        /// </summary>
        public async Task<T> QueryFirstAsync<T>(string textOrScriptName)
            => await NativeConnection.QueryFirstAsync<T>(EmbeddedResource.Load(textOrScriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or throws an exception.
        /// </summary>
        public async Task<T> QueryFirstAsync<T>(string textOrScriptName, object param)
            => await NativeConnection.QueryFirstAsync<T>(EmbeddedResource.Load(textOrScriptName), param);

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or a default value.
        /// </summary>
        public async Task<T> QueryFirstOrDefaultAsync<T>(string textOrScriptName, T defaultValue)
            => await NativeConnection.QueryFirstOrDefaultAsync<T>(EmbeddedResource.Load(textOrScriptName)) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or a default value.
        /// </summary>
        public async Task<T> QueryFirstOrDefaultAsync<T>(string textOrScriptName, object param, T defaultValue)
            => await NativeConnection.QueryFirstOrDefaultAsync<T>(EmbeddedResource.Load(textOrScriptName), param) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or throws an exception.
        /// </summary>
        public async Task<T> QuerySingleAsync<T>(string textOrScriptName)
            => await NativeConnection.QuerySingleAsync<T>(EmbeddedResource.Load(textOrScriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or throws an exception.
        /// </summary>
        public async Task<T> QuerySingleAsync<T>(string textOrScriptName, object param)
            => await NativeConnection.QuerySingleAsync<T>(EmbeddedResource.Load(textOrScriptName), param);

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public async Task<T> QuerySingleOrDefaultAsync<T>(string textOrScriptName, T defaultValue)
            => await NativeConnection.QuerySingleOrDefaultAsync<T>(EmbeddedResource.Load(textOrScriptName)) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public async Task<T> QuerySingleOrDefaultAsync<T>(string textOrScriptName, object param, T defaultValue)
            => await NativeConnection.QuerySingleOrDefaultAsync<T>(EmbeddedResource.Load(textOrScriptName), param) ?? defaultValue;

        /// <summary>
        /// /// Queries the database using the given script name or SQL text and returns a scalar value throws an exception.
        /// </summary>
        public async Task<T?> ExecuteScalarAsync<T>(string textOrScriptName)
            => await NativeConnection.ExecuteScalarAsync<T>(EmbeddedResource.Load(textOrScriptName));

        /// <summary>
        /// /// Queries the database using the given script name or SQL text and returns a scalar value throws an exception.
        /// </summary>
        public async Task<T?> ExecuteScalarAsync<T>(string textOrScriptName, object param)
            => await NativeConnection.ExecuteScalarAsync<T>(EmbeddedResource.Load(textOrScriptName), param);

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or a default value.
        /// </summary>
        public async Task<T?> QueryFirstOrDefaultAsync<T>(string textOrScriptName)
            => await NativeConnection.QueryFirstOrDefaultAsync<T>(EmbeddedResource.Load(textOrScriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or a default value.
        /// </summary>
        public async Task<T?> QueryFirstOrDefaultAsync<T>(string textOrScriptName, object param)
            => await NativeConnection.QueryFirstOrDefaultAsync<T>(EmbeddedResource.Load(textOrScriptName), param);

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public async Task<T?> QuerySingleOrDefaultAsync<T>(string textOrScriptName)
            => await NativeConnection.QuerySingleOrDefaultAsync<T>(EmbeddedResource.Load(textOrScriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public async Task<T?> QuerySingleOrDefaultAsync<T>(string textOrScriptName, object param)
            => await NativeConnection.QuerySingleOrDefaultAsync<T>(EmbeddedResource.Load(textOrScriptName), param);

        /// <summary>
        /// Executes the given script name or SQL text on the database and does not return a result.
        /// </summary>
        public async Task ExecuteAsync(string textOrScriptName)
            => await NativeConnection.ExecuteAsync(EmbeddedResource.Load(textOrScriptName));

        /// <summary>
        /// Executes the given script name or SQL text on the database and does not return a result.
        /// </summary>
        public async Task ExecuteAsync(string textOrScriptName, object param)
            => await NativeConnection.ExecuteAsync(EmbeddedResource.Load(textOrScriptName), param);

        #endregion

        #region Schema info operations.

        /// <summary>
        /// Retrieves a collection of table metadata from the SQLite database.
        /// </summary>
        /// <remarks>This method queries the SQLite system table `sqlite_master` to retrieve metadata
        /// about all tables in the database. The returned collection includes information such as table names and
        /// schema details.</remarks>
        /// <returns>An <see cref="List{T}"/> containing <see cref="TableInfo"/> objects that represent the tables in the
        /// SQLite database. Returns an empty collection if no tables are found.</returns>
        public List<TableInfo> GetTables()
            => Query<TableInfo>("SELECT * FROM sqlite_master WHERE type = 'table'").ToList();

        /// <summary>
        /// Retrieves a collection of index information for the specified table in a SQLite database.
        /// </summary>
        /// <remarks>This method queries the SQLite system table `sqlite_master` to retrieve metadata
        /// about indexes associated with the specified table. The returned collection includes details such as the
        /// index name, type, associated table name, root page, and SQL definition.</remarks>
        /// <param name="tableName">The name of the table for which to retrieve index information.</param>
        /// <returns>An <see cref="List{T}"/> of <see cref="IndexInfo"/> objects, where each object represents an index
        /// associated with the specified table. Returns an empty collection if no indexes are found.</returns>
        public List<IndexInfo> GetIndexes(string tableName)
        {
            var model = Query<IndexInfoModel>("SELECT * FROM sqlite_master WHERE type = 'index' AND tbl_name = @TableName",
                new { TableName = tableName });

            return model.Select(o => new IndexInfo
            {
                Type = o.Type,
                Name = o.Name,
                TableName = o.Tbl_Name,
                RootPage = o.RootPage,
                SQL = o.SQL
            }).ToList();
        }

        /// <summary>
        /// Retrieves a collection of index information from the SQLite database.
        /// </summary>
        /// <remarks>This method queries the SQLite system table <c>sqlite_master</c> to retrieve
        /// information about all indexes defined in the database. The returned collection includes details such as the
        /// index name, associated table name, root page, and SQL definition.</remarks>
        /// <returns>An <see cref="List{T}"/> of <see cref="IndexInfo"/> objects, where each object represents metadata
        /// about an index in the SQLite database.</returns>
        public List<IndexInfo> GetIndexes()
        {
            var model = Query<IndexInfoModel>("SELECT * FROM sqlite_master WHERE type = 'index'");
            return model.Select(o => new IndexInfo
            {
                Type = o.Type,
                Name = o.Name,
                TableName = o.Tbl_Name,
                RootPage = o.RootPage,
                SQL = o.SQL
            }).ToList();
        }

        /// <summary>
        /// Retrieves the schema information for the specified table in a SQLite database.
        /// </summary>
        /// <remarks>This method queries the SQLite database to retrieve column-level schema information for the specified table.
        /// The schema details are extracted using the SQLite `PRAGMA table_info` command.</remarks>
        /// <param name="tableName">The name of the table for which to retrieve schema information. Cannot be null or empty.</param>
        /// <returns>An <see cref="List{TableSchemaInfo}"/> containing schema details for the specified table. Each <see
        /// cref="TableSchemaInfo"/> object represents a column in the table, including its name, type, constraints, and
        /// whether it is part of the primary key.</returns>
        public List<TableSchemaInfo> GetTableSchema(string tableName)
        {
            var model = Query<TableSchemaInfoModel>($"PRAGMA table_info([{tableName}])");
            return model.Select(o => new TableSchemaInfo
            {
                Id = o.CID,
                Name = o.Name,
                Type = o.Type,
                IsNotNull = o.NotNull,
                DefaultValue = o.Dflt_Value,
                IsPrimaryKey = o.PK
            }).ToList();
        }

        /// <summary>
        /// Determines whether a table with the specified name exists in the SQLite database.
        /// </summary>
        /// <remarks>This method queries the SQLite system table <c>sqlite_master</c> to determine if a table with
        /// the specified name exists. It performs a case-sensitive match on the table name.</remarks>
        /// <param name="tableName">The name of the table to check for existence. Cannot be <see langword="null"/> or empty.</param>
        /// <returns><see langword="true"/> if the table exists; otherwise, <see langword="false"/>.</returns>
        public bool DoesTableExist(string tableName)
        {
            var result = ExecuteScalar<bool?>("SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = @TableName LIMIT 1",
                new { TableName = tableName });
            return result == true;
        }

        /// <summary>
        /// Determines whether a specified column exists in the given table.
        /// </summary>
        /// <remarks>This method performs a case-insensitive comparison when checking for the column's
        /// existence.</remarks>
        /// <param name="tableName">The name of the table to check. Cannot be null or empty.</param>
        /// <param name="columnName">The name of the column to check for existence. Cannot be null or empty.</param>
        /// <returns><see langword="true"/> if the specified column exists in the table; otherwise, <see langword="false"/>.</returns>
        public bool DoesColumnExist(string tableName, string columnName)
        {
            var tableSchema = GetTableSchema(tableName);
            return tableSchema.Any(o => o.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines whether a specified index exists on a given table in the database.
        /// </summary>
        /// <remarks>This method queries the database schema to determine the existence of the specified
        /// index. It is useful for verifying database structure before performing operations that depend on the
        /// index.</remarks>
        /// <param name="tableName">The name of the table to check for the index. Cannot be null or empty.</param>
        /// <param name="indexName">The name of the index to check for. Cannot be null or empty.</param>
        /// <returns><see langword="true"/> if the specified index exists on the table; otherwise, <see langword="false"/>.</returns>
        public bool DoesIndexExist(string tableName, string indexName)
        {
            var result = ExecuteScalar<bool?>("SELECT 1 FROM sqlite_master WHERE type = 'index' AND tbl_name = @TableName AND name = @IndexName LIMIT 1",
                new { TableName = tableName, IndexName = indexName });
            return result == true;
        }

        #endregion
    }
}
