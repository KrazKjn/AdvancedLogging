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
                    var members = tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>();
                    List<string> lstUsings = new List<string>();
                    if (tryCatch)
                        lstUsings.Add("System");
                    if (addAutoLog)
                        lstUsings.Add("AdvancedLogging.Logging");
                    if (retryHttp || retrySql)
                        lstUsings.Add("AdvancedLogging.DAL");
                    string fullItemName = "";
                    foreach (var member in members)
                    {
                        if (member is NamespaceDeclarationSyntax)
                        {
                            var NameSpace = member as NamespaceDeclarationSyntax;

                            //NameSpace.Usings
                        }
                        else if (member is ClassDeclarationSyntax)
                        {
                            ClassDeclarationSyntax cds = member as ClassDeclarationSyntax;
                            for (int i = cds.Members.Count - 1; i >= 0; i--)
                            {
                                if (cds.Members[i] is FieldDeclarationSyntax)
                                {
                                    FieldDeclarationSyntax fds = cds.Members[i] as FieldDeclarationSyntax;

                                    //if (fds.Declaration.Type is ILog || fds.Declaration.Type.ToString().ToLower().Contains("ilog"))
                                    if (fds.Declaration.GetType() is ILog || fds.Declaration.Type.ToString().ToLower().Contains("ilog"))
                                    {
                                        if (!ListLogName.Contains(fds.Declaration.Variables[0].Identifier.Text))
                                            ListLogName.Add(fds.Declaration.Variables[0].Identifier.Text);
                                        programText = programText.Replace(fds.ToFullString(), "");
                                    }
                                }
                            }
                            if (scanForILog)
                                return true;
                        }
                    }
                    int count = 0;
                    foreach (var member in members)
                    {
                        if (member is PropertyDeclarationSyntax propertyDeclarationSyntax)
                        {
                            if ((ci & CodeItems.Property) != CodeItems.Property)
                                continue;

                            fullItemName = propertyDeclarationSyntax.Identifier.Text;
                            Debug.WriteLine("Property: " + fullItemName);
                            //if (propertyDeclarationSyntax.Type is ILog || propertyDeclarationSyntax.Type.ToString().ToLower().Contains("ilog"))
                            if (propertyDeclarationSyntax.GetType() is ILog ||
							    propertyDeclarationSyntax.Type.ToString().ToLower().Contains("ilog") ||
                                propertyDeclarationSyntax.Type.ToString().ToLower().Contains("icommonLogger"))
                            {
                                if (!ListLogName.Contains(propertyDeclarationSyntax.Identifier.Text))
                                    ListLogName.Add(propertyDeclarationSyntax.Identifier.Text);
                                programText = programText.Replace(propertyDeclarationSyntax.ToFullString(), "");
                                //cds.Members.RemoveAt(i);
                            }
                            else
                            {
                                // Do stuff with the symbol here
                                if (propertyDeclarationSyntax.AccessorList == null)
                                {
                                    Debug.WriteLine("Skipping Auto Property: " + propertyDeclarationSyntax.Identifier + ".");
                                }
                                else
                                {
                                    SyntaxList<AccessorDeclarationSyntax> accessors = propertyDeclarationSyntax.AccessorList.Accessors;

                                    AccessorDeclarationSyntax getter = accessors.FirstOrDefault(ad => ad.Kind() == SyntaxKind.GetAccessorDeclaration);
                                    AccessorDeclarationSyntax setter = accessors.FirstOrDefault(ad => ad.Kind() == SyntaxKind.SetAccessorDeclaration);
                                    if (getter == null && setter == null)
                                        continue;
                                    if (getter == null)
                                    {
                                        Debug.WriteLine("Skipping auto implemented 'get'.");
                                    }
                                    else
                                    {
                                        if (getter.Body == null)
                                        {
                                            Debug.WriteLine("Skipping auto implemented 'get'.");
                                        }
                                        else
                                        {
                                            //fullItemName = getter.ToString().Replace(getter.Body.ToString(), "");
                                            Debug.WriteLine("Function [Get]: " + fullItemName);
                                            if (ProcessBody(ref root, member, getter.Body, ref programText, ref lstUsings, ci))
                                            {
                                                updated = true;
                                            }
                                        }
                                    }
                                    if (setter == null)
                                    {
                                        Debug.WriteLine("Skipping auto implemented 'set'.");
                                    }
                                    else
                                    {
                                        if (setter.Body == null)
                                        {
                                            Debug.WriteLine("Skipping auto implemented 'set'.");
                                        }
                                        else
                                        {
                                            //fullItemName = setter.ToString().Replace(setter.Body.ToString(), "");
                                            Debug.WriteLine("Function [Set]: " + fullItemName);
                                            if (ProcessBody(ref root, member, setter.Body, ref programText, ref lstUsings, ci))
                                            {
                                                updated = true;
                                            }
                                        }
                                    }
                                }
                                var fullMethodName = propertyDeclarationSyntax.Identifier.ToFullString();
                            }
                        }
                        else if (member is ConstructorDeclarationSyntax ||
                                    member is MethodDeclarationSyntax)
                        {
                            ConstructorDeclarationSyntax constructorBlockSyntax = null;
                            MethodDeclarationSyntax methodBlockSyntax = null;
                            /*
                                if (member is ClassDeclarationSyntax)
                                if (member is ConstructorDeclarationSyntax)
                                if (member is MethodDeclarationSyntax)
                                if (member is PropertyDeclarationSyntax)
                                if (member is FieldDeclarationSyntax)
                                if (member is NamespaceDeclarationSyntax)
                                if (member is EnumDeclarationSyntax)
                                if (member is EnumMemberDeclarationSyntax)
                                if (member is EventFieldDeclarationSyntax)
                                if (member is DelegateDeclarationSyntax)
                                if (member is InterfaceDeclarationSyntax)
                            */
                            if (member is ConstructorDeclarationSyntax)
                            {
                                if ((ci & CodeItems.Constructor) != CodeItems.Constructor)
                                    continue;

                                constructorBlockSyntax = member as ConstructorDeclarationSyntax;
                                Debug.WriteLine("Method: " + constructorBlockSyntax.Identifier);

                                if (constructorBlockSyntax.Body == null)
                                    continue;
                                fullItemName = constructorBlockSyntax.ToString().Replace(constructorBlockSyntax.Body.ToString(), "");
                                Debug.WriteLine("Function: " + fullItemName);
                                if (ProcessBody(ref root, member, constructorBlockSyntax.Body, ref programText, ref lstUsings, ci))
                                {
                                    updated = true;
                                }
                            }
                            else if (member is MethodDeclarationSyntax)
                            {
                                if ((ci & CodeItems.Method) != CodeItems.Method)
                                    continue;

                                methodBlockSyntax = member as MethodDeclarationSyntax;
                                fullItemName = methodBlockSyntax.Identifier.Text;
                                Debug.WriteLine("Method: " + fullItemName);

                                if (methodBlockSyntax.Body == null)
                                    continue;

                                fullItemName = methodBlockSyntax.ToString().Replace(methodBlockSyntax.Body.ToString(), "");
                                Debug.WriteLine("Function: " + fullItemName);
                                if (ProcessBody(ref root, member, methodBlockSyntax.Body, ref programText, ref lstUsings, ci))
                                {
                                    updated = true;
                                }
                            }
                        }
                        else
                        {
                            Debug.WriteLine(member.GetType().FullName);
                            if (member is NamespaceDeclarationSyntax)
                            { }
                            else if (member is InterfaceDeclarationSyntax)
                            { }
                            else if (member is ClassDeclarationSyntax)
                            {
                                ClassDeclarationSyntax cds = member as ClassDeclarationSyntax;
                                if (cds.BaseList != null)
                                {
                                    if ((ci & CodeItems.Class) == CodeItems.Class)
                                    {

                                        if (cds.BaseList.ToString().Contains(" ServiceBase"))
                                        {
                                            programText = programText.Replace(" ServiceBase", " WinServiceBase");
                                            if (!lstUsings.Contains("AdvancedLogging.BusinessLogic"))
                                                lstUsings.Add("AdvancedLogging.BusinessLogic");
                                            if (!ListLogName.Contains("Log"))
                                                ListLogName.Add("Log");
                                        }
                                        if (cds.BaseList.ToString().Contains("System.Web.HttpApplication"))
                                        {
                                            programText = programText.Replace("System.Web.HttpApplication", "WebServiceBase");
                                            if (!lstUsings.Contains("AdvancedLogging.BusinessLogic"))
                                                lstUsings.Add("AdvancedLogging.BLL");
                                        }
                                        if (cds.BaseList.ToString().Contains("System.Web.UI.Page"))
                                        {
                                            programText = programText.Replace("System.Web.UI.Page", "BasePage");
                                        }
                                    }
                                    for (int i = cds.Members.Count - 1; i >= 0; i--)
                                    {
                                        if (cds.Members[i] is FieldDeclarationSyntax)
                                        {
                                            FieldDeclarationSyntax fds = cds.Members[i] as FieldDeclarationSyntax;

                                            //if (fds.Declaration.Type is ILog || fds.Declaration.Type.ToString().ToLower().Contains("ilog"))
                                            if (fds.Declaration.GetType() is ILog || fds.Declaration.Type.ToString().ToLower().Contains("ilog"))
                                            {
                                                if (!ListLogName.Contains(fds.Declaration.Variables[0].Identifier.Text))
                                                    ListLogName.Add(fds.Declaration.Variables[0].Identifier.Text);
                                                programText = programText.Replace(fds.ToFullString(), "");
                                                //cds.Members.RemoveAt(i);
                                            }
                                            // Do stuff with the symbol here
                                        }
                                    }
                                }
                            }
                            else if (member is FieldDeclarationSyntax)
                            {
                                FieldDeclarationSyntax fds = member as FieldDeclarationSyntax;

                                //if (fds.Declaration.Type is ILog)
                                if (fds.Declaration.GetType() is ILog)
                                {
                                    //m_dicLogName.Add(fds.Declaration.Id)
                                    //cds.Members.Remove(vMember);
                                }
                            }
                            else if (member is EnumDeclarationSyntax)
                            { }
                            else if (member is EnumMemberDeclarationSyntax)
                            { }
                            else if (member is EventFieldDeclarationSyntax)
                            { }
                            else if (member is DelegateDeclarationSyntax)
                            { }
                        }
                        count++;
                        UpdateProgress(fi.FullName, "Code", count, members.Count());
                        Application.DoEvents();
                    }
                    if (retryHttp)
                    {
                        if (HttpsMethods.Keys.Any(programText.Contains))
                        {
                            foreach (string key in HttpsMethods.Keys)
                            {
                                if (!HttpsMethods[key])
                                    continue;
                                string httpCommand = "." + key + "(";
                                int pos = 0;
                                int items = 0;
                                if (ProgressEventsEnabled)
                                {
                                    while (pos > -1)
                                    {
                                        pos = programText.IndexOf(httpCommand, pos + 1);
                                        if (pos > -1)
                                        {
                                            int posClose = programText.IndexOf(")", pos);

                                            if (programText.Substring(pos, posClose - pos).Contains(">("))
                                            {
                                                posClose = programText.IndexOf(")", posClose + 1);
                                            }
                                            if (!programText.Substring(pos, posClose - pos).Contains("MaxAutoRetriesHttp"))
                                            {
                                                if (programText.Substring(pos, posClose - pos + 1) == httpCommand + ")")
                                                {
                                                    pos += "ApplicationSettings.MaxAutoRetriesHttp, ApplicationSettings.AutoRetrySleepMsHttp, ApplicationSettings.AutoTimeoutIncrementMsHttp)".Length;
                                                }
                                                else
                                                {
                                                    pos += ", ApplicationSettings.MaxAutoRetriesHttp, ApplicationSettings.AutoRetrySleepMsHttp, ApplicationSettings.AutoTimeoutIncrementMsHttp)".Length;
                                                }
                                                items++;
                                            }
                                        }
                                    }
                                }
                                pos = 0;
                                while (pos > -1)
                                {
                                    pos = programText.IndexOf(httpCommand, pos + 1);
                                    if (pos > -1)
                                    {
                                        int posClose = programText.IndexOf(")", pos);

                                        if (programText.Substring(pos, posClose - pos).Contains(">("))
                                        {
                                            posClose = programText.IndexOf(")", posClose + 1);
                                        }
                                        if (!programText.Substring(pos, posClose - pos).Contains("MaxAutoRetriesHttp"))
                                        {
                                            if (programText.Substring(pos, posClose - pos + 1) == httpCommand + ")")
                                            {
                                                programText = programText.Replace(programText.Substring(pos, posClose - pos + 1), programText.Substring(pos, posClose - pos) + "ApplicationSettings.MaxAutoRetriesHttp, ApplicationSettings.AutoRetrySleepMsHttp, ApplicationSettings.AutoTimeoutIncrementMsHttp)");
                                                pos += "ApplicationSettings.MaxAutoRetriesHttp, ApplicationSettings.AutoRetrySleepMsHttp, ApplicationSettings.AutoTimeoutIncrementMsHttp)".Length;
                                            }
                                            else
                                            {
                                                programText = programText.Replace(programText.Substring(pos, posClose - pos + 1), programText.Substring(pos, posClose - pos) + ", ApplicationSettings.MaxAutoRetriesHttp, ApplicationSettings.AutoRetrySleepMsHttp, ApplicationSettings.AutoTimeoutIncrementMsHttp)");
                                                pos += ", ApplicationSettings.MaxAutoRetriesHttp, ApplicationSettings.AutoRetrySleepMsHttp, ApplicationSettings.AutoTimeoutIncrementMsHttp)".Length;
                                            }
                                            count++;
                                            UpdateProgress(fi.FullName, "Http", count, items);
                                            Application.DoEvents();
                                            updated = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (retrySql)
                    {
                        //if (strProgramText.Contains(".ExecuteNonQuery(")
                        //    || strProgramText.Contains(".ExecuteReader(")
                        //    || strProgramText.Contains(".ExecuteScalar(")
                        //    || strProgramText.Contains(".Fill("))
                        //{
                        //    foreach (string strSqlCommand in new string[] { ".ExecuteNonQuery(", ".ExecuteReader(", ".ExecuteScalar(", ".Fill(" })

                        if (SqlMethods.Keys.Any(programText.Contains))
                        {
                            foreach (string key in SqlMethods.Keys)
                            {
                                if (!SqlMethods[key])
                                    continue;
                                string sqlCommand = "." + key + "(";
                                int pos = 0;
                                int items = 0;
                                if (ProgressEventsEnabled)
                                {
                                    while (pos > -1)
                                    {
                                        pos = programText.IndexOf(sqlCommand, pos + 1);
                                        if (pos > -1)
                                        {
                                            //                . - iPos = 250
                                            //                  
                                            //       SqlHelper.ExecuteScalar
                                            if (programText.Substring(pos - "Helper".Length, "Helper".Length).ToLower() == "Helper".ToLower())
                                            {
                                                // Skip Processing "*Helper"
                                                Debug.WriteLine("Skip Processing '*Helper'");
                                            }
                                            else if (programText.Substring(pos - "x => x.".Length, "x => x.".Length).Contains(" => "))
                                            {
                                                // x => x.ExecuteReader
                                                // Skip Processing "*Helper"
                                                Debug.WriteLine("Skip Processing '*Helper'");
                                            }
                                            // SqlHelperStatic.ExecuteScalar
                                            // SqlHelperStatic.ExecuteScalar
                                            else if (programText.Substring(pos - "SqlHelperStatic".Length, "SqlHelperStatic".Length) == "SqlHelperStatic")
                                            {
                                                // Skip Processing "SqlHelperStatic"
                                                Debug.WriteLine("Skip Processing 'SqlHelperStatic'");
                                            }
                                            // Izenda.AdHoc.AdHocContext.Driver
                                            else if (lstIzenda.Any(programText.Substring(pos - "Izenda.AdHoc.AdHocContext.Driver".Length, "Izenda.AdHoc.AdHocContext.Driver".Length).Contains))
                                            {
                                                // Skip Processing "Izenda"
                                                Debug.WriteLine("Skip Processing ''");
                                            }
                                            else
                                            {
                                                int posClose = programText.IndexOf(")", pos);
                                                if (!programText.Substring(pos, posClose - pos).Contains("MaxAutoRetriesSql"))
                                                {
                                                    if (programText.Substring(pos, posClose - pos + 1) == sqlCommand + ")")
                                                    {
                                                        pos += "ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql)".Length;
                                                    }
                                                    else
                                                    {
                                                        pos += ", ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql)".Length;
                                                    }
                                                    items++;
                                                }
                                            }
                                        }
                                    }
                                }
                                pos = 0;
                                while (pos > -1)
                                {
                                    pos = programText.IndexOf(sqlCommand, pos + 1);
                                    if (pos > -1)
                                    {
                                        //                . - iPos = 250
                                        //                  
                                        //       SqlHelper.ExecuteScalar
                                        if (programText.Substring(pos - "Helper".Length, "Helper".Length).ToLower() == "Helper".ToLower())
                                        {
                                            // Skip Processing "*Helper"
                                            Debug.WriteLine("Skip Processing '*Helper'");
                                        }
                                        else if (programText.Substring(pos - "x => x.".Length, "x => x.".Length).Contains(" => "))
                                        {
                                            // x => x.ExecuteReader
                                            // Skip Processing "*Helper"
                                            Debug.WriteLine("Skip Processing '*Helper'");
                                        }
                                        // SqlHelperStatic.ExecuteScalar
                                        // SqlHelperStatic.ExecuteScalar
                                        else if (programText.Substring(pos - "SqlHelperStatic".Length, "SqlHelperStatic".Length) == "SqlHelperStatic")
                                        {
                                            // Skip Processing "SqlHelperStatic"
                                            Debug.WriteLine("Skip Processing 'SqlHelperStatic'");
                                        }
                                        // Izenda.AdHoc.AdHocContext.Driver
                                        else if (lstIzenda.Any(programText.Substring(pos - "Izenda.AdHoc.AdHocContext.Driver".Length, "Izenda.AdHoc.AdHocContext.Driver".Length).Contains))
                                        {
                                            // Skip Processing "Izenda"
                                            Debug.WriteLine("Skip Processing ''");
                                        }
                                        else
                                        {
                                            int posClose = programText.IndexOf(")", pos);
                                            if (!programText.Substring(pos, posClose - pos).Contains("MaxAutoRetriesSql"))
                                            {
                                                if (programText.Substring(pos, posClose - pos + 1) == sqlCommand + ")")
                                                {
                                                    programText = programText.Replace(programText.Substring(pos, posClose - pos + 1), programText.Substring(pos, posClose - pos) + "ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql)");
                                                    pos += "ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql)".Length;
                                                }
                                                else
                                                {
                                                    programText = programText.Replace(programText.Substring(pos, posClose - pos + 1), programText.Substring(pos, posClose - pos) + ", ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql)");
                                                    pos += ", ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql)".Length;
                                                }
                                                count++;
                                                UpdateProgress(fi.FullName, "Sql", count, items);
                                                Application.DoEvents();
                                                updated = true;
                                            }
                                        }
                                    }
                                }
                            }
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

        private bool ProcessBody(ref CompilationUnitSyntax root, MemberDeclarationSyntax member, BlockSyntax oBody, ref string programText, ref List<string> usings, CodeItems ci, bool addLineLogging = false)
        {
            bool updated = false;
            bool addAutoLog = ((ci & CodeItems.AutoLog) == CodeItems.AutoLog);
            bool tryCatch = ((ci & CodeItems.TryCatch) == CodeItems.TryCatch);
            bool constructor = (ci & CodeItems.Constructor) == CodeItems.Constructor;
            bool method = (ci & CodeItems.Method) == CodeItems.Method;
            bool property = (ci & CodeItems.Property) == CodeItems.Property;

            if (oBody.Statements.Count == 0)
                return updated;
            var vBody = oBody.ToString();
#if DEBUG
            if (addLineLogging)
            {
                // Line for Line Debug Statements Testing
                string tempReplacement = "";
                foreach (var statement in oBody.Statements)
                {
                    tempReplacement += "vAutoLogFunction.WriteDebug(\"" + statement.ToString().Replace("\"", "\\\"") + "\");" + Environment.NewLine;
                    tempReplacement += statement.ToString() + Environment.NewLine;
                }
                Debug.WriteLine(tempReplacement);
            }
#endif
            if ((ci & CodeItems.ModifyEmptyBody) == CodeItems.ModifyEmptyBody)
            {
                if (vBody.Replace(" ", "").Replace(Environment.NewLine, "") == (oBody.OpenBraceToken.ToString() + oBody.CloseBraceToken.ToString()))
                    return updated;
            }

            String replaceFind = "";
            String replaceWith = "";
            String variables = "";
#if INDENT_SPACES
            Int16 baseIndenting = 3;
            string baseIndentText = "    ";
#else
            Int16 baseIndenting = 3;
            string baseIndentText = "\t";
#endif

            if (member is ConstructorDeclarationSyntax constructorDeclarationSyntax)
            {
                if (constructorDeclarationSyntax != null)
                {
                    if (constructorDeclarationSyntax.ParameterList != null)
                    {
                        foreach (var parameter in constructorDeclarationSyntax.ParameterList.Parameters)
                        {
                            if (parameter.Modifiers != null)
                            {
                                // Need to exclude OUT parameters
                                if (parameter.Modifiers.Any(p => p.Text == "out"))
                                    continue;
                            }
                            if (variables == "")
                                variables = "new " + oBody.OpenBraceToken.ToString() + " ";
                            else
                                variables += ", ";
                            variables += parameter.Identifier.Value;
                            Debug.WriteLine((parameter.Identifier.Value));
                        }

                        if (variables != "")
                            variables += " " + oBody.CloseBraceToken.ToString() + "";
                    }
                }
            }
            else if (member is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                if (methodDeclarationSyntax.ParameterList != null)
                {
                    foreach (var parameter in methodDeclarationSyntax.ParameterList.Parameters)
                    {
                        if (parameter.Modifiers != null)
                        {
                            // Need to exclude OUT parameters
                            if (parameter.Modifiers.Any(p => p.Text == "out"))
                                continue;
                        }
                        if (variables == "")
                            variables = "new " + oBody.OpenBraceToken.ToString() + " ";
                        else
                            variables += ", ";
                        variables += parameter.Identifier.Value;
                        Debug.WriteLine((parameter.Identifier.Value));
                    }

                    if (variables != "")
                        variables += " " + oBody.CloseBraceToken.ToString() + "";
                }
            }
            else if (member is PropertyDeclarationSyntax)
            {
                baseIndenting = 4;
            }
            replaceFind = vBody;
            string body = vBody;
            if (tryCatch && !vBody.Replace(" ", "").Contains("catch(ExceptionexOuter)")) //! body.Replace(" ", "").Replace(Environment.NewLine, "").StartsWith(oBody.OpenBraceToken.ToString() + "try"))
            {
                if (vBody.Replace(" ", "").Replace(Environment.NewLine, "").StartsWith(oBody.OpenBraceToken.ToString() + "using(varvAutoLogFunction=newAutoLogFunction"))
                {
                    if (oBody.Statements[0] is UsingStatementSyntax usingStatementSyntax)
                    {
                        bool addTry = true;
                        if (usingStatementSyntax.Statement is BlockSyntax bs)
                        {
                            Debug.WriteLine(bs.Statements[0].ToString());
                            addTry = !(bs.Statements[0].ToString().StartsWith("try") && bs.Statements[0].ToString().Replace(" ", "").Contains("catch(ExceptionexOuter)"));
                        }
                        else if (usingStatementSyntax.Statement.ToString().Replace(" ", "").Replace(Environment.NewLine, "").StartsWith(oBody.OpenBraceToken.ToString() + "try"))
                        {
                            addTry = false;
                        }
                        if (addTry)
                        {
                            Debug.WriteLine("Stop");
                            replaceWith = oBody.OpenBraceToken.ToString() + Environment.NewLine;
                            replaceWith += Indention(baseIndenting, baseIndentText) + "try" + Environment.NewLine;
                            string uSS = usingStatementSyntax.Statement.ToString().Replace(Environment.NewLine, Environment.NewLine + Indention(1, baseIndentText)) + Environment.NewLine;
                            foreach (string logName in ListLogName)
                            {
                                if (uSS.Contains(logName))
                                {
                                    foreach (string token in new string[] { ".", "?." })
                                    {
                                        uSS = uSS.Replace(logName + token + "Info(", "vAutoLogFunction.WriteLog(");
                                        uSS = uSS.Replace(logName + token + "Warn(", "vAutoLogFunction.WriteWarn(");
                                        uSS = uSS.Replace(logName + token + "Error(", "vAutoLogFunction.WriteError(");
                                        uSS = uSS.Replace(logName + token + "Debug(", "vAutoLogFunction.WriteDebug(");
                                        uSS = uSS.Replace(logName + token + "InfoFormat(", "vAutoLogFunction.WriteLogFormat(");
                                        uSS = uSS.Replace(logName + token + "WarnFormat(", "vAutoLogFunction.WriteWarnFormat(");
                                        uSS = uSS.Replace(logName + token + "ErrorFormat(", "vAutoLogFunction.WriteErrorFormat(");
                                        uSS = uSS.Replace(logName + token + "DebugFormat(", "vAutoLogFunction.WriteDebugFormat(");
                                    }
                                }
                            }
                            replaceWith += Indention(baseIndenting, baseIndentText) + uSS;
                            replaceWith += Indention(baseIndenting, baseIndentText) + "catch (Exception exOuter)" + Environment.NewLine;
                            replaceWith += Indention(baseIndenting, baseIndentText) + oBody.OpenBraceToken.ToString() + Environment.NewLine;
                            if (variables.Length == 0)
                                replaceWith += Indention(baseIndenting + 2, baseIndentText) + "vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), error:true, _ex:exOuter);" + Environment.NewLine;
                            else
                                replaceWith += Indention(baseIndenting + 2, baseIndentText) + "vAutoLogFunction.LogFunction(" + variables + "System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);" + Environment.NewLine;
                            replaceWith += Indention(baseIndenting + 2, baseIndentText) + "throw;" + Environment.NewLine;
                            replaceWith += Indention(baseIndenting, baseIndentText) + oBody.CloseBraceToken.ToString() + Environment.NewLine;
                            replaceWith += Indention(baseIndenting - 1, baseIndentText) + oBody.CloseBraceToken.ToString();
                            replaceWith = body.Replace(usingStatementSyntax.Statement.ToString(), replaceWith);
                            body = replaceWith;
                        }
                    }
                    Debug.WriteLine("Stop");
                }
                else
                {
                    replaceWith = oBody.OpenBraceToken.ToString() + Environment.NewLine;
                    replaceWith += Indention(baseIndenting, baseIndentText) + "try" + Environment.NewLine;
                    body = body.Replace(Environment.NewLine, Environment.NewLine + Indention(1, baseIndentText)) + Environment.NewLine;
                    foreach (string logName in ListLogName)
                    {
                        if (body.Contains(logName))
                        {
                            foreach (string token in new string[] { ".", "?." })
                            {
                                if (addAutoLog)
                                {
                                    body = body.Replace(logName + token + "Info(", "vAutoLogFunction.WriteLog(");
                                    body = body.Replace(logName + token + "Warn(", "vAutoLogFunction.WriteWarn(");
                                    body = body.Replace(logName + token + "Error(", "vAutoLogFunction.WriteError(");
                                    body = body.Replace(logName + token + "Debug(", "vAutoLogFunction.WriteDebug(");
                                    body = body.Replace(logName + token + "InfoFormat(", "vAutoLogFunction.WriteLogFormat(");
                                    body = body.Replace(logName + token + "WarnFormat(", "vAutoLogFunction.WriteWarnFormat(");
                                    body = body.Replace(logName + token + "ErrorFormat(", "vAutoLogFunction.WriteErrorFormat(");
                                    body = body.Replace(logName + token + "DebugFormat(", "vAutoLogFunction.WriteDebugFormat(");
                                }
                                else
                                {
                                    body = body.Replace(logName + token + "Info(", "LoggingUtils.WriteLog(");
                                    body = body.Replace(logName + token + "Warn(", "LoggingUtils.WriteWarn(");
                                    body = body.Replace(logName + token + "Error(", "LoggingUtils.WriteError(");
                                    body = body.Replace(logName + token + "Debug(", "LoggingUtils.WriteDebug(");
                                    body = body.Replace(logName + token + "InfoFormat(", "LoggingUtils.WriteLogFormat(");
                                    body = body.Replace(logName + token + "WarnFormat(", "LoggingUtils.WriteWarnFormat(");
                                    body = body.Replace(logName + token + "ErrorFormat(", "LoggingUtils.WriteErrorFormat(");
                                    body = body.Replace(logName + token + "DebugFormat(", "LoggingUtils.WriteDebugFormat(");
                                }
                            }
                        }
                    }
                    replaceWith += Indention(baseIndenting, baseIndentText) + body;
                    replaceWith += Indention(baseIndenting, baseIndentText) + "catch (Exception exOuter)" + Environment.NewLine;
                    replaceWith += Indention(baseIndenting, baseIndentText) + oBody.OpenBraceToken.ToString() + Environment.NewLine;
                    if (addAutoLog)
                    {
                        replaceWith += Indention(baseIndenting + 1, baseIndentText) + "vAutoLogFunction.LogFunction(" + variables + ", System.Reflection.MethodBase.GetCurrentMethod(), error:true, _ex:exOuter);" + Environment.NewLine;
                    }
                    else
                    {
                        replaceWith += Indention(baseIndenting + 1, baseIndentText) + "LoggingUtils.LogFunction(" + variables + ", System.Reflection.MethodBase.GetCurrentMethod(), error:true, _ex:exOuter);" + Environment.NewLine;
                    }
                    replaceWith += Indention(baseIndenting + 1, baseIndentText) + "throw;" + Environment.NewLine;
                    replaceWith += Indention(baseIndenting, baseIndentText) + oBody.CloseBraceToken.ToString() + Environment.NewLine;
                    replaceWith += Indention(baseIndenting - 1, baseIndentText) + oBody.CloseBraceToken.ToString();
                    body = replaceWith;
                }
            }
            if (addAutoLog && !vBody.Replace(" ", "").Replace(Environment.NewLine, "").StartsWith(oBody.OpenBraceToken.ToString() + "using(varvAutoLogFunction=newAutoLogFunction"))
            {
                if (vBody.Contains("AutoLogFunction("))
                {
                    // Add strReplaceWith inside existing AutoLogFunction(
                    Debug.WriteLine("Stop");
                }
                else
                {
                    replaceWith = oBody.OpenBraceToken.ToString() + Environment.NewLine;
                    replaceWith += Indention(baseIndenting, baseIndentText) + "using (var vAutoLogFunction = new AutoLogFunction(" + variables + "))" + Environment.NewLine;
                    body = body.Replace(Environment.NewLine, Environment.NewLine + Indention(1, baseIndentText)) + Environment.NewLine;
                    foreach (string logName in ListLogName)
                    {
                        if (body.Contains(logName))
                        {
                            foreach (string token in new string[] { ".", "?." })
                            {
                                if (addAutoLog)
                                {
                                    body = body.Replace(logName + token + "Info(", "vAutoLogFunction.WriteLog(");
                                    body = body.Replace(logName + token + "Warn(", "vAutoLogFunction.WriteWarn(");
                                    body = body.Replace(logName + token + "Error(", "vAutoLogFunction.WriteError(");
                                    body = body.Replace(logName + token + "Debug(", "vAutoLogFunction.WriteDebug(");
                                    body = body.Replace(logName + token + "InfoFormat(", "vAutoLogFunction.WriteLogFormat(");
                                    body = body.Replace(logName + token + "WarnFormat(", "vAutoLogFunction.WriteWarnFormat(");
                                    body = body.Replace(logName + token + "ErrorFormat(", "vAutoLogFunction.WriteErrorFormat(");
                                    body = body.Replace(logName + token + "DebugFormat(", "vAutoLogFunction.WriteDebugFormat(");
                                }
                                else
                                {
                                    body = body.Replace(logName + token + "Info(", "LoggingUtils.WriteLog(");
                                    body = body.Replace(logName + token + "Warn(", "LoggingUtils.WriteWarn(");
                                    body = body.Replace(logName + token + "Error(", "LoggingUtils.WriteError(");
                                    body = body.Replace(logName + token + "Debug(", "LoggingUtils.WriteDebug(");
                                    body = body.Replace(logName + token + "InfoFormat(", "LoggingUtils.WriteLogFormat(");
                                    body = body.Replace(logName + token + "WarnFormat(", "LoggingUtils.WriteWarnFormat(");
                                    body = body.Replace(logName + token + "ErrorFormat(", "LoggingUtils.WriteErrorFormat(");
                                    body = body.Replace(logName + token + "DebugFormat(", "LoggingUtils.WriteDebugFormat(");
                                }
                            }
                        }
                    }
                    replaceWith += Indention(baseIndenting, baseIndentText) + body;
                    replaceWith += Indention(baseIndenting - 1, baseIndentText) + oBody.CloseBraceToken.ToString();
                }
            }
            else if (replaceWith.Length > 0)
            {
                if (vBody.Replace(" ", "").Replace(Environment.NewLine, "").StartsWith(oBody.OpenBraceToken.ToString() + "using(varvAutoLogFunction=newAutoLogFunction"))
                {
                    Debug.WriteLine("Stop");
                }
            }

            if (replaceWith.Length > 0)
            {
                foreach (string @using in usings)
                {
                    NameSyntax uMHSUtils = SyntaxFactory.IdentifierName(@using);

                    string usingStatement = "using " + uMHSUtils.ToFullString() + ";";

                    if (!root.Usings.Any(x => x.ToFullString() == usingStatement) &&
                        !programText.Contains(usingStatement))
                    {
                        UsingDirectiveSyntax obj = SyntaxFactory.UsingDirective(uMHSUtils);
                        var newusing = root.Usings.Add(obj);
                        String strUsings = "";
                        if (root.Usings.Count > 0)
                            strUsings += "using " + root.Usings[root.Usings.Count - 1].Name + ";" + Environment.NewLine;

                        Debug.WriteLine("Adding: " + usingStatement + " ...");
                        if (strUsings.Length > 0)
                        {
                            programText = programText.Replace(strUsings, strUsings + usingStatement + Environment.NewLine);
                        }
                        else
                        {
                            programText = usingStatement + Environment.NewLine + Environment.NewLine + programText;
                        }
                    }
                    else
                    {
                        if (!root.Usings.ToString().Contains(usingStatement) && !programText.Contains(usingStatement))
                        {
                            Debugger.Break();
                        }
                    }
                }
                programText = programText.Replace(replaceFind, replaceWith);
                updated = true;

                Debug.WriteLine("Find:     " + replaceFind);
                Debug.WriteLine("Replace:  " + replaceWith);
            }
            return updated;
        }

    }
}
