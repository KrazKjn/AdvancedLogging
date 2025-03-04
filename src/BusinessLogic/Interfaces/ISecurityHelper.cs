namespace AdvancedLogging.Interfaces
{
    /// <summary>
    /// Provides methods for handling security-related operations.
    /// </summary>
    public interface ISecurityHelper
    {
        /// <summary>
        /// Saves the provided plain text password securely.
        /// </summary>
        /// <param name="plainTextPassword">The plain text password to save.</param>
        void SavePassword(string plainTextPassword);

        /// <summary>
        /// Checks if the provided plain text password is correct.
        /// </summary>
        /// <param name="plainTextPassword">The plain text password to check.</param>
        /// <returns>True if the password is correct; otherwise, false.</returns>
        bool IsPasswordCorrect(string plainTextPassword);
    }
}
