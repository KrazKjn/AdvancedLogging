using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace AdvancedLogging.AutoCoder
{
    /// <summary>
    /// The CodeBase class provides functionality to manipulate code files, track progress, and manage logging and HTTP/SQL methods.
    /// </summary>
    public class CodeBase
    {
        /// <summary>
        /// Event arguments for progress changed events.
        /// </summary>
        public class ProgressChangedEventArgs : EventArgs
        {
            public string FileName { get; private set; }
            public string ItemName { get; private set; }
            public int Current { get; private set; }
            public int Total { get; private set; }

            /// <summary>
            /// Initializes a new instance of the ProgressChangedEventArgs class.
            /// </summary>
            /// <param name="_FileName">The name of the file being processed.</param>
            /// <param name="_ItemName">The name of the item being processed.</param>
            /// <param name="_Value">The current progress value.</param>
            /// <param name="_Total">The total progress value.</param>
            public ProgressChangedEventArgs(string _FileName, string _ItemName, int _Value, int _Total)
            {
                FileName = _FileName ?? throw new ArgumentNullException(nameof(_FileName));
                ItemName = _ItemName ?? throw new ArgumentNullException(nameof(_ItemName));
                Current = _Value;
                Total = _Total;
            }
        }

        /// <summary>
        /// Event triggered when progress changes.
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// List of log names.
        /// </summary>
        public List<string> ListLogName { get; internal set; } = new List<string>();

        /// <summary>
        /// List of detailed logging functions.
        /// </summary>
        public List<string> DetailedLoggingFunctions { get; internal set; } = new List<string>();

        /// <summary>
        /// Dictionary of HTTP methods.
        /// </summary>
        public Dictionary<string, bool> HttpsMethods { get; internal set; } = new Dictionary<string, bool>();

        /// <summary>
        /// Dictionary of SQL methods.
        /// </summary>
        public Dictionary<string, bool> SqlMethods { get; internal set; } = new Dictionary<string, bool>();

        /// <summary>
        /// Collection of files to process.
        /// </summary>
        public StringCollection ProcessFiles { get; internal set; }

        /// <summary>
        /// Indicates whether to save as stream.
        /// </summary>
        public bool SaveAsStream { get; internal set; }

        /// <summary>
        /// The folder to process.
        /// </summary>
        public string Folder { get; internal set; }

        private bool _walkTree = false;

        /// <summary>
        /// Indicates whether to walk the tree.
        /// </summary>
        public bool WalkTree
        {
            get { return _walkTree; }
            internal set { _walkTree = value; }
        }

        /// <summary>
        /// Initializes a new instance of the CodeBase class.
        /// </summary>
        /// <param name="processFiles">The collection of files to process.</param>
        /// <param name="listLogName">The list of log names.</param>
        /// <param name="httpsMethods">The dictionary of HTTP methods.</param>
        /// <param name="sqlMethods">The dictionary of SQL methods.</param>
        /// <param name="folder">The folder to process.</param>
        /// <param name="saveAsStream">Indicates whether to save as stream.</param>
        public CodeBase(StringCollection processFiles, List<string> listLogName, Dictionary<string, bool> httpsMethods, Dictionary<string, bool> sqlMethods, string folder, bool saveAsStream)
        {
            ProcessFiles = processFiles ?? throw new ArgumentNullException(nameof(processFiles));
            ListLogName = listLogName ?? throw new ArgumentNullException(nameof(listLogName));
            HttpsMethods = httpsMethods ?? throw new ArgumentNullException(nameof(httpsMethods));
            SqlMethods = sqlMethods ?? throw new ArgumentNullException(nameof(sqlMethods));
            Folder = folder ?? throw new ArgumentNullException(nameof(folder));
            SaveAsStream = saveAsStream;
        }

        /// <summary>
        /// Updates the progress and triggers the ProgressChanged event.
        /// </summary>
        /// <param name="_FileName">The name of the file being processed.</param>
        /// <param name="_ItemName">The name of the item being processed.</param>
        /// <param name="_Current">The current progress value.</param>
        /// <param name="_Total">The total progress value.</param>
        protected void UpdateProgress(string _FileName, string _ItemName, int _Current, int _Total)
        {
            ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(_FileName, _ItemName, _Current, _Total));
        }

        /// <summary>
        /// Gets a value indicating whether progress events are enabled.
        /// </summary>
        public bool ProgressEventsEnabled => ProgressChanged != null;

        /// <summary>
        /// Generates an indentation string based on the specified levels and indent string.
        /// </summary>
        /// <param name="levels">The number of indentation levels.</param>
        /// <param name="indentString">The string to use for indentation.</param>
        /// <returns>The generated indentation string.</returns>
        protected string Indention(int levels, string indentString = "")
        {
            if (levels == 0 || string.IsNullOrEmpty(indentString))
                return string.Empty;

            var sb = new StringBuilder();
            foreach (char ch in indentString)
            {
                sb.Append(new string(ch, levels));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets the encoding of the specified file.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <returns>The encoding of the file.</returns>
        protected static Encoding GetEncoding(string filename)
        {
            // This is a direct quote from MSDN:  
            // The CurrentEncoding value can be different after the first
            // call to any Read method of StreamReader, since encoding
            // autodetection is not done until the first call to a Read method.

            using (var reader = new StreamReader(filename, Encoding.Default, true))
            {
                if (reader.Peek() >= 0) // you need this!
                    reader.Read();

                return reader.CurrentEncoding;
            }
        }
    }
}
