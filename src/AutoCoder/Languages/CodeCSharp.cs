using log4net;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Trinet.Core.IO.Ntfs;

namespace AdvancedLogging.AutoCoder
{
    public class CodeCSharp : CodeBase
    {
        public CodeCSharp(System.Collections.Specialized.StringCollection processFiles, List<string> listLogName, Dictionary<string, bool> httpsMethods, Dictionary<string, bool> sqlMethods, string folder, bool saveAsStream) : base(processFiles, listLogName, httpsMethods, sqlMethods, folder, saveAsStream)
        { }

        public bool ProcessFile(FileInfo fi, CodeItems ci, bool backup = false, bool showFile = false, bool scanForILog = false)
        {
            bool addAutoLog = ((ci & CodeItems.AutoLog) == CodeItems.AutoLog);
            bool tryCatch = ((ci & CodeItems.TryCatch) == CodeItems.TryCatch);
            bool constructor = (ci & CodeItems.Constructor) == CodeItems.Constructor;
            bool method = (ci & CodeItems.Method) == CodeItems.Method;
            bool property = (ci & CodeItems.Property) == CodeItems.Property;
            bool @class = (ci & CodeItems.Class) == CodeItems.Class;
            bool retryHttp = (ci & CodeItems.RetryHttp) == CodeItems.RetryHttp;
            bool retrySql = (ci & CodeItems.RetrySql) == CodeItems.RetrySql;

            if (!(((addAutoLog || tryCatch) && (constructor || method || property || @class)) ||
                retryHttp ||
                retrySql))
                throw new ArgumentException("Must select (AddAutoLog and/or TryCatch AND Constructor and/or Method) or Retry Http or Retry Sql.");
            if (fi.Exists)
            {
                bool updated = false;

                Encoding fileEncoding = GetEncoding(fi.FullName);
                String programText = File.ReadAllText(fi.FullName, fileEncoding);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(programText);
                CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
                List<string> lstIzenda = new List<string>() { "Izenda", "AdHoc", "AdHocContext", "Driver" };
                var compilation = CSharpCompilation.Create("Sample", new[] { tree });
                var semanticModel = compilation.GetSemanticModel(tree, true);

                Debug.WriteLine(tree.Length);
                Debug.WriteLine(root.Language);
                if (WalkTree)
                {
                	var walker = new CSharpDeeperWalker();
	                walker.Visit(tree.GetRoot());
                }
                if (root.Language == "C#")
                {
                    var rewriter = new AutoLogTryCatchRewriter(ci);
                    var newRoot = rewriter.Visit(root);

                    if (newRoot != root)
                    {
                        updated = true;
                        programText = newRoot.ToFullString();
                        root = (CompilationUnitSyntax)newRoot;
                    }
                    if (retryHttp || retrySql)
                    {
                        var retryRewriter = new RetryParameterRewriter(HttpsMethods, SqlMethods);
                        var newRootRetry = retryRewriter.Visit(root);
                        if (newRootRetry != root)
                        {
                            updated = true;
                            programText = newRootRetry.ToFullString();
                            root = (CompilationUnitSyntax)newRootRetry;
                        }
                    }

                    if (updated)
                    {
                        if (backup)
                        {
                            if (!Directory.Exists(Path.Combine(fi.DirectoryName, "backup")))
                            {
                                Directory.CreateDirectory(Path.Combine(fi.DirectoryName, "backup"));
                            }
                            fi.CopyTo(Path.Combine(fi.DirectoryName, "backup", fi.Name), true);
                        }

                        bool ctrlKD = false;
                        if (ctrlKD)
                        {
                            SyntaxTree treeNew = CSharpSyntaxTree.ParseText(programText);
                            SyntaxNode rootNew = treeNew.GetRoot().NormalizeWhitespace();
                            programText = rootNew.ToFullString();
                            Debug.WriteLine(programText);

                            //Microsoft.CodeAnalysis.Host.HostServices hs = new 
                            //Microsoft.CodeAnalysis.Workspace workspace = Microsoft.CodeAnalysis.Workspace.GetWorkspaceRegistration("");
                            //var formattedResult = Formatter.Format(treeNew.GetRoot(), workspace);
                        }

                        File.WriteAllText(fi.FullName, programText, fileEncoding);
                        //try
                        //{
                        //    File.WriteAllText(fi.FullName + ":Status", "<Status>Processed</Status>");
                        //}
                        //catch (Exception ex)
                        //{

                        //}
                        if (!ProcessFiles.Contains(fi.FullName.Substring(Folder.Trim().Length + 1)))
                            ProcessFiles.Add(fi.FullName.Substring(Folder.Trim().Length + 1));
                        if (fi.AlternateDataStreamExists("Status"))
                        {
                            Debug.WriteLine("Found Status stream:");

                            AlternateDataStreamInfo s = fi.GetAlternateDataStream("Status", FileMode.Open);
                            using (TextReader reader = s.OpenText())
                            {
                                Debug.WriteLine(reader.ReadToEnd());
                            }
                            if (SaveAsStream)
                            {
                                // Delete the stream:
                                s.Delete();

                                s = fi.GetAlternateDataStream("Status", FileMode.OpenOrCreate);
                                using (StreamWriter sw = new StreamWriter(s.OpenWrite()))
                                {
                                    sw.WriteLine("<Status>Processed</Status>");
                                }
                            }
                        }
                        else if (SaveAsStream)
                        {
                            AlternateDataStreamInfo s = fi.GetAlternateDataStream("Status", FileMode.OpenOrCreate);
                            using (StreamWriter sw = new StreamWriter(s.OpenWrite()))
                            {
                                sw.WriteLine("<Status>Processed</Status>");
                            }
                        }
                    }
                }
                if (showFile && File.Exists(fi.FullName))
                {
                    Process p = new Process();
                    p.StartInfo.FileName = "notepad.exe";
                    p.StartInfo.Arguments = fi.FullName;
                    p.Start();
                }

                return updated;
            }
            return false;
        }

    }
}
