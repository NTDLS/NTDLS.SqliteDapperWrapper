namespace NTDLS.SqliteDapperWrapper
{
    /// <summary>
    /// Represents metadata information about a database table.
    /// </summary>
    /// <remarks>This class provides details about a table's type, name, root page, and associated SQL
    /// definition. It is commonly used to retrieve or store structural information about tables in a
    /// database.</remarks>
    public class TableInfo
    {
        /// <summary>
        /// Name associated with the object.
        /// </summary>
        public int Name { get; set; }
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
}
