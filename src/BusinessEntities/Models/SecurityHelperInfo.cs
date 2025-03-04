using AdvancedLogging.Enumerations;

namespace AdvancedLogging.Models
{
    /// <summary>
    /// Represents security-related information including primary key, encrypted password, and hash types.
    /// </summary>
    public class SecurityHelperInfo
    {
        /// <summary>
        /// Gets or sets the primary key as a string.
        /// </summary>
        public string PrimaryKeyStr { get; set; }

        /// <summary>
        /// Gets or sets the encrypted password.
        /// </summary>
        public string EncryptedPassword { get; set; }

        /// <summary>
        /// Gets or sets the current hash type.
        /// </summary>
        public HashType CurrentHash { get; set; }

        /// <summary>
        /// Gets or sets the target hash type.
        /// </summary>
        public HashType TargetHash { get; set; }
    }
}
