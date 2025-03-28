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
        /// Creates a new instance of ManagedDataStorageFactory.
        /// </summary>
        public SqliteManagedFactory(string connectionString)
        {
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
        public T Ephemeral<T>(string connectionString, EphemeralProc<T> func)
        {
            using var connection = new SqliteManagedInstance(connectionString);
            return func(connection);
        }

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the results.
        /// </summary>
        public IEnumerable<T> Query<T>(string scriptName)
            => Ephemeral(DefaultConnectionString, o => o.Query<T>(scriptName));

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the results.
        /// </summary>
        public IEnumerable<T> Query<T>(string scriptName, object param)
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
    }
}
