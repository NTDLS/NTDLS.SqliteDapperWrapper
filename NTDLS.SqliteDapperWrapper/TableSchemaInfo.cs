namespace NTDLS.SqliteDapperWrapper
{
    /// <summary>
    /// Provides information about the schema of a table.
    /// </summary>
    public class TableSchemaInfo
    {
        /// <summary>
        /// Unique identifier for the entity.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Name associated with the object.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Type of the entity or object represented by this instance.
        /// </summary>
        public string Type { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets a value indicating whether the object is not null.
        /// </summary>
        public bool IsNotNull { get; set; }
        /// <summary>
        /// Default value to be used when no specific value is provided.
        /// </summary>
        public string DefaultValue { get; set; } = string.Empty;
        /// <summary>
        /// Indicates whether the field is the primary key in the table.
        /// </summary>
        public bool IsPrimaryKey { get; set; }
    }

    internal class TableSchemaInfoModel
    {
        public int CID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool NotNull { get; set; }
        public string Dflt_Value { get; set; } = string.Empty;
        public bool PK { get; set; }
    }
}
