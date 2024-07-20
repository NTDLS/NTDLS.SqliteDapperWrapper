using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;

namespace NTDLS.SqliteDapperWrapper
{
    /// <summary>
    /// A disposable database connection wrapper that functions as an ephemeral instance.
    /// One instance of this class is generally created per query.
    /// </summary>
    public class ManagedDataStorageInstance : IDisposable
    {
        private static readonly MemoryCache _cache = new("ManagedDataStorageInstance");

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
        /// <param name="connection"></param>
        public delegate void EphemeralProc(ManagedDataStorageInstance connection);

        /// <summary>
        /// Delegate used for ephemeral operations.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <returns></returns>
        public delegate T EphemeralProc<T>(ManagedDataStorageInstance connection);

        /// <summary>
        /// Creases a new instance of ManagedDataStorageInstance.
        /// </summary>
        /// <param name="connectionString"></param>
        public ManagedDataStorageInstance(string connectionString)
        {
            NativeConnection = new SqliteConnection(connectionString);

            Directory = Path.GetFullPath(Path.GetDirectoryName(NativeConnection.DataSource) ?? string.Empty);

            NativeConnection.Open();
        }

        /// <summary>
        /// Begins an atomic transaction.
        /// </summary>
        /// <returns></returns>
        public SqliteTransaction BeginTransaction()
        {
            return NativeConnection.BeginTransaction();
        }

        /// <summary>
        /// Begins an atomic transaction.
        /// </summary>
        /// <param name="isolationLevel"></param>
        /// <returns></returns>
        public SqliteTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            return NativeConnection.BeginTransaction(isolationLevel);
        }

