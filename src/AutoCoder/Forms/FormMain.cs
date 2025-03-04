using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Trinet.Core.IO.Ntfs;

namespace AdvancedLogging.AutoCoder
{
    public partial class FormMain : Form
    {
        private readonly List<string> m_lstFoldersToSkip = null;
        private readonly System.Collections.Specialized.StringCollection m_colProcessFiles = new System.Collections.Specialized.StringCollection();
        private Dictionary<string, bool> m_dicHttpsMethods = new Dictionary<string, bool>();
        private Dictionary<string, bool> m_dicSqlMethods = new Dictionary<string, bool>();

        private readonly bool m_bSaveAsStream = false;
        private readonly List<string> m_lstLogName = new List<string>();
        public FormMain()
        {
            InitializeComponent();
            m_lstFoldersToSkip = new List<string>() { "bin", "obj", "images", "backup", "css", "ig_common", "scripts" };
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            if (chkRecurseFolders.Checked)
            {
                switch (MessageBox.Show("Recurse all folders?", Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.No:
                        chkRecurseFolders.Checked = false;
                        break;
                    case DialogResult.Cancel:
                        return;
                }
            }
            Cursor = Cursors.WaitCursor;
            lvwFiles.Items.Clear();
            CodeItems ci = CodeItems.None;
            if (chkFixConstructors.Checked)
                ci |= CodeItems.Constructor;
            if (chkFixMethods.Checked)
                ci |= CodeItems.Method;
            if (chkFixProperties.Checked)
                ci |= CodeItems.Property;
            if (chkFixBaseClass.Checked)
                ci |= CodeItems.Class;
            if (chkAutoReTrySQL.Checked)
                ci |= CodeItems.RetrySql;
            if (chkAutoReTryHttp.Checked)
                ci |= CodeItems.RetryHttp;
            if (chkAddAutoLog.Checked)
                ci |= CodeItems.AutoLog;
            if (chkAddTryCatch.Checked)
                ci |= CodeItems.TryCatch;
            if (chkProcessEmptyFunctions.Checked)
                ci |= CodeItems.ModifyEmptyBody;
            m_colProcessFiles.Clear();
            if (File.Exists(Path.Combine(txtFolder.Text, "AutoLogManagement.txt")))
            {
                m_colProcessFiles.AddRange(File.ReadAllLines(Path.Combine(txtFolder.Text, "AutoLogManagement.txt")));
            }
            SearchForFilesInFolder(txtFolder.Text, ci, chkBackup.Checked, chkRecurseFolders.Checked, false);
            tsslStatus.Text = "Ready!";
            Cursor = Cursors.Default;
        }

        private void SearchForFilesInFolder(string strFolderName, CodeItems ci, bool bBackup = false, bool bRecurseFolders = false, bool bShowFile = false)
        {
            List<string> FileExtenstions = new List<string>() { ".cs", ".vb" };
            SearchForFilesInFolder(new DirectoryInfo(strFolderName), FileExtenstions, ci, bBackup, bRecurseFolders, bShowFile);
        }

        private void SearchForFilesInFolder(DirectoryInfo diFolder, List<string> SearchString, CodeItems ci, bool bBackup = false, bool bRecurseFolders = false, bool bShowFile = false)
        {
            ListViewGroup lvg = lvwFiles.Groups.Add(diFolder.FullName, diFolder.FullName);
            foreach (FileInfo fiSelected in diFolder.GetFiles())
            {
                if (!SearchString.Any(p => p == fiSelected.Extension))
                    continue;

                CodeItems lci = ci;
                if (fiSelected.Name.ToLower() == "HttpWebExtensions.cs".ToLower())
                    lci = ci ^ CodeItems.RetryHttp;
                if (fiSelected.Name.ToLower() == "SqlExtensions.cs".ToLower())
                    lci = ci ^ CodeItems.RetrySql;
                if (fiSelected.Name.ToLower() == "AutoLogFunction.cs".ToLower() ||
                    fiSelected.Name.ToLower() == "ICommonLogger.cs".ToLower() ||
                    fiSelected.Name.ToLower() == "CommonLogger.cs".ToLower())
                    continue;

                bool bProcessed = false;
                if (fiSelected.AlternateDataStreamExists("Status"))
                {
                    Debug.WriteLine("Found Status stream:");

                    AlternateDataStreamInfo s = fiSelected.GetAlternateDataStream("Status", FileMode.Open);
                    using (TextReader reader = s.OpenText())
                    {
                        Debug.WriteLine(reader.ReadToEnd());
                    }
                }
                bProcessed = m_colProcessFiles.Contains(fiSelected.FullName.Substring(txtFolder.Text.Trim().Length + 1));
                if (fiSelected.Length > 0)
                {
                    ListViewItem lvi = lvwFiles.Items.Add(fiSelected.FullName, fiSelected.Extension.Replace(".", ""));
                    lvi.Group = lvg;
                    lvi.SubItems.Add(fiSelected.LastWriteTime.ToShortDateString() + " " + fiSelected.LastWriteTime.ToShortTimeString());
                    try
                    {
                        fiSelected.Refresh();
                        lvi.SubItems.Add(fiSelected.Length.ToString("#,##0"));
                        lvi.SubItems.Add(bProcessed ? "Updated" : "Not Updated");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                    tsslTotalFiles.Text = lvwFiles.Items.Count.ToString("#,##0") + " Files";
                }
                Application.DoEvents();
            }
            if (bRecurseFolders)
            {
                foreach (DirectoryInfo diSelected in diFolder.GetDirectories())
                {
                    if (!m_lstFoldersToSkip.Any(p => p == diSelected.Name.ToLower()))
                    {
                        SearchForFilesInFolder(diSelected.FullName, ci, bBackup, bRecurseFolders, bShowFile);
                    }
                }
            }
        }

        private void BtnFolder_Click(object sender, EventArgs e)
        {
            fbdCode.RootFolder = Environment.SpecialFolder.MyComputer;
            fbdCode.Description = "Find Code Folder";
            if (txtFolder.Text.Length > 0)
            {
                if (Directory.Exists(txtFolder.Text))
                {
                    fbdCode.SelectedPath = txtFolder.Text;
                }
            }
            if (fbdCode.ShowDialog() == DialogResult.OK)
            {
                Cursor = Cursors.WaitCursor;
                txtFolder.Text = fbdCode.SelectedPath;
                Cursor = Cursors.Default;
            }
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            String[] arrTest = new string[] { "Test", "Test2"};

            txtFolder.Text = Properties.Settings.Default.Directory;
            chkBackup.Checked = Properties.Settings.Default.Backup;
            chkRecurseFolders.Checked = Properties.Settings.Default.RecurseFolders;
            chkAddAutoLog.Checked = Properties.Settings.Default.AddAutoLog;
            chkAddTryCatch.Checked = Properties.Settings.Default.AddTryCatch;
            chkFixConstructors.Checked = Properties.Settings.Default.FixConstructors;
            chkFixMethods.Checked = Properties.Settings.Default.FixMethods;
            chkAutoReTrySQL.Checked = Properties.Settings.Default.AutoReTrySQL;
            chkAutoReTryHttp.Checked = Properties.Settings.Default.AutoReTryHttp;
            chkFixProperties.Checked = Properties.Settings.Default.FixProperties;
            chkFixBaseClass.Checked = Properties.Settings.Default.FixClass;

            Debug.WriteLine("HttpWebExtension Functions ...");
            foreach (var method in typeof(AdvancedLogging.Extensions.WebClientExtensions).GetMethods())
            {
                var parameters = method.GetParameters();
                var parameterDescriptions = string.Join(", ", method.GetParameters().Select(x => x.ParameterType + " " + x.Name).ToArray());

                Debug.WriteLine("{0} {1} ({2})", method.ReturnType, method.Name, parameterDescriptions);
                if (method.GetParameters().Any(x => x.Name.ToLower().Contains("retries")))
                {
                    if (!m_dicHttpsMethods.ContainsKey(method.Name))
                        m_dicHttpsMethods.Add(method.Name, true);
                }
            }
            foreach (var method in typeof(AdvancedLogging.Extensions.HttpClientExtensions).GetMethods())
            {
                var parameters = method.GetParameters();
                var parameterDescriptions = string.Join(", ", method.GetParameters().Select(x => x.ParameterType + " " + x.Name).ToArray());

                Debug.WriteLine("{0} {1} ({2})", method.ReturnType, method.Name, parameterDescriptions);
                if (method.GetParameters().Any(x => x.Name.ToLower().Contains("retries")))
                {
                    if (!m_dicHttpsMethods.ContainsKey(method.Name))
                        m_dicHttpsMethods.Add(method.Name, true);
                }
            }
            Debug.WriteLine("SqlExtension Functions ...");
            foreach (var method in typeof(AdvancedLogging.Extensions.DataExtensions).GetMethods())
            {
                var parameters = method.GetParameters();
                var parameterDescriptions = string.Join(", ", method.GetParameters().Select(x => x.ParameterType + " " + x.Name).ToArray());

                Debug.WriteLine("{0} {1} ({2})", method.ReturnType, method.Name, parameterDescriptions);
                if (method.GetParameters().Any(x => x.Name.ToLower().Contains("retries")))
                {
                    if (!m_dicSqlMethods.ContainsKey(method.Name))
                        m_dicSqlMethods.Add(method.Name, true);
                }
            }
            // Test System.Reflection.MethodBase.GetCurrentMethod() Cost
            Stopwatch sw = new Stopwatch();
            int i = 0;
            int iCount = 100000;
            System.Reflection.MethodBase mb = null;
            sw.Start();
            for (i = 0; i < iCount; i++)
            {
                mb = System.Reflection.MethodBase.GetCurrentMethod();
            }
            sw.Stop();
            Debug.WriteLine("Calling GetCurrentMethod() " + iCount.ToString() + " time = " + sw.ElapsedTicks.ToString() + "; Total MS: " + ((double)sw.ElapsedTicks / TimeSpan.TicksPerMillisecond).ToString());
            Debug.WriteLine("  Tickets per call " + ((double)sw.ElapsedTicks / iCount).ToString());

            sw.Reset();
            sw.Start();
            for (i = 0; i < iCount; i++)
            {
                //mb = System.Reflection.MethodBase.GetCurrentMethod();
            }
            sw.Stop();
            Debug.WriteLine("Calling Nothing " + iCount.ToString() + " time = " + sw.ElapsedTicks.ToString() + "; Total MS: " + ((double)sw.ElapsedTicks / TimeSpan.TicksPerMillisecond).ToString());
            Debug.WriteLine("  Tickets per call " + ((double)sw.ElapsedTicks / iCount).ToString());
            TestMethodbase(System.Reflection.MethodBase.GetCurrentMethod());
        }

        private void TestMethodbase(System.Reflection.MethodBase method)
        {
            StackFrame sf = new StackFrame(1, true);
            Debug.WriteLine(sf.GetMethod());
            Debug.WriteLine(method);
            Debug.WriteLine("Same: " + (sf.GetMethod() == method));
        }

        private void Common_CheckedChanged(object sender, EventArgs e)
        {
            btnProcess.Enabled = (((chkAddAutoLog.Checked || chkAddTryCatch.Checked) && (chkFixConstructors.Checked || chkFixMethods.Checked || chkFixBaseClass.Checked) || chkAutoReTryHttp.Checked || chkAutoReTrySQL.Checked) && lvwFiles.CheckedItems.Count > 0);
        }

        private void TxtFolder_TextChanged(object sender, EventArgs e)
        {
            btnSearch.Enabled = Directory.Exists(txtFolder.Text);
        }

        private void BtnProcess_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;

            tspbSelectedFiles.Maximum = lvwFiles.CheckedItems.Count;
            tspbSelectedFiles.Value = 0;
            m_lstLogName.Clear();

            CodeItems ci = CodeItems.None;
            if (chkFixConstructors.Checked)
                ci |= CodeItems.Constructor;
            if (chkFixMethods.Checked)
                ci |= CodeItems.Method;
            if (chkFixProperties.Checked)
                ci |= CodeItems.Property;
            if (chkFixBaseClass.Checked)
                ci |= CodeItems.Class;
            if (chkAutoReTrySQL.Checked)
                ci |= CodeItems.RetrySql;
            if (chkAutoReTryHttp.Checked)
                ci |= CodeItems.RetryHttp;
            if (chkAddAutoLog.Checked)
                ci |= CodeItems.AutoLog;
            if (chkAddTryCatch.Checked)
                ci |= CodeItems.TryCatch;

            if (File.Exists(Path.Combine(txtFolder.Text, "AutoLogManagement.txt")))
            {
                m_colProcessFiles.Clear();
                m_colProcessFiles.AddRange(File.ReadAllLines(Path.Combine(txtFolder.Text, "AutoLogManagement.txt")));
            }
            foreach (ListViewItem lviChecked in lvwFiles.CheckedItems)
            {
                FileInfo fiSelected = new FileInfo(lviChecked.Text);
                tsslStatus.Text = "Scanning: " + fiSelected.FullName + " for ILog Declaration ...";
                CodeItems lci = ci;
                if (fiSelected.Name.ToLower() == "HttpWebExtensions.cs".ToLower())
                    lci = ci ^ CodeItems.RetryHttp;
                if (fiSelected.Name.ToLower() == "SqlExtensions.cs".ToLower())
                    lci = ci ^ CodeItems.RetrySql;
                if (fiSelected.Name.ToLower() == "AutoLogFunction.cs".ToLower() ||
                    fiSelected.Name.ToLower() == "ICommonLogger.cs".ToLower() ||
                    fiSelected.Name.ToLower() == "CommonLogger.cs".ToLower())
                    continue;
                lviChecked.EnsureVisible();
                lviChecked.Selected = true;
                if (fiSelected.Extension == ".vb")
                {
                    CodeVisualBasic codeVB = new CodeVisualBasic(m_colProcessFiles, m_lstLogName, m_dicHttpsMethods, m_dicSqlMethods, txtFolder.Text, m_bSaveAsStream);
                    codeVB.ProgressChanged += CodeVB_ProgressChanged;
                    codeVB.WalkTree = true;
                    if (codeVB.ProcessFile(fiSelected, lci, chkBackup.Checked, false, true))
                    {
                        fiSelected.Refresh();

                        tspbSelectedFiles.Value++;
                        Application.DoEvents();
                    }
                    codeVB.ProgressChanged -= CodeVB_ProgressChanged;
                    tssbFileStatus.Visible = false;
                }
                else if (fiSelected.Extension == ".cs")
                {
                    CodeCSharp codeCS = new CodeCSharp(m_colProcessFiles, m_lstLogName, m_dicHttpsMethods, m_dicSqlMethods, txtFolder.Text, m_bSaveAsStream);
                    codeCS.ProgressChanged += CodeVB_ProgressChanged;
                    if (codeCS.ProcessFile(fiSelected, lci, chkBackup.Checked, false, true))
                    {
                        fiSelected.Refresh();

                        tspbSelectedFiles.Value++;
                        Application.DoEvents();
                    }
                    codeCS.ProgressChanged -= CodeVB_ProgressChanged;
                    tssbFileStatus.Visible = false;
                }
            }
            tspbSelectedFiles.Value = 0;
            foreach (ListViewItem lviChecked in lvwFiles.CheckedItems)
            {
                FileInfo fiSelected = new FileInfo(lviChecked.Text);
                tsslStatus.Text = "Processing: " + fiSelected.FullName + "...";
                CodeItems lci = ci;
                if (fiSelected.Name.ToLower() == "HttpWebExtensions.cs".ToLower())
                    lci = ci ^ CodeItems.RetryHttp;
                if (fiSelected.Name.ToLower() == "SqlExtensions.cs".ToLower())
                    lci = ci ^ CodeItems.RetrySql;
                if (fiSelected.Name.ToLower() == "AutoLogFunction.cs".ToLower() ||
                    fiSelected.Name.ToLower() == "ICommonLogger.cs".ToLower() ||
                    fiSelected.Name.ToLower() == "CommonLogger.cs".ToLower())
                    continue;
                lviChecked.EnsureVisible();
                lviChecked.Selected = true;
                if (m_colProcessFiles.Contains(fiSelected.FullName.Substring(txtFolder.Text.Trim().Length + 1)))
                {
                    Debug.WriteLine("Already processed!");
                }
                else
                {
                    if (fiSelected.Extension == ".vb")
                    {
                        CodeVisualBasic codeVB = new CodeVisualBasic(m_colProcessFiles, m_lstLogName, m_dicHttpsMethods, m_dicSqlMethods, txtFolder.Text, m_bSaveAsStream);
                        codeVB.ProgressChanged += CodeVB_ProgressChanged;
                        codeVB.WalkTree = true;
                        if (codeVB.ProcessFile(fiSelected, lci, chkBackup.Checked, false))
                        {
                            fiSelected.Refresh();

                            lviChecked.Checked = false;
                            lviChecked.SubItems[3].Text = "Updated";
                        }
                        tspbSelectedFiles.Value++;
                        Application.DoEvents();
                        codeVB.ProgressChanged -= CodeVB_ProgressChanged;
                        tssbFileStatus.Visible = false;
                    }
                    else if (fiSelected.Extension == ".cs")
                    {
                        CodeCSharp codeCS = new CodeCSharp(m_colProcessFiles, m_lstLogName, m_dicHttpsMethods, m_dicSqlMethods, txtFolder.Text, m_bSaveAsStream);
                        codeCS.ProgressChanged += CodeVB_ProgressChanged;
                        if (codeCS.ProcessFile(fiSelected, lci, chkBackup.Checked, false))
                        {
                            fiSelected.Refresh();

                            lviChecked.Checked = false;
                            lviChecked.SubItems[3].Text = "Updated";
                        }
                        tspbSelectedFiles.Value++;
                        Application.DoEvents();
                        codeCS.ProgressChanged -= CodeVB_ProgressChanged;
                        tssbFileStatus.Visible = false;
                    }
                }
                lviChecked.Selected = false;
            }
            if (File.Exists(Path.Combine(txtFolder.Text, "AutoLogManagement.txt")))
            {
                File.Delete(Path.Combine(txtFolder.Text, "AutoLogManagement.txt"));
            }
            File.WriteAllLines(Path.Combine(txtFolder.Text, "AutoLogManagement.txt"), m_colProcessFiles.Cast<string>());

            tsslStatus.Text = "Ready!";
            Cursor = Cursors.Default;
        }

        private void CodeVB_ProgressChanged(object sender, CodeVisualBasic.ProgressChangedEventArgs e)
        {
            if (!tssbFileStatus.Visible)
                tssbFileStatus.Visible = true;
            if (tssbFileStatus.Maximum != e.Total)
                tssbFileStatus.Maximum = e.Total;
            if (e.Current <= tssbFileStatus.Maximum)
                tssbFileStatus.Value = e.Current;
            tssbFileStatus.ToolTipText = "Processing " + e.ItemName + " ...";
            Debug.WriteLine(string.Format("Processing {0} - {1} - {2}%", e.FileName, e.ItemName, (e.Current * 100.0 / e.Total).ToString("#0.00")));
        }

        private void LvwFiles_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (!lvwFiles.Enabled)
                return;
            tsslSelectedFiles.Text = lvwFiles.CheckedItems.Count.ToString("#,##0") + " Files Selected";
            Common_CheckedChanged(sender, null);
        }

