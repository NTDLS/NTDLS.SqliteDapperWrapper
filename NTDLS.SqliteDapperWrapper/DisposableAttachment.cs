using Dapper;
using Microsoft.Data.Sqlite;

namespace NTDLS.SqliteDapperWrapper
{
    /// <summary>
    /// Allows for attaching other databases and performing cross-database-joins. 
    /// </summary>
    public class DisposableAttachment : IDisposable
    {
        /// <summary>
        /// The underlying connection to the SQLite database.
        /// </summary>
        public SqliteConnection NativeConnection { get; private set; }

        /// <summary>
        /// The alias of the attached database. This is how the database should be referenced in scripts.
        /// </summary>
        public string DatabaseAlias { get; private set; }

        internal DisposableAttachment(SqliteConnection nativeConnection, string databaseAlias)
        {
            NativeConnection = nativeConnection;
            DatabaseAlias = databaseAlias;
        }


        /// <summary>
        /// Detaches the attached database file.
        /// </summary>
        public void Dispose()
        {
            NativeConnection.Execute($"DETACH DATABASE {DatabaseAlias}");
        }
    }
}
