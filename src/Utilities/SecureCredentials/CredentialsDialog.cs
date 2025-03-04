using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AdvancedLogging.SecureCredentials
{
    public sealed class CredentialsDialog
    {
        private const int ValidBannerHeight = 60;
        private const int ValidBannerWidth = 320;

        public CredentialsDialog(string target) : this(target, null) { }
        public CredentialsDialog(string target, string caption) : this(target, caption, null) { }
        public CredentialsDialog(string target, string caption, string message) : this(target, caption, message, null) { }
        public CredentialsDialog(string target, string caption, string message, Image banner)
        {
            this.Target = target;
            this.Caption = caption;
            this.Message = message;
            this.Banner = banner;
        }

        private bool _alwaysDisplay = false;
        public bool AlwaysDisplay
        {
            get { return _alwaysDisplay; }
            set { _alwaysDisplay = value; }
        }

        private bool _excludeCertificates = true;
        public bool ExcludeCertificates
        {
            get { return _excludeCertificates; }
            set { _excludeCertificates = value; }
        }

        private bool _persist = true;
        public bool Persist
        {
            get { return _persist; }
            set { _persist = value; }
        }

        private bool _keepName = false;
        public bool KeepName
        {
            get { return _keepName; }
            set { _keepName = value; }
        }

        private string _name = String.Empty;
        public string Name
        {
            get { return _name; }
            set
            {
                if (value != null && value.Length > CREDUI.MAX_USERNAME_LENGTH)
                {
                    string message = String.Format(
                        Thread.CurrentThread.CurrentUICulture,
                        "The name has a maximum length of {0} characters.",
                        CREDUI.MAX_USERNAME_LENGTH);
                    throw new ArgumentException(message, "Name");
                }
                _name = value;
            }
        }

        private string _password = String.Empty;
        public string Password
        {
            get { return _password; }
            set
            {
                if (value != null && value.Length > CREDUI.MAX_PASSWORD_LENGTH)
                {
                    string message = String.Format(
                        Thread.CurrentThread.CurrentUICulture,
                        "The password has a maximum length of {0} characters.",
                        CREDUI.MAX_PASSWORD_LENGTH);
                    throw new ArgumentException(message, "Password");
                }
                _password = value;
            }
        }

        private bool _saveChecked = false;
        public bool SaveChecked
        {
            get { return _saveChecked; }
            set { _saveChecked = value; }
        }

        private bool _saveDisplayed = true;
        public bool SaveDisplayed
        {
            get { return _saveDisplayed; }
            set { _saveDisplayed = value; }
        }

        private string _target = String.Empty;
        public string Target
        {
            get { return _target; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentException("The target cannot be a null value.", "Target");
                }
                else if (value.Length > CREDUI.MAX_GENERIC_TARGET_LENGTH)
                {
                    string message = String.Format(
                        Thread.CurrentThread.CurrentUICulture,
                        "The target has a maximum length of {0} characters.",
                        CREDUI.MAX_GENERIC_TARGET_LENGTH);
                    throw new ArgumentException(message, "Target");
                }
                _target = value;
            }
        }

        private string _caption = String.Empty;
        public string Caption
        {
            get { return _caption; }
            set
            {
                if (value != null && value.Length > CREDUI.MAX_CAPTION_LENGTH)
                {
                    string message = String.Format(
                        Thread.CurrentThread.CurrentUICulture,
                        "The caption has a maximum length of {0} characters.",
                        CREDUI.MAX_CAPTION_LENGTH);
                    throw new ArgumentException(message, "Caption");
                }
                _caption = value;
            }
        }

        private string _message = String.Empty;
        public string Message
        {
            get { return _message; }
            set
            {
                if (value != null && value.Length > CREDUI.MAX_MESSAGE_LENGTH)
                {
                    string message = String.Format(
                        Thread.CurrentThread.CurrentUICulture,
                        "The message has a maximum length of {0} characters.",
                        CREDUI.MAX_MESSAGE_LENGTH);
                    throw new ArgumentException(message, "Message");
                }
                _message = value;
            }
        }

        private Image _banner = null;
        public Image Banner
        {
            get { return _banner; }
            set
            {
                if (value != null)
                {
                    if (value.Width != ValidBannerWidth)
                    {
                        throw new ArgumentException("The banner image width must be 320 pixels.", "Banner");
                    }
                    if (value.Height != ValidBannerHeight)
                    {
                        throw new ArgumentException("The banner image height must be 60 pixels.", "Banner");
                    }
                }
                _banner = value;
            }
        }

        public DialogResult Show()
        {
            return Show(null, this.Name, this.Password, this.SaveChecked);
        }

        public DialogResult Show(bool saveChecked)
        {
            return Show(null, this.Name, this.Password, saveChecked);
        }

        public DialogResult Show(string name)
        {
            return Show(null, name, this.Password, this.SaveChecked);
        }

        public DialogResult Show(string name, string password)
        {
            return Show(null, name, password, this.SaveChecked);
        }

        public DialogResult Show(string name, string password, bool saveChecked)
        {
            return Show(null, name, password, saveChecked);
        }

        public DialogResult Show(IWin32Window owner)
        {
            return Show(owner, this.Name, this.Password, this.SaveChecked);
        }

        public DialogResult Show(IWin32Window owner, bool saveChecked)
        {
            return Show(owner, this.Name, this.Password, saveChecked);
        }

        public DialogResult Show(IWin32Window owner, string name, string password)
        {
            return Show(owner, name, password, this.SaveChecked);
        }

        public DialogResult Show(IWin32Window owner, string name, string password, bool saveChecked)
        {
            if (Environment.OSVersion.Version.Major < 5)
            {
                throw new ApplicationException("The Credential Management API requires Windows XP / Windows Server 2003 or later.");
            }
            this.Name = name;
            this.Password = password;
            this.SaveChecked = saveChecked;

            return ShowDialog(owner);
        }

        public void Confirm(bool value)
        {
            switch (CREDUI.ConfirmCredentials(this.Target, value))
            {
                case CREDUI.ReturnCodes.NO_ERROR:
                    break;

                case CREDUI.ReturnCodes.ERROR_INVALID_PARAMETER:
                    break;

                default:
                    throw new ApplicationException("Credential confirmation failed.");
            }
        }

        private DialogResult ShowDialog(IWin32Window owner)
        {
            StringBuilder name = new StringBuilder(CREDUI.MAX_USERNAME_LENGTH);
            name.Append(this.Name);

            StringBuilder password = new StringBuilder(CREDUI.MAX_PASSWORD_LENGTH);
            password.Append(this.Password);

            int saveChecked = Convert.ToInt32(this.SaveChecked);

            CREDUI.INFO info = GetInfo(owner);
            CREDUI.FLAGS flags = GetFlags();

            CREDUI.ReturnCodes code = CREDUI.PromptForCredentials(
                ref info,
                this.Target,
                IntPtr.Zero, 0,
                name, CREDUI.MAX_USERNAME_LENGTH,
                password, CREDUI.MAX_PASSWORD_LENGTH,
                ref saveChecked,
                flags
                );

            if (this.Banner != null) GDI32.DeleteObject(info.hbmBanner);

            this.Name = name.ToString();
            this.Password = password.ToString();
            this.SaveChecked = Convert.ToBoolean(saveChecked);

            return GetDialogResult(code);
        }

        private CREDUI.INFO GetInfo(IWin32Window owner)
        {
            CREDUI.INFO info = new CREDUI.INFO();
            if (owner != null) info.hwndParent = owner.Handle;
            info.pszCaptionText = this.Caption;
            info.pszMessageText = this.Message;
            if (this.Banner != null)
            {
                info.hbmBanner = new Bitmap(this.Banner, ValidBannerWidth, ValidBannerHeight).GetHbitmap();
            }
            info.cbSize = Marshal.SizeOf(info);
            return info;
        }

        private CREDUI.FLAGS GetFlags()
        {
            CREDUI.FLAGS flags = CREDUI.FLAGS.GENERIC_CREDENTIALS;

            if (this.AlwaysDisplay) flags = flags | CREDUI.FLAGS.ALWAYS_SHOW_UI;
            if (this.ExcludeCertificates) flags = flags | CREDUI.FLAGS.EXCLUDE_CERTIFICATES;
            if (this.Persist)
            {
                flags = flags | CREDUI.FLAGS.EXPECT_CONFIRMATION;
                if (this.SaveDisplayed) flags = flags | CREDUI.FLAGS.SHOW_SAVE_CHECK_BOX;
            }
            else
            {
                flags = flags | CREDUI.FLAGS.DO_NOT_PERSIST;
            }
            if (this.KeepName) flags = flags | CREDUI.FLAGS.KEEP_USERNAME;

            return flags;
        }

        private DialogResult GetDialogResult(CREDUI.ReturnCodes code)
        {
            DialogResult result;
            switch (code)
            {
                case CREDUI.ReturnCodes.NO_ERROR:
                    result = DialogResult.OK;
                    break;
                case CREDUI.ReturnCodes.ERROR_CANCELLED:
                    result = DialogResult.Cancel;
                    break;
                case CREDUI.ReturnCodes.ERROR_NO_SUCH_LOGON_SESSION:
                    throw new ApplicationException("No such logon session.");
                case CREDUI.ReturnCodes.ERROR_NOT_FOUND:
                    throw new ApplicationException("Not found.");
                case CREDUI.ReturnCodes.ERROR_INVALID_ACCOUNT_NAME:
                    throw new ApplicationException("Invalid account name.");
                case CREDUI.ReturnCodes.ERROR_INSUFFICIENT_BUFFER:
                    throw new ApplicationException("Insufficient buffer.");
                case CREDUI.ReturnCodes.ERROR_INVALID_PARAMETER:
                    throw new ApplicationException("Invalid parameter.");
                case CREDUI.ReturnCodes.ERROR_INVALID_FLAGS:
                    throw new ApplicationException("Invalid flags.");
                default:
                    throw new ApplicationException("Unknown credential result encountered.");
            }
            return result;
        }
    }
}
