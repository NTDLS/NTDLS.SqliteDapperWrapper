using NTDLS.EmbeddedResource;
using System.Text;

namespace NTDLS.SqliteDapperWrapper
{
    /// <summary>
    /// Loads SQL scripts from embedded resources or file system.
    /// </summary>
    public static partial class SqlScriptLoader
    {
        /// <summary>
        /// Loads a SQL script from an embedded resource or file system.
        /// </summary>
        /// <param name="sqlTextOrEmbeddedResource">The SQL text, stored procedure name or the name and path of an embedded resource file.</param>
        /// <returns>The loaded SQL script.</returns>
        public static string LoadSqlScript(string sqlTextOrEmbeddedResource)
        {
            if (sqlTextOrEmbeddedResource.EndsWith(".sql", StringComparison.InvariantCultureIgnoreCase))
            {
                sqlTextOrEmbeddedResource = EmbeddedResourceReader.LoadText(sqlTextOrEmbeddedResource);
            }
            return sqlTextOrEmbeddedResource;
        }

        /// <summary>
        /// Loads a SQL script from an embedded resource or file system.
        /// </summary>
        /// <param name="sqlTextOrEmbeddedResource">The SQL text, stored procedure name or the name and path of an embedded resource file.</param>
        /// <param name="param">An array of objects to format the text content of the embedded resource. The formatting is performed using string.Format semantics.</param>
        /// <param name="encoding"> Optional parameter to specify the encoding of the embedded resource. If not provided, UTF-8 encoding is used by default.</param>
        /// <returns>The loaded SQL script.</returns>
        public static string FormatSqlScript(string sqlTextOrEmbeddedResource, object[] param, Encoding? encoding = null)
        {
            if (sqlTextOrEmbeddedResource.EndsWith(".sql", StringComparison.InvariantCultureIgnoreCase))
            {
                sqlTextOrEmbeddedResource = EmbeddedResourceReader.Format(sqlTextOrEmbeddedResource, param, encoding);
            }
            return sqlTextOrEmbeddedResource;
        }
    }
}