        private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            mnuFileSaveConfiguration.PerformClick();
        }

        private void MnuFilesSelectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in lvwFiles.Items)
            {
                lvi.Selected = true;
            }
        }

        private void MnuFilesCheckSelected_Click(object sender, EventArgs e)
        {
            lvwFiles.Enabled = false;
            foreach (ListViewItem lvi in lvwFiles.SelectedItems)
            {
                lvi.Checked = true;
            }
            lvwFiles.Enabled = true;
            Common_CheckedChanged(sender, null);
        }

        private void MnuFilesClear_Click(object sender, EventArgs e)
        {
            lvwFiles.SelectedItems.Clear();
            lvwFiles.Enabled = false;
            foreach (ListViewItem lvi in lvwFiles.CheckedItems)
            {
                lvi.Checked = false;
            }
            lvwFiles.Enabled = true;
            Common_CheckedChanged(sender, null);
        }

        private void LvwFiles_MouseDown(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo lvhti = lvwFiles.HitTest(e.Location);
            if (lvhti != null)
            {
                if (lvhti.Item != null)
                {
                    mnuFilesResetUpdate.Enabled = (lvhti.Item.SubItems[3].Text == "Updated");
                    mnuFilesResetUpdate.Tag = lvhti.Item;
                }
            }
        }

        private void MnuFilesResetUpdate_Click(object sender, EventArgs e)
        {
            if (mnuFilesResetUpdate.Tag is ListViewItem)
            {
                ListViewItem lvi = mnuFilesResetUpdate.Tag as ListViewItem;
                lvi.SubItems[3].Text = "Not Updated";
                string strTemp = lvi.Text.Substring(txtFolder.Text.Trim().Length + 1);
                while (m_colProcessFiles.Contains(strTemp))
                    m_colProcessFiles.Remove(strTemp);
                if (File.Exists(Path.Combine(txtFolder.Text, "AutoLogManagement.txt")))
                {
                    File.Delete(Path.Combine(txtFolder.Text, "AutoLogManagement.txt"));
                }
                File.WriteAllLines(Path.Combine(txtFolder.Text, "AutoLogManagement.txt"), m_colProcessFiles.Cast<string>());
            }
        }

        private void MnuViewRetryFunctions_Click(object sender, EventArgs e)
        {
            FormRetryFunctions frmRetry = new FormRetryFunctions
            {
                HttpsMethods = m_dicHttpsMethods,
                SqlMethods = m_dicSqlMethods
            };
            if (frmRetry.ShowDialog() == DialogResult.OK)
            {
                m_dicHttpsMethods = frmRetry.HttpsMethods;
                m_dicSqlMethods = frmRetry.SqlMethods;
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MnuFileSaveConfiguration_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Directory = txtFolder.Text;
            Properties.Settings.Default.Backup = chkBackup.Checked;
            Properties.Settings.Default.RecurseFolders = chkRecurseFolders.Checked;
            Properties.Settings.Default.AddAutoLog = chkAddAutoLog.Checked;
            Properties.Settings.Default.AddTryCatch = chkAddTryCatch.Checked;
            Properties.Settings.Default.FixConstructors = chkFixConstructors.Checked;
            Properties.Settings.Default.FixMethods = chkFixMethods.Checked;
            Properties.Settings.Default.AutoReTrySQL = chkAutoReTrySQL.Checked;
            Properties.Settings.Default.AutoReTryHttp = chkAutoReTryHttp.Checked;
            Properties.Settings.Default.FixProperties = chkFixProperties.Checked;
            Properties.Settings.Default.FixClass = chkFixBaseClass.Checked;
            Properties.Settings.Default.SqlMethods = new System.Collections.Specialized.StringCollection();
            Properties.Settings.Default.Upgrade();
            Properties.Settings.Default.Save();
        }

        private void MnuFilesOpen_Click(object sender, EventArgs e)
        {
            if (lvwFiles.SelectedItems.Count > 0)
            {
                FileInfo fiSelected = new FileInfo(lvwFiles.SelectedItems[0].Text);
                Process p = new Process();
                p.StartInfo.FileName = "notepad.exe";
                p.StartInfo.Arguments = fiSelected.FullName;
                p.Start();
            }
        }

        private void CmsFiles_Opening(object sender, CancelEventArgs e)
        {
            mnuFilesOpen.Enabled = mnuFilesOpenFolder.Enabled = (lvwFiles.SelectedItems.Count > 0);
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormAbout frmAboutDlg = new FormAbout();
            frmAboutDlg.ShowDialog();
        }

        private void MnuFilesOpenFolder_Click(object sender, EventArgs e)
        {
            if (lvwFiles.SelectedItems.Count > 0)
            {
                FileInfo fiSelected = new FileInfo(lvwFiles.SelectedItems[0].Text);
                Process p = new Process();
                p.StartInfo.FileName = "explorer.exe";
                p.StartInfo.Arguments = fiSelected.DirectoryName;
                p.Start();
            }
        }
    }
}
