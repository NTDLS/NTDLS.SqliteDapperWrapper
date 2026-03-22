using Dapper;

namespace NTDLS.SqliteDapperWrapper
{
    /// <summary>
    /// An instance that creates ManagedDataStorageInstances based off of the connection string stored in this class.
    /// </summary>
    public class SqliteManagedFactory
    {
        /// <summary>
        /// The connection string that will be used by the factory, can be set using SetConnectionString().
        /// </summary>
        public string DefaultConnectionString { get; private set; } = string.Empty;

        /// <summary>
        /// Delegate used for ephemeral operations.
        /// </summary>
        public delegate void EphemeralProc(SqliteManagedInstance connection);

        /// <summary>
        /// Delegate used for ephemeral operations.
        /// </summary>
        public delegate T EphemeralProc<T>(SqliteManagedInstance connection);

        /// <summary>
        /// Delegate used for ephemeral operations.
        /// </summary>
        public delegate Task EphemeralProcAsync(SqliteManagedInstance connection);

        /// <summary>
        /// Delegate used for ephemeral operations.
        /// </summary>
        public delegate Task<T> EphemeralProcAsync<T>(SqliteManagedInstance connection);

        /// <summary>
        /// Creates a new instance of ManagedDataStorageFactory.
        /// </summary>
        public SqliteManagedFactory(string connectionString)
        {
            if (!connectionString.StartsWith("Data Source", StringComparison.InvariantCultureIgnoreCase))
            {
                connectionString = @$"Data Source={connectionString}";
            }

            DefaultConnectionString = connectionString;

            if (!SqlMapper.HasTypeHandler(typeof(GuidTypeHandler)))
                SqlMapper.AddTypeHandler(new GuidTypeHandler());

            if (!SqlMapper.HasTypeHandler(typeof(NullableGuidTypeHandler)))
                SqlMapper.AddTypeHandler(new NullableGuidTypeHandler());
        }

        /// <summary>
        /// Creates a new instance of ManagedDataStorageFactory.
        /// </summary>
        public SqliteManagedFactory()
        {
            if (!SqlMapper.HasTypeHandler(typeof(GuidTypeHandler)))
                SqlMapper.AddTypeHandler(new GuidTypeHandler());

            if (!SqlMapper.HasTypeHandler(typeof(NullableGuidTypeHandler)))
                SqlMapper.AddTypeHandler(new NullableGuidTypeHandler());
        }

        /// <summary>
        /// Sets the connection string that will be used by this factory.
        /// </summary>
        public void SetConnectionString(string? connectionString)
        {
            if (connectionString != null && !connectionString.StartsWith("Data Source", StringComparison.InvariantCultureIgnoreCase))
            {
                connectionString = @$"Data Source={connectionString}";
            }

            DefaultConnectionString = connectionString ?? string.Empty;
        }

        /// <summary>
        /// Instantiates/opens a SQL connection using the default connection string, executes the given delegate and then closed/disposes the connection.
        /// </summary>
        public void Ephemeral(EphemeralProc func)
        {
            using var connection = new SqliteManagedInstance(DefaultConnectionString);
            func(connection);
        }

        /// <summary>
        /// Instantiates/opens a SQL connection using the default connection string, executes the given delegate and then closed/disposes the connection.
        /// </summary>
        public async Task EphemeralAsync(EphemeralProcAsync func)
        {
            using var connection = new SqliteManagedInstance(DefaultConnectionString);
            await func(connection);
        }

        /// <summary>
        /// Instantiates/opens a SQL connection using the default connection string, executes the given delegate and then closed/disposes the connection.
        /// </summary>
        public T Ephemeral<T>(EphemeralProc<T> func)
        {
            using var connection = new SqliteManagedInstance(DefaultConnectionString);
            return func(connection);
        }

        /// <summary>
        /// Instantiates/opens a SQL connection using the given connection string, executes the given delegate and then closed/disposes the connection.
        /// </summary>
        public void Ephemeral(string connectionString, EphemeralProc func)
        {
            using var connection = new SqliteManagedInstance(connectionString);
            func(connection);
        }

