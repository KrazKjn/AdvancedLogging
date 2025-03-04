using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
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
    public class CodeVisualBasic : CodeBase
    {
        public CodeVisualBasic(System.Collections.Specialized.StringCollection processFiles, List<string> listLogName, Dictionary<string, bool> httpsMethods, Dictionary<string, bool> sqlMethods, string folder, bool saveAsStream) : base(processFiles, listLogName, httpsMethods, sqlMethods, folder, saveAsStream)
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
                SyntaxTree tree = VisualBasicSyntaxTree.ParseText(programText);
                CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
                List<string> lstIzenda = new List<string>() { "Izenda", "AdHoc", "AdHocContext", "Driver" };
                var compilation = VisualBasicCompilation.Create("Sample", new[] { tree });
                var semanticModel = compilation.GetSemanticModel(tree, true);

                Debug.WriteLine(tree.Length);
                Debug.WriteLine(root.Language);
                if (WalkTree)
                {
                    var walker = new VBDeeperWalker();
                    walker.Visit(tree.GetRoot());
                }
                if (root.Language == "Visual Basic")
                {
                    var members = tree.GetRoot().DescendantNodes().OfType<StatementSyntax>();
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
                        if (member is NamespaceBlockSyntax namespaceBlockSyntax)
                        {
                            //NameSpace.Usings
                        }
                        else if (member is ImportsStatementSyntax importsStatementSyntax)
                        {
#if DEBUG
                            ImportsCollector ic = new ImportsCollector();
                            ic.Visit(root);
                            foreach(var statement in ic.Imports)
                            {
                                Debug.WriteLine(statement);
                            }
#endif							
                            //NameSpace.Usings
                        }
                        else if (member is Microsoft.CodeAnalysis.VisualBasic.Syntax.ClassBlockSyntax classBlockSyntax) // ClassDeclarationSyntax)
                        {
                            fullItemName = classBlockSyntax.ClassStatement.Identifier.Text;
                            for (int i = classBlockSyntax.Members.Count - 1; i >= 0; i--)
                            {
                                if (classBlockSyntax.Members[i] is FieldDeclarationSyntax)
                                {
                                    FieldDeclarationSyntax fds = classBlockSyntax.Members[i] as FieldDeclarationSyntax;

                                    if (fds.IsType("ILog") ||
                                        fds.IsType("ICommonLogger"))
                                    {
                                        // Fix This - Check this
                                        //if (!m_lstLogName.Contains(cds.ClassStatement.Identifier.Text))
                                        //    m_lstLogName.Add(cds.ClassStatement.Identifier.Text);
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
                        if (member is PropertyBlockSyntax propertyBlockSyntax)
                        {
                            if ((ci & CodeItems.Property) != CodeItems.Property)
                                continue;

                            fullItemName = propertyBlockSyntax.PropertyStatement.Identifier.Text;
                            Debug.WriteLine("Property: " + fullItemName);

                            if (propertyBlockSyntax.IsType("ILog") ||
                                propertyBlockSyntax.IsType("ICommonLogger"))
                            {
                                // Fix This
                                if (!ListLogName.Contains(propertyBlockSyntax.PropertyStatement.Identifier.Text))
                                    ListLogName.Add(propertyBlockSyntax.PropertyStatement.Identifier.Text);
                                programText = programText.Replace(propertyBlockSyntax.ToFullString(), "");
                            }
                            else
                            {
                                // Do stuff with the symbol here
                                if (propertyBlockSyntax.Accessors == null)
                                {
                                    Debug.WriteLine("Skipping Auto Property: " + propertyBlockSyntax.ToString() + ".");
                                }
                                else
                                {
                                    SyntaxList<AccessorBlockSyntax> accessors = propertyBlockSyntax.Accessors;

                                    AccessorBlockSyntax getter = accessors.FirstOrDefault(ad => ad.Kind() == SyntaxKind.GetAccessorBlock);
                                    AccessorBlockSyntax setter = accessors.FirstOrDefault(ad => ad.Kind() == SyntaxKind.SetAccessorBlock);
                                    if (getter == null && setter == null)
                                        continue;
                                    if (getter == null)
                                    {
                                        Debug.WriteLine("Skipping auto implemented 'get'.");
                                    }
                                    else
                                    {
                                        //if (getter.FullSpan == null)
                                        if (getter.Statements == null || getter.Statements.Count == 0)
                                        {
                                            Debug.WriteLine("Skipping auto implemented 'get'.");
                                        }
                                        else
                                        {
                                            //fullItemName = getter.ToString().Replace(getter.FullSpan.ToString(), "");
                                            //fullItemName = property.PropertyStatement.Identifier.Text + " (Get)";
                                            Debug.WriteLine("Function [Get]: " + fullItemName);
                                            
                                            if (ProcessBody(ref root, member, getter.Statements, ref programText, ref lstUsings, ci, DetailedLoggingFunctions.Any(p => p == fullItemName)))
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
                                        if (setter.Statements == null || setter.Statements.Count == 0)
                                        {
                                            Debug.WriteLine("Skipping auto implemented 'set'.");
                                        }
                                        else
                                        {
                                            //fullItemName = setter.ToString().Replace(setter.Statements.ToString(), "");
                                            //fullItemName = property.PropertyStatement.Identifier.Text + " (Set)";
                                            Debug.WriteLine("Function [Set]: " + fullItemName);
                                            if (ProcessBody(ref root, member, setter.Statements, ref programText, ref lstUsings, ci, DetailedLoggingFunctions.Any(p => p == fullItemName)))
                                            {
                                                updated = true;
                                            }
                                        }
                                    }
                                }
                                var fullMethodName = propertyBlockSyntax.ToFullString();
                            }
                        }
                        else if (member is ConstructorBlockSyntax ||
                                    member is MethodBlockSyntax)
                        {
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
                            if (member is ConstructorBlockSyntax constructorBlockSyntax)
                            {
                                if ((ci & CodeItems.Constructor) != CodeItems.Constructor)
                                    continue;

                                Debug.WriteLine("Constructor: " + constructorBlockSyntax.ToFullString()); //.Identifier);
                                if (constructorBlockSyntax.ToString().Contains("Private Sub InitializeComponent()"))
                                {
                                    Debug.WriteLine("Stop");
                                }

                                if (constructorBlockSyntax.Statements == null || constructorBlockSyntax.Statements.Count == 0)
                                    continue;
                                fullItemName = constructorBlockSyntax.ToString().Replace(constructorBlockSyntax.Statements.ToString(), "");
                                Debug.WriteLine("Function: " + fullItemName);
                                if (ProcessBody(ref root, member, constructorBlockSyntax.Statements, ref programText, ref lstUsings, ci, DetailedLoggingFunctions.Any(p => p == fullItemName)))
                                {
                                    updated = true;
                                }
                            }
                            else if (member is MethodBlockSyntax methodBlockSyntax)
                            {
                                if ((ci & CodeItems.Method) != CodeItems.Method)
                                    continue;

                                fullItemName = methodBlockSyntax.SubOrFunctionStatement.Identifier.Text;
                                Debug.WriteLine("Method: " + fullItemName);
                                if (methodBlockSyntax.ToString().Contains("Private Sub InitializeComponent()"))
                                {
                                    Debug.WriteLine("Stop");
                                }

                                if (methodBlockSyntax.Statements == null || methodBlockSyntax.Statements.Count == 0)
                                    continue;

                                foreach (var node in methodBlockSyntax.ChildNodes())
                                {
                                    Debug.WriteLine(node.ToString());
                                    foreach (var node1 in node.ChildNodes())
                                    {
                                        Debug.WriteLine(node1.ToString());
                                        foreach (var node2 in node1.ChildNodes())
                                        {
                                            Debug.WriteLine(node2.ToString());
                                        }
                                    }
                                }
                                Debug.WriteLine("Function: " + fullItemName);
                                if (ProcessBody(ref root, member, methodBlockSyntax.Statements, ref programText, ref lstUsings, ci, DetailedLoggingFunctions.Any(p => p == fullItemName)))
                                {
                                    updated = true;
                                }
                            }
                        }
                        else
                        {
                            Debug.WriteLine(member.GetType().FullName);
                            if (member is NamespaceBlockSyntax)
                            { }
                            else if (member is InterfaceBlockSyntax)
                            { }
                            else if (member is ClassBlockSyntax classBlockSyntax)
                            {
                                if (classBlockSyntax.Inherits != null)
                                {
                                    if ((ci & CodeItems.Class) == CodeItems.Class)
                                    {
                                        if (classBlockSyntax.Inherits.ToString().Contains(" ServiceBase"))
                                        {
                                            programText = programText.Replace(" ServiceBase", " WinServiceBase");
                                            if (!lstUsings.Contains("AdvancedLogging.BusinessLogic"))
                                                lstUsings.Add("AdvancedLogging.BusinessLogic");
                                            if (!ListLogName.Contains("Log"))
                                                ListLogName.Add("Log");
                                        }
                                        if (classBlockSyntax.Inherits.ToString().Contains("System.Web.HttpApplication"))
                                        {
                                            programText = programText.Replace("System.Web.HttpApplication", "WebServiceBase");
                                            if (!lstUsings.Contains("AdvancedLogging.BusinessLogic"))
                                                lstUsings.Add("AdvancedLogging.BusinessLogic");
                                        }
                                        if (classBlockSyntax.Inherits.ToString().Contains("System.Web.UI.Page"))
                                        {
                                            programText = programText.Replace("System.Web.UI.Page", "BasePage");
                                        }
                                    }
                                    for (int i = classBlockSyntax.Members.Count - 1; i >= 0; i--)
                                    {
                                        if (classBlockSyntax.Members[i] is FieldDeclarationSyntax)
                                        {
                                            FieldDeclarationSyntax fds = classBlockSyntax.Members[i] as FieldDeclarationSyntax;

                                            if (fds.IsType("ILog") ||
                                                fds.IsType("ICommonLogger"))
                                            {
                                                // Fix This
                                                //if (!m_lstLogName.Contains(fds.Declaration.Variables[0].Identifier.Text))
                                                //    m_lstLogName.Add(fds.Declaration.Variables[0].Identifier.Text);
                                                programText = programText.Replace(fds.ToFullString(), "");
                                            }
                                            // Do stuff with the symbol here
                                        }
                                    }
                                }
                            }
                            else if (member is FieldDeclarationSyntax fieldDeclarationSyntax)
                            {
                                if (fieldDeclarationSyntax.IsType("ILog") ||
                                    fieldDeclarationSyntax.IsType("ICommonLogger"))
                                {
                                    //m_dicLogName.Add(fds.Declaration.Id)
                                    //cds.Members.Remove(vMember);
                                }
                            }
                            else if (member is EnumBlockSyntax)
                            { }
                            else if (member is EnumMemberDeclarationSyntax)
                            { }
                            else if (member is Microsoft.CodeAnalysis.VisualBasic.Syntax.EventBlockSyntax)
                            { }
                            else if (member is Microsoft.CodeAnalysis.VisualBasic.Syntax.DelegateStatementSyntax)
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
                            SyntaxTree treeNew = VisualBasicSyntaxTree.ParseText(programText);
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

        private bool ProcessBody(ref CompilationUnitSyntax root, StatementSyntax member, SyntaxList<StatementSyntax> oBody, ref string programText, ref List<string> imports, CodeItems ci, bool addLineLogging = false)
        {
            bool updated = false;
            bool addAutoLog = ((ci & CodeItems.AutoLog) == CodeItems.AutoLog);
            bool tryCatch = ((ci & CodeItems.TryCatch) == CodeItems.TryCatch);
            bool constructor = (ci & CodeItems.Constructor) == CodeItems.Constructor;
            bool method = (ci & CodeItems.Method) == CodeItems.Method;
            bool property = (ci & CodeItems.Property) == CodeItems.Property;

            if (oBody.Count == 0)
                return updated;
            var vBody = oBody.ToString();
#if DEBUG
            if (addLineLogging)
            {
                // Line for Line Debug Statements Testing
                string tempReplacement = "";
                foreach (var statement in oBody)
                {
                    tempReplacement += "vAutoLogFunction.WriteDebug(\"" + statement.ToString().Replace("\"", "\\\"") + "\");" + Environment.NewLine;
                    tempReplacement += statement.ToString() + Environment.NewLine;
                }
                Debug.WriteLine(tempReplacement);
            }
#endif
            if ((ci & CodeItems.ModifyEmptyBody) == CodeItems.ModifyEmptyBody)
            {
                if (vBody.Replace(" ", "").Replace(Environment.NewLine, "") == "")
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
            string baseIndentText = "    ";
#endif

            if (member is ConstructorBlockSyntax constructorBlockSyntax)
            {
                if (constructorBlockSyntax != null)
                {
                    if (constructorBlockSyntax.BlockStatement.ParameterList != null && constructorBlockSyntax.BlockStatement.ParameterList.Parameters != null && constructorBlockSyntax.BlockStatement.ParameterList.Parameters.Count > 0)
                    {
                        foreach (var vParameter in constructorBlockSyntax.BlockStatement.ParameterList.Parameters)
                        {
                            if (variables == "")
                                variables = "New With { ";
                            else
                                variables += ", ";
                            variables += vParameter.Identifier.ToString();
                            Debug.WriteLine((vParameter.Identifier.ToString()));
                        }

                        if (variables != "")
                            variables += " }";
                    }
                }
            }
            else if (member is MethodStatementSyntax methodStatementSyntax)
            {
                if (methodStatementSyntax.ParameterList != null)
                {
                    foreach (var vParameter in methodStatementSyntax.ParameterList.Parameters)
                    {
                        if (variables == "")
                            variables = "New With { ";
                        else
                            variables += ", ";
                        variables += vParameter.Identifier.ToString();
                        Debug.WriteLine((vParameter.Identifier.ToString()));
                    }

                    if (variables != "")
                        variables += " }";

                }
            }
            else if (member is MethodBlockSyntax methodBlockSyntax)
            {
                Debug.WriteLine("Method: " + methodBlockSyntax.ToString());
                if (methodBlockSyntax.ToString().Contains("Private Sub InitializeComponent()"))
                {
                    Debug.WriteLine("Stop");
                }
                if (methodBlockSyntax.Statements != null && methodBlockSyntax.Statements.Count > 0)
                {
                    if (methodBlockSyntax.BlockStatement.ParameterList != null)
                    {
                        foreach (var vParameter in methodBlockSyntax.BlockStatement.ParameterList.Parameters)
                        {
                            if (variables == "")
                                variables = "New With { ";
                            else
                                variables += ", ";
                            variables += vParameter.Identifier.ToString();
                            Debug.WriteLine((vParameter.Identifier.ToString()));
                        }

                        if (variables != "")
                            variables += " }";
                    }
                }
            }
            else if (member is PropertyStatementSyntax)
            {
                baseIndenting = 4;
            }
            else if (member is PropertyBlockSyntax)
            {
                baseIndenting = 4;
            }
            replaceFind = vBody;
            string body = vBody;
            if (tryCatch && !vBody.Replace(" ", "").Contains("CatchexOuterAsException"))
            {
                if (vBody.Replace(" ", "").Replace(Environment.NewLine, "").StartsWith("UsingvAutoLogFunctionAsNewAutoLogFunction"))
                {
                    Debug.WriteLine(oBody[0].GetType().Name);
                    if (oBody[0] is ImportsStatementSyntax uss)
                    {
                        bool addTry = true;
                        //if (uss.ImportsClauses.Statement is BlockSyntax)
                        {
                        //    BlockSyntax bs = (BlockSyntax)uss.Statement;
                        //    Debug.WriteLine(bs.Statements[0].ToString());
                        //    bAddTry = !(bs.Statements[0].ToString().StartsWith("Try") && bs.Statements[0].ToString().Replace(" ", "").Contains("CatchexOuterAsException)"));
                        }
                        //else if (uss.Statement.ToString().Replace(" ", "").Replace(Environment.NewLine, "").StartsWith(oBody.OpenBraceToken.ToString() + "Try"))
                        {
                            addTry = false;
                        }
                        if (addTry)
                        {
                            Debug.WriteLine("Stop");
                            //    strReplaceWith = oBody.OpenBraceToken.ToString() + Environment.NewLine;
                            //    strReplaceWith += Indention(iBaseIndent, strBaseIndent) + "Try" + Environment.NewLine;
                            //    string strUSS = uss.Statement.ToString().Replace(Environment.NewLine, Environment.NewLine + Indention(1, strBaseIndent)) + Environment.NewLine;
                            //    foreach (string strLogName in m_lstLogName)
                            //    {
                            //        if (strUSS.Contains(strLogName))
                            //        {
                            //            foreach (string strToken in new string[] { ".", "?." })
                            //            {
                            //                strUSS = strUSS.Replace(strLogName + strToken + "Info(", "vAutoLogFunction.WriteLog(");
                            //                strUSS = strUSS.Replace(strLogName + strToken + "Warn(", "vAutoLogFunction.WriteWarn(");
                            //                strUSS = strUSS.Replace(strLogName + strToken + "Error(", "vAutoLogFunction.WriteError(");
                            //                strUSS = strUSS.Replace(strLogName + strToken + "Debug(", "vAutoLogFunction.WriteDebug(");
                            //                strUSS = strUSS.Replace(strLogName + strToken + "InfoFormat(", "vAutoLogFunction.WriteLogFormat(");
                            //                strUSS = strUSS.Replace(strLogName + strToken + "WarnFormat(", "vAutoLogFunction.WriteWarnFormat(");
                            //                strUSS = strUSS.Replace(strLogName + strToken + "ErrorFormat(", "vAutoLogFunction.WriteErrorFormat(");
                            //                strUSS = strUSS.Replace(strLogName + strToken + "DebugFormat(", "vAutoLogFunction.WriteDebugFormat(");
                            //            }
                            //        }
                            //    }
                            //    strReplaceWith += Indention(iBaseIndent, strBaseIndent) + strUSS;
                            //    strReplaceWith += Indention(iBaseIndent, strBaseIndent) + "Catch exOuter As Exception" + Environment.NewLine;
                            //    strReplaceWith += Indention(iBaseIndent, strBaseIndent) + oBody.OpenBraceToken.ToString() + Environment.NewLine;
                            //    if (strVariables.Length == 0)
                            //        strReplaceWith += Indention(iBaseIndent + 2, strBaseIndent) + "vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), error:=true, _ex:=exOuter)" + Environment.NewLine;
                            //    else
                            //        strReplaceWith += Indention(iBaseIndent + 2, strBaseIndent) + "vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod()" + strVariables + ", true, exOuter)" + Environment.NewLine;
                            //    strReplaceWith += Indention(iBaseIndent + 2, strBaseIndent) + "Throw" + Environment.NewLine;
                            //    strReplaceWith += Indention(iBaseIndent, strBaseIndent) + "End Try" + Environment.NewLine;
                            //    strReplaceWith += Indention(iBaseIndent - 1, strBaseIndent) + "End Using";
                            //    strReplaceWith = strBody.Replace(uss.Statement.ToString(), strReplaceWith);
                            //    strBody = strReplaceWith;
                        }
                    }
                    Debug.WriteLine("Stop");
                }
                else
                {
                    replaceWith = Environment.NewLine;
                    replaceWith += Indention(baseIndenting, baseIndentText) + "Try" + Environment.NewLine;
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
                    replaceWith += Indention(baseIndenting, baseIndentText) + "Catch exOuter As Exception" + Environment.NewLine;
                    if (addAutoLog)
                    {
                        replaceWith += Indention(baseIndenting + 1, baseIndentText) + "vAutoLogFunction.LogFunction(" + variables + ", System.Reflection.MethodBase.GetCurrentMethod(), error:=true, _ex:=exOuter)" + Environment.NewLine;
                    }
                    else
                    {
                        replaceWith += Indention(baseIndenting + 1, baseIndentText) + "LoggingUtils.LogFunction(" + variables + ", System.Reflection.MethodBase.GetCurrentMethod(), error:=true, _ex:=exOuter)" + Environment.NewLine;
                    }
                    replaceWith += Indention(baseIndenting + 1, baseIndentText) + "Throw" + Environment.NewLine;
                    replaceWith += Indention(baseIndenting, baseIndentText) + "End Try";
                    body = replaceWith;
                }
            }
            if (addAutoLog && !vBody.Replace(" ", "").Replace(Environment.NewLine, "").StartsWith("UsingvAutoLogFunctionAsNewAutoLogFunction"))
            {
                if (vBody.Contains("AutoLogFunction("))
                {
                    // Add strReplaceWith inside existing AutoLogFunction(
                    Debug.WriteLine("Stop");
                }
                else
                {
                    replaceWith = ""; // Environment.NewLine;
                    replaceWith += Indention(baseIndenting, baseIndentText) + "Using vAutoLogFunction As New AutoLogFunction(" + variables + ")"; // + Environment.NewLine;
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
                    replaceWith += Indention(baseIndenting - 1, baseIndentText) + "End Using";
                }
            }
            else if (replaceWith.Length > 0)
            {
                if (vBody.Replace(" ", "").Replace(Environment.NewLine, "").StartsWith("UsingvAutoLogFunctionAsNewAutoLogFunction"))
                {
                    Debug.WriteLine("Stop");
                }
            }

            if (replaceWith.Length > 0)
            {
                foreach (string @using in imports)
                {
                    NameSyntax uMHSUtils = SyntaxFactory.IdentifierName(@using);

                    string usingStatement = "Imports " + uMHSUtils.ToFullString();

                    if (!root.Imports.Any(x => x.ToFullString() == usingStatement) &&
                        !programText.Contains(usingStatement))
                    {
                        //ImportsStatementSyntax obj = SyntaxFactory.ImportsStatement("Imports", uMHSUtils);
                        //ImportsStatementSyntax obj2 = SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("System.Runtime.InteropServices"))));
                        ImportsStatementSyntax obj = SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName(uMHSUtils.ToString()))));
                        var newusing = root.Imports.Add(obj);
                        String usings = "";
                        if (root.Imports.Count > 0)
                            usings += root.Imports[root.Imports.Count - 1].ToString() + Environment.NewLine; //.Name

                        Debug.WriteLine("Adding: " + usingStatement + " ...");
                        if (usings.Length > 0)
                        {
                            programText = programText.Replace(usings, usings + usingStatement + Environment.NewLine);
                        }
                        else
                        {
                            programText = usingStatement + Environment.NewLine + Environment.NewLine + programText;
                        }
                    }
                    else
                    {
                        if (!root.Imports.ToString().Contains(usingStatement) && !programText.Contains(usingStatement))
                        {
                            Debugger.Break();
                        }
                    }
                }
                if (programText.Contains("\t" + replaceFind + Environment.NewLine))
                {
                    programText = programText.Replace("\t" + replaceFind + Environment.NewLine, replaceWith.Remove(0, 1) + Environment.NewLine);
                }
                else if (programText.Contains("    " + replaceFind + Environment.NewLine))
                {
                    programText = programText.Replace("    " + replaceFind + Environment.NewLine, replaceWith.Substring(4) + Environment.NewLine);
                }
                else
                {
                    programText = programText.Replace(replaceFind + Environment.NewLine, replaceWith + Environment.NewLine);
                }
                updated = true;

                Debug.WriteLine("Find:     " + replaceFind);
                Debug.WriteLine("Replace:  " + replaceWith);
            }
            return updated;
        }

    }
}
