namespace AdvancedLogging.AutoCoder
{
    /// <summary>
    /// Enumeration representing different types of code items.
    /// </summary>
    public enum CodeItems
    {
        /// <summary>
        /// No code item.
        /// </summary>
        None = 0,

        /// <summary>
        /// Represents a constructor.
        /// </summary>
        Constructor = 0x1,

        /// <summary>
        /// Represents a method.
        /// </summary>
        Method = 0x2,

        /// <summary>
        /// Represents a retry mechanism for SQL operations.
        /// </summary>
        RetrySql = 0x4,

        /// <summary>
        /// Represents a retry mechanism for HTTP operations.
        /// </summary>
        RetryHttp = 0x8,

        /// <summary>
        /// Represents automatic logging.
        /// </summary>
        AutoLog = 0x10,

        /// <summary>
        /// Represents a try-catch block.
        /// </summary>
        TryCatch = 0x20,

        /// <summary>
        /// Represents a property.
        /// </summary>
        Property = 0x40,

        /// <summary>
        /// Represents a class.
        /// </summary>
        Class = 0x80,

        /// <summary>
        /// Represents modification of an empty body.
        /// </summary>
        ModifyEmptyBody = 0x100
    }
}
