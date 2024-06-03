using Dapper;
using Microsoft.Data.Sqlite;

namespace NTDLS.SqliteDapperWrapper
{
    /// <summary>
    /// Creates a session scoped temp table that contains the values from the supplied list, the temp table is dropped on dispose.
    /// This allows for a simple replacement implementation of STRING_SPLIT.
    /// </summary>
    public class DisposableValueListTable : IDisposable
    {
        /// <summary>
        /// The underlying connection to the SQLite database.
        /// </summary>
        public SqliteConnection NativeConnection { get; private set; }

        /// <summary>
        /// The table name of the temporary table.
        /// </summary>
        public string TableName { get; private set; }

        internal DisposableValueListTable(SqliteConnection nativeConnection, string tableName)
        {
            NativeConnection = nativeConnection;
            TableName = tableName;
        }

        /// <summary>
        /// Drops the temporary table.
        /// </summary>
        public void Dispose()
        {
            NativeConnection.Execute($"DROP TABLE {TableName}");
        }
    }
}