        /// <summary>
        /// Instantiates/opens a SQL connection using the given connection string, executes the given delegate and then closed/disposes the connection.
        /// </summary>
        public async Task EphemeralAsync(string connectionString, EphemeralProcAsync func)
        {
            using var connection = new SqliteManagedInstance(connectionString);
            await func(connection);
        }

        /// <summary>
        /// Instantiates/opens a SQL connection using the given connection string, executes the given delegate and then closed/disposes the connection.
        /// </summary>
        public T Ephemeral<T>(string connectionString, EphemeralProc<T> func)
        {
            using var connection = new SqliteManagedInstance(connectionString);
            return func(connection);
        }

        #region SqliteManagedInstance passthroughs.

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the results.
        /// </summary>
        public List<T> Query<T>(string scriptName)
            => Ephemeral(DefaultConnectionString, o => o.Query<T>(scriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the results.
        /// </summary>
        public List<T> Query<T>(string scriptName, object param)
            => Ephemeral(DefaultConnectionString, o => o.Query<T>(scriptName, param));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or throws an exception.
        /// </summary>
        public T QueryFirst<T>(string scriptName)
            => Ephemeral(DefaultConnectionString, o => o.QueryFirst<T>(scriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or a default value.
        /// </summary>
        public T QueryFirstOrDefault<T>(string scriptName, T defaultValue)
            => Ephemeral(DefaultConnectionString, o => o.QueryFirstOrDefault<T>(scriptName)) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or throws an exception.
        /// </summary>
        public T QueryFirst<T>(string scriptName, object param)
            => Ephemeral(DefaultConnectionString, o => o.QueryFirst<T>(scriptName, param));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or a default value.
        /// </summary>
        public T QueryFirstOrDefault<T>(string scriptName, object param, T defaultValue)
            => Ephemeral(DefaultConnectionString, o => o.QueryFirstOrDefault<T>(scriptName, param)) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or throws an exception.
        /// </summary>
        public T QuerySingle<T>(string scriptName)
            => Ephemeral(DefaultConnectionString, o => o.QuerySingle<T>(scriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or throws an exception.
        /// </summary>
        public T QuerySingle<T>(string scriptName, object param)
            => Ephemeral(DefaultConnectionString, o => o.QuerySingle<T>(scriptName, param));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public T QuerySingleOrDefault<T>(string scriptName, T defaultValue)
            => Ephemeral(DefaultConnectionString, o => o.QuerySingleOrDefault<T>(scriptName, defaultValue));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public T QuerySingleOrDefault<T>(string scriptName, object param, T defaultValue)
            => Ephemeral(DefaultConnectionString, o => o.QuerySingleOrDefault<T>(scriptName, param, defaultValue));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public T? QuerySingleOrDefault<T>(string scriptName)
            => Ephemeral(DefaultConnectionString, o => o.QuerySingleOrDefault<T>(scriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public T? QuerySingleOrDefault<T>(string scriptName, object param)
            => Ephemeral(DefaultConnectionString, o => o.QuerySingleOrDefault<T>(scriptName, param));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public T? QueryFirstOrDefault<T>(string scriptName)
            => Ephemeral(DefaultConnectionString, o => o.QueryFirstOrDefault<T>(scriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public T? QueryFirstOrDefault<T>(string scriptName, object param)
            => Ephemeral(DefaultConnectionString, o => o.QueryFirstOrDefault<T>(scriptName, param));

        /// <summary>
        /// Executes the given script name or SQL text on the database and does not return a result.
        /// </summary>
        public void Execute(string scriptName)
            => Ephemeral(DefaultConnectionString, o => o.Execute(scriptName));

        /// <summary>
        /// Executes the given script name or SQL text on the database and does not return a result.
        /// </summary>
        public void Execute(string scriptName, object param)
            => Ephemeral(DefaultConnectionString, o => o.Execute(scriptName, param));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the scalar result.
        /// </summary>
        public T ExecuteScalar<T>(string scriptName, T defaultValue)
            => Ephemeral(DefaultConnectionString, o => o.ExecuteScalar<T>(scriptName)) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the scalar result.
        /// </summary>
        public T ExecuteScalar<T>(string scriptName, object param, T defaultValue)
            => Ephemeral(DefaultConnectionString, o => o.ExecuteScalar<T>(scriptName, param)) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the scalar result or throws an exception.
        /// </summary>
        public T? ExecuteScalar<T>(string scriptName)
            => Ephemeral(DefaultConnectionString, o => o.ExecuteScalar<T>(scriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the scalar result or throws an exception.
        /// </summary>
        public T? ExecuteScalar<T>(string scriptName, object param)
            => Ephemeral(DefaultConnectionString, o => o.ExecuteScalar<T>(scriptName, param));

        #endregion

        #region SqliteManagedInstance passthroughs (async).

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the results.
        /// </summary>
        public async Task<List<T>> QueryAsync<T>(string scriptName)
            => await Ephemeral(DefaultConnectionString, async o => await o.QueryAsync<T>(scriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the results.
        /// </summary>
        public async Task<List<T>> QueryAsync<T>(string scriptName, object param)
            => await Ephemeral(DefaultConnectionString, async o => await o.QueryAsync<T>(scriptName, param));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or throws an exception.
        /// </summary>
        public async Task<T> QueryFirstAsync<T>(string scriptName)
            => await Ephemeral(DefaultConnectionString, async o => await o.QueryFirstAsync<T>(scriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or a default value.
        /// </summary>
        public async Task<T> QueryFirstOrDefaultAsync<T>(string scriptName, T defaultValue)
            => await Ephemeral(DefaultConnectionString, async o => await o.QueryFirstOrDefaultAsync<T>(scriptName)) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or throws an exception.
        /// </summary>
        public async Task<T> QueryFirstAsync<T>(string scriptName, object param)
            => await Ephemeral(DefaultConnectionString, async o => await o.QueryFirstAsync<T>(scriptName, param));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or a default value.
        /// </summary>
        public async Task<T> QueryFirstOrDefaultAsync<T>(string scriptName, object param, T defaultValue)
            => await Ephemeral(DefaultConnectionString, async o => await o.QueryFirstOrDefaultAsync<T>(scriptName, param)) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or throws an exception.
        /// </summary>
        public async Task<T> QuerySingleAsync<T>(string scriptName)
            => await Ephemeral(DefaultConnectionString, async o => await o.QuerySingleAsync<T>(scriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or throws an exception.
        /// </summary>
        public async Task<T> QuerySingleAsync<T>(string scriptName, object param)
            => await Ephemeral(DefaultConnectionString, async o => await o.QuerySingleAsync<T>(scriptName, param));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public async Task<T> QuerySingleOrDefaultAsync<T>(string scriptName, T defaultValue)
            => await Ephemeral(DefaultConnectionString, async o => await o.QuerySingleOrDefaultAsync<T>(scriptName, defaultValue));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public async Task<T> QuerySingleOrDefaultAsync<T>(string scriptName, object param, T defaultValue)
            => await Ephemeral(DefaultConnectionString, async o => await o.QuerySingleOrDefaultAsync<T>(scriptName, param, defaultValue));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public async Task<T?> QuerySingleOrDefaultAsync<T>(string scriptName)
            => await Ephemeral(DefaultConnectionString, async o => await o.QuerySingleOrDefaultAsync<T>(scriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public async Task<T?> QuerySingleOrDefaultAsync<T>(string scriptName, object param)
            => await Ephemeral(DefaultConnectionString, async o => await o.QuerySingleOrDefaultAsync<T>(scriptName, param));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public async Task<T?> QueryFirstOrDefaultAsync<T>(string scriptName)
            => await Ephemeral(DefaultConnectionString, async o => await o.QueryFirstOrDefaultAsync<T>(scriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        public async Task<T?> QueryFirstOrDefaultAsync<T>(string scriptName, object param)
            => await Ephemeral(DefaultConnectionString, async o => await o.QueryFirstOrDefaultAsync<T>(scriptName, param));

        /// <summary>
        /// Executes the given script name or SQL text on the database and does not return a result.
        /// </summary>
        public async Task ExecuteAsync(string scriptName)
            => await Ephemeral(DefaultConnectionString, async o => await o.ExecuteAsync(scriptName));

        /// <summary>
        /// Executes the given script name or SQL text on the database and does not return a result.
        /// </summary>
        public async Task ExecuteAsync(string scriptName, object param)
            => await Ephemeral(DefaultConnectionString, async o => await o.ExecuteAsync(scriptName, param));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the scalar result.
        /// </summary>
        public async Task<T> ExecuteScalarAsync<T>(string scriptName, T defaultValue)
            => await Ephemeral(DefaultConnectionString, async o => await o.ExecuteScalarAsync<T>(scriptName)) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the scalar result.
        /// </summary>
        public async Task<T> ExecuteScalarAsync<T>(string scriptName, object param, T defaultValue)
            => await Ephemeral(DefaultConnectionString, async o => await o.ExecuteScalarAsync<T>(scriptName, param)) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the scalar result or throws an exception.
        /// </summary>
        public async Task<T?> ExecuteScalarAsync<T>(string scriptName)
            => await Ephemeral(DefaultConnectionString, async o => await o.ExecuteScalarAsync<T>(scriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the scalar result or throws an exception.
        /// </summary>
        public async Task<T?> ExecuteScalarAsync<T>(string scriptName, object param)
            => await Ephemeral(DefaultConnectionString, async o => await o.ExecuteScalarAsync<T>(scriptName, param));

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
            => Ephemeral(DefaultConnectionString, o => o.GetTables());

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
            => Ephemeral(DefaultConnectionString, o => o.GetIndexes(tableName));

        /// <summary>
        /// Retrieves a collection of index information from the SQLite database.
        /// </summary>
        /// <remarks>This method queries the SQLite system table <c>sqlite_master</c> to retrieve
        /// information about all indexes defined in the database. The returned collection includes details such as the
        /// index name, associated table name, root page, and SQL definition.</remarks>
        /// <returns>An <see cref="List{T}"/> of <see cref="IndexInfo"/> objects, where each object represents metadata
        /// about an index in the SQLite database.</returns>
        public List<IndexInfo> GetIndexes()
            => Ephemeral(DefaultConnectionString, o => o.GetIndexes());

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
            => Ephemeral(DefaultConnectionString, o => o.GetTableSchema(tableName));

        /// <summary>
        /// Determines whether a table with the specified name exists in the SQLite database.
        /// </summary>
        /// <remarks>This method queries the SQLite system table <c>sqlite_master</c> to determine if a table with
        /// the specified name exists. It performs a case-sensitive match on the table name.</remarks>
        /// <param name="tableName">The name of the table to check for existence. Cannot be <see langword="null"/> or empty.</param>
        /// <returns><see langword="true"/> if the table exists; otherwise, <see langword="false"/>.</returns>
        public bool DoesTableExist(string tableName)
            => Ephemeral(DefaultConnectionString, o => o.DoesTableExist(tableName));

        /// <summary>
        /// Determines whether a specified column exists in the given table.
        /// </summary>
        /// <remarks>This method performs a case-insensitive comparison when checking for the column's
        /// existence.</remarks>
        /// <param name="tableName">The name of the table to check. Cannot be null or empty.</param>
        /// <param name="columnName">The name of the column to check for existence. Cannot be null or empty.</param>
        /// <returns><see langword="true"/> if the specified column exists in the table; otherwise, <see langword="false"/>.</returns>
        public bool DoesColumnExist(string tableName, string columnName)
            => Ephemeral(DefaultConnectionString, o => o.DoesColumnExist(tableName, columnName));

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
            => Ephemeral(DefaultConnectionString, o => o.DoesIndexExist(tableName, indexName));

        #endregion
    }
}
