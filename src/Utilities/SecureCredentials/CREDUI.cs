using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AdvancedLogging.SecureCredentials
{
    public sealed class CREDUI
    {
        private CREDUI() { }

        public const int MAX_MESSAGE_LENGTH = 100;
        public const int MAX_CAPTION_LENGTH = 100;
        public const int MAX_GENERIC_TARGET_LENGTH = 100;
        public const int MAX_DOMAIN_TARGET_LENGTH = 100;
        public const int MAX_USERNAME_LENGTH = 100;
        public const int MAX_PASSWORD_LENGTH = 100;

        [Flags]
        public enum FLAGS
        {
            INCORRECT_PASSWORD = 0x1,
            DO_NOT_PERSIST = 0x2,
            REQUEST_ADMINISTRATOR = 0x4,
            EXCLUDE_CERTIFICATES = 0x8,
            REQUIRE_CERTIFICATE = 0x10,
            SHOW_SAVE_CHECK_BOX = 0x40,
            ALWAYS_SHOW_UI = 0x80,
            REQUIRE_SMARTCARD = 0x100,
            PASSWORD_ONLY_OK = 0x200,
            VALIDATE_USERNAME = 0x400,
            COMPLETE_USERNAME = 0x800,
            PERSIST = 0x1000,
            SERVER_CREDENTIAL = 0x4000,
            EXPECT_CONFIRMATION = 0x20000,
            GENERIC_CREDENTIALS = 0x40000,
            USERNAME_TARGET_CREDENTIALS = 0x80000,
            KEEP_USERNAME = 0x100000,
        }

        public enum ReturnCodes
        {
            NO_ERROR = 0,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_INSUFFICIENT_BUFFER = 122,
            ERROR_INVALID_FLAGS = 1004,
            ERROR_NOT_FOUND = 1168,
            ERROR_CANCELLED = 1223,
            ERROR_NO_SUCH_LOGON_SESSION = 1312,
            ERROR_INVALID_ACCOUNT_NAME = 1315
        }

        public struct INFO
        {
            public int cbSize;
            public IntPtr hwndParent;
            [MarshalAs(UnmanagedType.LPWStr)] public string pszMessageText;
            [MarshalAs(UnmanagedType.LPWStr)] public string pszCaptionText;
            public IntPtr hbmBanner;
        }

        [DllImport("credui", EntryPoint = "CredUIPromptForCredentialsW", CharSet = CharSet.Unicode)]
        public static extern ReturnCodes PromptForCredentials(
            ref INFO creditUR,
            string targetName,
            IntPtr reserved1,
            int iError,
            StringBuilder userName,
            int maxUserName,
            StringBuilder password,
            int maxPassword,
            ref int iSave,
            FLAGS flags
            );

        [DllImport("credui.dll", EntryPoint = "CredUIConfirmCredentialsW", CharSet = CharSet.Unicode)]
        public static extern ReturnCodes ConfirmCredentials(
            string targetName,
            bool confirm
            );
    }
}
