namespace NTDLS.SqliteDapperWrapper
{
    /// <summary>
    /// Represents metadata information about a database index, including its associated table, type, root page, and
    /// creation SQL.
    /// </summary>
    /// <remarks>This class provides details about a database index, such as the name of the table it
    /// is associated with,  the type of the index, the root page number in the database file, and the SQL statement
    /// used to create the index. It can be used to inspect or manage database index structures.</remarks>
    public class IndexInfo
    {
        /// <summary>
        /// Name associated with the object.
        /// </summary>
        public int Name { get; set; }
        /// <summary>
        /// Name of the database table associated with the current index.
        /// </summary>
        public string TableName { get; set; } = string.Empty;
        /// <summary>
        /// Type of the entity or object represented by this instance.
        /// </summary>
        public string Type { get; set; } = string.Empty;
        /// <summary>
        /// Root page number of the database file.
        /// </summary>
        public int RootPage { get; set; }
        /// <summary>
        /// SQL statement used to create the table.
        /// </summary>
        public string SQL { get; set; } = string.Empty;
    }

    internal class IndexInfoModel
    {
        public string Type { get; set; } = string.Empty;
        public string Tbl_Name { get; set; } = string.Empty;
        public int Name { get; set; }
        public int RootPage { get; set; }
        public string SQL { get; set; } = string.Empty;
    }
}