        /// <summary>
        /// Creates a temporary table with the given name from an enumerable set of strings. The strings are stored in the column Value.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="values"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public DisposableValueListTable CreateTempTableFrom(string tableName, IEnumerable<string> values, SqliteTransaction transaction)
        {
            var result = new DisposableValueListTable(NativeConnection, tableName);

            var createTempTableCommand = new SqliteCommand($"CREATE TEMP TABLE {tableName} (Value TEXT COLLATE NOCASE);", NativeConnection, transaction);
            createTempTableCommand.ExecuteNonQuery();

            var insertTagCommand = new SqliteCommand($"INSERT INTO {tableName} (Value) VALUES (@Tag);", NativeConnection, transaction);

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
        /// <param name="tableName"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public DisposableValueListTable CreateTempTableFrom(string tableName, IEnumerable<string> values)
        {
            var result = new DisposableValueListTable(NativeConnection, tableName);

            using var transaction = NativeConnection.BeginTransaction();

            var createTempTableCommand = new SqliteCommand($"CREATE TEMP TABLE {tableName} (Value TEXT COLLATE NOCASE);", NativeConnection, transaction);
            createTempTableCommand.ExecuteNonQuery();

            var insertTagCommand = new SqliteCommand($"INSERT INTO {tableName} (Value) VALUES (@Tag);", NativeConnection, transaction);

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
        /// <param name="tableName"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public DisposableValueListTable CreateTempTableFrom(string tableName, IEnumerable<int> values)
        {
            var result = new DisposableValueListTable(NativeConnection, tableName);

            using var transaction = NativeConnection.BeginTransaction();

            var createTempTableCommand = new SqliteCommand($"CREATE TEMP TABLE {tableName} (Value TEXT COLLATE NOCASE);", NativeConnection, transaction);
            createTempTableCommand.ExecuteNonQuery();

            var insertTagCommand = new SqliteCommand($"INSERT INTO {tableName} (Value) VALUES (@Tag);", NativeConnection, transaction);

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
        /// <typeparam name="T"></typeparam>
        /// <param name="tableName"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public DisposableValueListTable CreateTempTableFrom<T>(string tableName, IEnumerable<T> values)
        {
            var result = new DisposableValueListTable(NativeConnection, tableName);
            using var transaction = NativeConnection.BeginTransaction();

            // Use reflection to get property names and types of T
            PropertyInfo[] props = typeof(T).GetProperties();
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
            var createTempTableCommand = new SqliteCommand($"CREATE TEMP TABLE {tableName} ({columns});", NativeConnection, transaction);
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
            var insertCommand = new SqliteCommand(insertCommandText, NativeConnection, transaction);

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
        }

        /// <summary>
        /// Attaches another SQLite database to the current native connection. The attached database is removed when this object is disposed.
        /// </summary>
        /// <param name="databaseFileName"></param>
        /// <param name="alias"></param>
        /// <returns></returns>
        public DisposableAttachment Attach(string databaseFileName, string alias)
        {
            NativeConnection.Execute($"ATTACH DATABASE '{Directory}\\{databaseFileName}' AS {alias};");
            return new DisposableAttachment(NativeConnection, alias);
        }

        /// <summary>
        /// Returns the given text, or if the script ends with ".sql", the script will be
        /// located and loaded form the executing assembly (assuming it is an embedded resource).
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string TranslateSqlScript(string script)
        {
            string cacheKey = $":{script.ToLower()}".Replace('.', ':');

            if (cacheKey.EndsWith(":sql"))
            {
                if (_cache.Get(cacheKey) is string cachedScriptText)
                {
                    return cachedScriptText;
                }

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    var scriptText = SearchAssembly(assembly, cacheKey);
                    if (scriptText != null)
                    {
                        return scriptText;
                    }
                }

                throw new Exception($"The embedded script resource could not be found after enumeration: '{cacheKey}'");
            }

            return script;
        }

        /// <summary>
        /// Searches the given assembly for a script file.
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private static string? SearchAssembly(Assembly assembly, string cacheKey)
        {
            var allScriptNames = _cache.Get($"TranslateSqlScript:SearchAssembly:{assembly.FullName}") as List<string>;
            if (allScriptNames == null)
            {
                allScriptNames = assembly.GetManifestResourceNames().Where(o => o.ToLower().EndsWith(".sql"))
                    .Select(o => $":{o}".Replace('.', ':')).ToList();
                _cache.Add("TranslateSqlScript:Names", allScriptNames, new CacheItemPolicy
                {
                    SlidingExpiration = new TimeSpan(1, 0, 0)
                });
            }

            if (allScriptNames.Count > 0)
            {
                var script = allScriptNames.Where(o => o.ToLower().EndsWith(cacheKey)).ToList();
                if (script.Count > 1)
                {
                    throw new Exception($"The script name is ambiguous: {cacheKey}.");
                }
                else if (script == null || script.Count == 0)
                {
                    return null;
                }

                using var stream = assembly.GetManifestResourceStream(script.Single().Replace(':', '.').Trim(new char[] { '.' }))
                    ?? throw new InvalidOperationException("Script not found: " + cacheKey);

                using var reader = new StreamReader(stream);
                var scriptText = reader.ReadToEnd();

                _cache.Add(cacheKey, allScriptNames, new CacheItemPolicy
                {
                    SlidingExpiration = new TimeSpan(1, 0, 0)
                });

                return scriptText;
            }

            return null;
        }

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the results.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(string textOrScriptName)
            => NativeConnection.Query<T>(TranslateSqlScript(textOrScriptName));
        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the results.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(string textOrScriptName, object param)
            => NativeConnection.Query<T>(TranslateSqlScript(textOrScriptName), param);

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the scalar result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T ExecuteScalar<T>(string textOrScriptName, T defaultValue)
            => NativeConnection.ExecuteScalar<T>(TranslateSqlScript(textOrScriptName)) ?? defaultValue;
        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the scalar result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <param name="param"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T ExecuteScalar<T>(string textOrScriptName, object param, T defaultValue)
            => NativeConnection.ExecuteScalar<T>(TranslateSqlScript(textOrScriptName), param) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or throws an exception.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <returns></returns>
        public T QueryFirst<T>(string textOrScriptName)
            => NativeConnection.QueryFirst<T>(TranslateSqlScript(textOrScriptName));
        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or throws an exception.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public T QueryFirst<T>(string textOrScriptName, object param)
            => NativeConnection.QueryFirst<T>(TranslateSqlScript(textOrScriptName), param);

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or a default value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T QueryFirstOrDefault<T>(string textOrScriptName, T defaultValue)
            => NativeConnection.QueryFirstOrDefault<T>(TranslateSqlScript(textOrScriptName)) ?? defaultValue;
        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or a default value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <param name="param"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T QueryFirstOrDefault<T>(string textOrScriptName, object param, T defaultValue)
            => NativeConnection.QueryFirstOrDefault<T>(TranslateSqlScript(textOrScriptName), param) ?? defaultValue;

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or throws an exception.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <returns></returns>
        public T QuerySingle<T>(string textOrScriptName)
            => NativeConnection.QuerySingle<T>(TranslateSqlScript(textOrScriptName));
        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or throws an exception.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public T QuerySingle<T>(string textOrScriptName, object param)
            => NativeConnection.QuerySingle<T>(TranslateSqlScript(textOrScriptName), param);

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T QuerySingleOrDefault<T>(string textOrScriptName, T defaultValue)
            => NativeConnection.QuerySingleOrDefault<T>(TranslateSqlScript(textOrScriptName)) ?? defaultValue;
        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <param name="param"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T QuerySingleOrDefault<T>(string textOrScriptName, object param, T defaultValue)
            => NativeConnection.QuerySingleOrDefault<T>(TranslateSqlScript(textOrScriptName), param) ?? defaultValue;

        /// <summary>
        /// /// Queries the database using the given script name or SQL text and returns a scalar value throws an exception.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <returns></returns>
        public T? ExecuteScalar<T>(string textOrScriptName)
            => NativeConnection.ExecuteScalar<T>(TranslateSqlScript(textOrScriptName));
        /// <summary>
        /// /// Queries the database using the given script name or SQL text and returns a scalar value throws an exception.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public T? ExecuteScalar<T>(string textOrScriptName, object param)
            => NativeConnection.ExecuteScalar<T>(TranslateSqlScript(textOrScriptName), param);

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or a default value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <returns></returns>
        public T? QueryFirstOrDefault<T>(string textOrScriptName)
            => NativeConnection.QueryFirstOrDefault<T>(TranslateSqlScript(textOrScriptName));
        /// <summary>
        /// Queries the database using the given script name or SQL text and returns the first result or a default value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public T? QueryFirstOrDefault<T>(string textOrScriptName, object param)
            => NativeConnection.QueryFirstOrDefault<T>(TranslateSqlScript(textOrScriptName), param);

        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <returns></returns>
        public T? QuerySingleOrDefault<T>(string textOrScriptName)
            => NativeConnection.QuerySingleOrDefault<T>(TranslateSqlScript(textOrScriptName));
        /// <summary>
        /// Queries the database using the given script name or SQL text and returns a single value or a default.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textOrScriptName"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public T? QuerySingleOrDefault<T>(string textOrScriptName, object param)
            => NativeConnection.QuerySingleOrDefault<T>(TranslateSqlScript(textOrScriptName), param);

        /// <summary>
        /// Executes the given script name or SQL text on the database and does not return a result.
        /// </summary>
        /// <param name="textOrScriptName"></param>
        public void Execute(string textOrScriptName)
            => NativeConnection.Execute(TranslateSqlScript(textOrScriptName));
        /// <summary>
        /// Executes the given script name or SQL text on the database and does not return a result.
        /// </summary>
        /// <param name="textOrScriptName"></param>
        /// <param name="param"></param>
        public void Execute(string textOrScriptName, object param)
            => NativeConnection.Execute(TranslateSqlScript(textOrScriptName), param);
    }
}
