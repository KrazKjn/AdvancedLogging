using AdvancedLogging.Enumerations;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Models;
using System;
using System.Security.Cryptography;
using System.Text;

namespace AdvancedLogging.BusinessLogic
{
    /// <summary>
    /// Provides security-related helper methods such as password encryption and validation.
    /// </summary>
    public class SecurityHelper : ISecurityHelper
    {
        private readonly string userName;
        private readonly ISecurityHelperDataAccess dal;
        private SecurityHelperInfo securityInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityHelper"/> class using a username.
        /// </summary>
        /// <param name="user">The username.</param>
        /// <param name="dalInterface">The data access layer interface.</param>
        public SecurityHelper(string user, ISecurityHelperDataAccess dalInterface)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { user, dalInterface }))
            {
                try
                {
                    userName = user;
                    dal = dalInterface;
                    securityInfo = dal.GetSecurityInfo(user);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { user, dalInterface }, null, true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityHelper"/> class using a primary ID.
        /// </summary>
        /// <param name="secPrimaryId">The security primary ID.</param>
        /// <param name="dalInterface">The data access layer interface.</param>
        public SecurityHelper(long secPrimaryId, ISecurityHelperDataAccess dalInterface)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { secPrimaryId, dalInterface }))
            {
                try
                {
                    dal = dalInterface;
                    securityInfo = dal.GetSecurityInfo(secPrimaryId);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { secPrimaryId, dalInterface }, null, true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Encrypts the given password using a custom unsecured algorithm.
        /// </summary>
        /// <param name="newPassword">The new password to encrypt.</param>
        /// <returns>The encrypted password.</returns>
        private string EncryptToUnsecure(string newPassword)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { newPassword }))
            {
                try
                {
                    var resultHash = new StringBuilder();
                    string strPasswd = newPassword.PadRight(32, ' ');
                    char[] HexChars = { '€', '', '‚', 'ƒ', '„', '…', '†', '‡' };

                    for (int intIndex = 0; intIndex < strPasswd.Length; intIndex++)
                    {
                        if (strPasswd[intIndex] == ' ')
                        {
                            resultHash.Append(' ');
                        }
                        else
                        {
                            byte[] tempBytes = Encoding.ASCII.GetBytes(strPasswd.Substring(intIndex, 1));
                            tempBytes = new byte[] { Convert.ToByte((Convert.ToInt32(tempBytes[0]) + 9)) };

                            if (tempBytes[0] > 127)
                            {
                                int ArrVal = tempBytes[0] - 128;
                                resultHash.Append(HexChars[ArrVal]);
                            }
                            else
                            {
                                resultHash.Append(Encoding.ASCII.GetString(tempBytes));
                            }
                        }
                    }

                    return resultHash.ToString();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { newPassword }, null, true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Encrypts the given password using the SHA-256 algorithm.
        /// </summary>
        /// <param name="newPassword">The new password to encrypt.</param>
        /// <returns>The encrypted password.</returns>
        private string EncryptToSHA256(string newPassword)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { newPassword }))
            {
                try
                {
                    string strPasswd = securityInfo.PrimaryKeyStr + newPassword;
                    using (SHA256Managed hashClass = new SHA256Managed())
                    {
                        byte[] bytePassword = hashClass.ComputeHash(Encoding.UTF8.GetBytes(strPasswd));
                        return Convert.ToBase64String(bytePassword);
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { newPassword }, null, true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Encrypts the given password using the SHA-512 algorithm.
        /// </summary>
        /// <param name="newPassword">The new password to encrypt.</param>
        /// <returns>The encrypted password.</returns>
        private string EncryptToSHA512(string newPassword)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { newPassword }))
            {
                try
                {
                    string strPasswd = securityInfo.PrimaryKeyStr + newPassword;
                    using (SHA512Managed hashClass = new SHA512Managed())
                    {
                        byte[] bytePassword = hashClass.ComputeHash(Encoding.UTF8.GetBytes(strPasswd));
                        return Convert.ToBase64String(bytePassword);
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { newPassword }, null, true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Converts a plain text password to a hashed password using the specified hash type.
        /// </summary>
        /// <param name="plainTextPassword">The plain text password.</param>
        /// <param name="hashToUse">The hash type to use.</param>
        /// <returns>The hashed password.</returns>
        private string PlainTextToHashed(string plainTextPassword, HashType hashToUse)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { plainTextPassword, hashToUse }))
            {
                try
                {
                    string hashedPassword = string.Empty;

                    switch (hashToUse)
                    {
                        case HashType.Unsecured:
                            hashedPassword = EncryptToUnsecure(plainTextPassword);
                            break;
                        case HashType.SHA2_256:
                            hashedPassword = EncryptToSHA256(plainTextPassword);
                            break;
                        case HashType.SHA2_512:
                            hashedPassword = EncryptToSHA512(plainTextPassword);
                            break;
                        default:
                            break;
                    }

                    return hashedPassword;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { plainTextPassword, hashToUse }, null, true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Saves the given plain text password after encrypting it.
        /// </summary>
        /// <param name="plainTextPassword">The plain text password to save.</param>
        public void SavePassword(string plainTextPassword)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { plainTextPassword }))
            {
                try
                {
                    string encryptedPassword = PlainTextToHashed(plainTextPassword, securityInfo.TargetHash);
                    securityInfo = dal.SetSecurityInfo(securityInfo, encryptedPassword);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { plainTextPassword }, null, true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Checks if the given plain text password is correct.
        /// </summary>
        /// <param name="plainTextPassword">The plain text password to check.</param>
        /// <returns><c>true</c> if the password is correct; otherwise, <c>false</c>.</returns>
        public bool IsPasswordCorrect(string plainTextPassword)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { plainTextPassword }))
            {
                try
                {
                    bool result = false;
                    string encryptedPassword = PlainTextToHashed(plainTextPassword, securityInfo.CurrentHash).PadRight(65, ' ');
                    string currentEncryptedPassword = securityInfo.EncryptedPassword.PadRight(65, ' ');

                    vAutoLogFunction.WriteDebug(2, string.Format("Password Check: plain = '{0}', new encrypt '{1}', from db '{2}'. ", plainTextPassword, encryptedPassword, currentEncryptedPassword));

                    if (encryptedPassword == currentEncryptedPassword)
                    {
                        result = true;
                        if (securityInfo.TargetHash != securityInfo.CurrentHash)
                        {
                            SavePassword(plainTextPassword);
                        }
                    }
                    return result;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { plainTextPassword }, null, true, exOuter);
                    throw;
                }
            }
        }
    }
}
