namespace AdvancedLogging.AutoCoder
{
    partial class FormMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.btnSearch = new System.Windows.Forms.Button();
            this.btnFolder = new System.Windows.Forms.Button();
            this.txtFolder = new System.Windows.Forms.TextBox();
            this.fbdCode = new System.Windows.Forms.FolderBrowserDialog();
            this.chkBackup = new System.Windows.Forms.CheckBox();
            this.chkRecurseFolders = new System.Windows.Forms.CheckBox();
            this.lvwFiles = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.cmsFiles = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuFilesSelectAll = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFilesCheckSelected = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFilesClear = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuFilesResetUpdate = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuFilesOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.chkAddAutoLog = new System.Windows.Forms.CheckBox();
            this.chkAddTryCatch = new System.Windows.Forms.CheckBox();
            this.chkFixConstructors = new System.Windows.Forms.CheckBox();
            this.chkFixMethods = new System.Windows.Forms.CheckBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.chkAutoReTrySQL = new System.Windows.Forms.CheckBox();
            this.chkAutoReTryHttp = new System.Windows.Forms.CheckBox();
            this.chkFixProperties = new System.Windows.Forms.CheckBox();
            this.chkFixBaseClass = new System.Windows.Forms.CheckBox();
            this.chkProcessEmptyFunctions = new System.Windows.Forms.CheckBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tsslTotalFiles = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslSelectedFiles = new System.Windows.Forms.ToolStripStatusLabel();
            this.tspbSelectedFiles = new System.Windows.Forms.ToolStripProgressBar();
            this.tssbFileStatus = new System.Windows.Forms.ToolStripProgressBar();
            this.tsslStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnProcess = new System.Windows.Forms.Button();
            this.mnuMain = new System.Windows.Forms.MenuStrip();
            this.mnuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFileSaveConfiguration = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuEditSelectAll = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuEditCheckSelected = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuEditClear = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuEditResetUpdate = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuOptions = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuView = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuViewRetryFunctions = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFilesOpenFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsFiles.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.mnuMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnSearch
            // 
            this.btnSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSearch.Enabled = false;
            this.btnSearch.Location = new System.Drawing.Point(864, 33);
            this.btnSearch.Margin = new System.Windows.Forms.Padding(4);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(99, 28);
            this.btnSearch.TabIndex = 15;
            this.btnSearch.Text = "&Search ..";
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += new System.EventHandler(this.BtnSearch_Click);
            // 
            // btnFolder
            // 
            this.btnFolder.Location = new System.Drawing.Point(16, 33);
            this.btnFolder.Margin = new System.Windows.Forms.Padding(4);
            this.btnFolder.Name = "btnFolder";
            this.btnFolder.Size = new System.Drawing.Size(100, 28);
            this.btnFolder.TabIndex = 1;
            this.btnFolder.Text = "&Folder";
            this.btnFolder.UseVisualStyleBackColor = true;
            this.btnFolder.Click += new System.EventHandler(this.BtnFolder_Click);
            // 
            // txtFolder
            // 
            this.txtFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFolder.Location = new System.Drawing.Point(124, 36);
            this.txtFolder.Margin = new System.Windows.Forms.Padding(4);
            this.txtFolder.Name = "txtFolder";
            this.txtFolder.Size = new System.Drawing.Size(731, 22);
            this.txtFolder.TabIndex = 2;
            this.txtFolder.TextChanged += new System.EventHandler(this.TxtFolder_TextChanged);
            // 
            // chkBackup
            // 
            this.chkBackup.AutoSize = true;
            this.chkBackup.Location = new System.Drawing.Point(16, 81);
            this.chkBackup.Margin = new System.Windows.Forms.Padding(4);
            this.chkBackup.Name = "chkBackup";
            this.chkBackup.Size = new System.Drawing.Size(75, 20);
            this.chkBackup.TabIndex = 3;
            this.chkBackup.Text = "&Backup";
            this.chkBackup.UseVisualStyleBackColor = true;
            this.chkBackup.CheckedChanged += new System.EventHandler(this.Common_CheckedChanged);
            // 
            // chkRecurseFolders
            // 
            this.chkRecurseFolders.AutoSize = true;
            this.chkRecurseFolders.Location = new System.Drawing.Point(16, 110);
            this.chkRecurseFolders.Margin = new System.Windows.Forms.Padding(4);
            this.chkRecurseFolders.Name = "chkRecurseFolders";
            this.chkRecurseFolders.Size = new System.Drawing.Size(129, 20);
            this.chkRecurseFolders.TabIndex = 4;
            this.chkRecurseFolders.Text = "&Recurse Folders";
            this.chkRecurseFolders.UseVisualStyleBackColor = true;
            this.chkRecurseFolders.CheckedChanged += new System.EventHandler(this.Common_CheckedChanged);
            // 
            // lvwFiles
            // 
            this.lvwFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lvwFiles.CheckBoxes = true;
            this.lvwFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4});
            this.lvwFiles.ContextMenuStrip = this.cmsFiles;
            this.lvwFiles.FullRowSelect = true;
            this.lvwFiles.HideSelection = false;
            this.lvwFiles.LargeImageList = this.imageList1;
            this.lvwFiles.Location = new System.Drawing.Point(16, 138);
            this.lvwFiles.Margin = new System.Windows.Forms.Padding(4);
            this.lvwFiles.Name = "lvwFiles";
            this.lvwFiles.Size = new System.Drawing.Size(945, 336);
            this.lvwFiles.SmallImageList = this.imageList1;
            this.lvwFiles.TabIndex = 14;
            this.lvwFiles.UseCompatibleStateImageBehavior = false;
            this.lvwFiles.View = System.Windows.Forms.View.Details;
            this.lvwFiles.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.LvwFiles_ItemChecked);
            this.lvwFiles.MouseDown += new System.Windows.Forms.MouseEventHandler(this.LvwFiles_MouseDown);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "File";
            this.columnHeader1.Width = 130;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Date/Time";
            this.columnHeader2.Width = 176;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Size";
            this.columnHeader3.Width = 139;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Status";
            // 
            // cmsFiles
            // 
            this.cmsFiles.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.cmsFiles.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFilesSelectAll,
            this.mnuFilesCheckSelected,
            this.mnuFilesClear,
            this.toolStripMenuItem1,
            this.mnuFilesResetUpdate,
            this.toolStripMenuItem3,
            this.mnuFilesOpen,
            this.mnuFilesOpenFolder});
            this.cmsFiles.Name = "cmsFiles";
            this.cmsFiles.Size = new System.Drawing.Size(211, 188);
            this.cmsFiles.Opening += new System.ComponentModel.CancelEventHandler(this.CmsFiles_Opening);
            // 
            // mnuFilesSelectAll
            // 
            this.mnuFilesSelectAll.Name = "mnuFilesSelectAll";
            this.mnuFilesSelectAll.Size = new System.Drawing.Size(210, 24);
            this.mnuFilesSelectAll.Text = "&Select All";
            this.mnuFilesSelectAll.Click += new System.EventHandler(this.MnuFilesSelectAll_Click);
            // 
            // mnuFilesCheckSelected
            // 
            this.mnuFilesCheckSelected.Name = "mnuFilesCheckSelected";
            this.mnuFilesCheckSelected.Size = new System.Drawing.Size(210, 24);
            this.mnuFilesCheckSelected.Text = "C&heck Selected";
            this.mnuFilesCheckSelected.Click += new System.EventHandler(this.MnuFilesCheckSelected_Click);
            // 
            // mnuFilesClear
            // 
            this.mnuFilesClear.Name = "mnuFilesClear";
            this.mnuFilesClear.Size = new System.Drawing.Size(210, 24);
            this.mnuFilesClear.Text = "&Clear";
            this.mnuFilesClear.Click += new System.EventHandler(this.MnuFilesClear_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(207, 6);
            // 
            // mnuFilesResetUpdate
            // 
            this.mnuFilesResetUpdate.Enabled = false;
            this.mnuFilesResetUpdate.Name = "mnuFilesResetUpdate";
            this.mnuFilesResetUpdate.Size = new System.Drawing.Size(210, 24);
            this.mnuFilesResetUpdate.Text = "&Reset Update";
            this.mnuFilesResetUpdate.Click += new System.EventHandler(this.MnuFilesResetUpdate_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(207, 6);
            // 
            // mnuFilesOpen
            // 
            this.mnuFilesOpen.Name = "mnuFilesOpen";
            this.mnuFilesOpen.Size = new System.Drawing.Size(210, 24);
            this.mnuFilesOpen.Text = "&Open ...";
            this.mnuFilesOpen.Click += new System.EventHandler(this.MnuFilesOpen_Click);
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "cs");
            this.imageList1.Images.SetKeyName(1, "vb");
            // 
            // chkAddAutoLog
            // 
            this.chkAddAutoLog.AutoSize = true;
            this.chkAddAutoLog.Location = new System.Drawing.Point(161, 81);
            this.chkAddAutoLog.Margin = new System.Windows.Forms.Padding(4);
            this.chkAddAutoLog.Name = "chkAddAutoLog";
            this.chkAddAutoLog.Size = new System.Drawing.Size(107, 20);
            this.chkAddAutoLog.TabIndex = 5;
            this.chkAddAutoLog.Text = "Add &AutoLog";
            this.toolTip1.SetToolTip(this.chkAddAutoLog, "Include Auto Logging to track detailed/automatic logging.");
            this.chkAddAutoLog.UseVisualStyleBackColor = true;
            this.chkAddAutoLog.CheckedChanged += new System.EventHandler(this.Common_CheckedChanged);
            // 
            // chkAddTryCatch
            // 
            this.chkAddTryCatch.AutoSize = true;
            this.chkAddTryCatch.Location = new System.Drawing.Point(161, 110);
            this.chkAddTryCatch.Margin = new System.Windows.Forms.Padding(4);
            this.chkAddTryCatch.Name = "chkAddTryCatch";
            this.chkAddTryCatch.Size = new System.Drawing.Size(115, 20);
            this.chkAddTryCatch.TabIndex = 6;
            this.chkAddTryCatch.Text = "Add &Try/Catch";
            this.toolTip1.SetToolTip(this.chkAddTryCatch, "Include Try/Catch for untrapped/logged exceptions.");
            this.chkAddTryCatch.UseVisualStyleBackColor = true;
            this.chkAddTryCatch.CheckedChanged += new System.EventHandler(this.Common_CheckedChanged);
            // 
            // chkFixConstructors
            // 
            this.chkFixConstructors.AutoSize = true;
            this.chkFixConstructors.Location = new System.Drawing.Point(297, 81);
            this.chkFixConstructors.Margin = new System.Windows.Forms.Padding(4);
            this.chkFixConstructors.Name = "chkFixConstructors";
            this.chkFixConstructors.Size = new System.Drawing.Size(123, 20);
            this.chkFixConstructors.TabIndex = 7;
            this.chkFixConstructors.Text = "Fix &Constructors";
            this.toolTip1.SetToolTip(this.chkFixConstructors, "Update class Contructors.");
            this.chkFixConstructors.UseVisualStyleBackColor = true;
            this.chkFixConstructors.CheckedChanged += new System.EventHandler(this.Common_CheckedChanged);
            // 
            // chkFixMethods
            // 
            this.chkFixMethods.AutoSize = true;
            this.chkFixMethods.Location = new System.Drawing.Point(297, 110);
            this.chkFixMethods.Margin = new System.Windows.Forms.Padding(4);
            this.chkFixMethods.Name = "chkFixMethods";
            this.chkFixMethods.Size = new System.Drawing.Size(101, 20);
            this.chkFixMethods.TabIndex = 8;
            this.chkFixMethods.Text = "Fix &Methods";
            this.toolTip1.SetToolTip(this.chkFixMethods, "Update class Methods.");
            this.chkFixMethods.UseVisualStyleBackColor = true;
            this.chkFixMethods.CheckedChanged += new System.EventHandler(this.Common_CheckedChanged);
            // 
            // chkAutoReTrySQL
            // 
            this.chkAutoReTrySQL.AutoSize = true;
            this.chkAutoReTrySQL.Location = new System.Drawing.Point(572, 81);
            this.chkAutoReTrySQL.Margin = new System.Windows.Forms.Padding(4);
            this.chkAutoReTrySQL.Name = "chkAutoReTrySQL";
            this.chkAutoReTrySQL.Size = new System.Drawing.Size(126, 20);
            this.chkAutoReTrySQL.TabIndex = 11;
            this.chkAutoReTrySQL.Text = "Auto ReTry &SQL";
            this.toolTip1.SetToolTip(this.chkAutoReTrySQL, "Modify SQL Functions for Auto Retry.");
            this.chkAutoReTrySQL.UseVisualStyleBackColor = true;
            this.chkAutoReTrySQL.CheckedChanged += new System.EventHandler(this.Common_CheckedChanged);
            // 
            // chkAutoReTryHttp
            // 
            this.chkAutoReTryHttp.AutoSize = true;
            this.chkAutoReTryHttp.Location = new System.Drawing.Point(572, 110);
            this.chkAutoReTryHttp.Margin = new System.Windows.Forms.Padding(4);
            this.chkAutoReTryHttp.Name = "chkAutoReTryHttp";
            this.chkAutoReTryHttp.Size = new System.Drawing.Size(124, 20);
            this.chkAutoReTryHttp.TabIndex = 12;
            this.chkAutoReTryHttp.Text = "Auto ReTry &Http";
            this.toolTip1.SetToolTip(this.chkAutoReTryHttp, "Modify HTTP Functions for Auto Retry.");
            this.chkAutoReTryHttp.UseVisualStyleBackColor = true;
            this.chkAutoReTryHttp.CheckedChanged += new System.EventHandler(this.Common_CheckedChanged);
            // 
            // chkFixProperties
            // 
            this.chkFixProperties.AutoSize = true;
            this.chkFixProperties.Location = new System.Drawing.Point(440, 81);
            this.chkFixProperties.Margin = new System.Windows.Forms.Padding(4);
            this.chkFixProperties.Name = "chkFixProperties";
            this.chkFixProperties.Size = new System.Drawing.Size(111, 20);
            this.chkFixProperties.TabIndex = 9;
            this.chkFixProperties.Text = "Fix &Properties";
            this.toolTip1.SetToolTip(this.chkFixProperties, "Update class Properties (Get/Set)");
            this.chkFixProperties.UseVisualStyleBackColor = true;
            // 
            // chkFixBaseClass
            // 
            this.chkFixBaseClass.AutoSize = true;
            this.chkFixBaseClass.Location = new System.Drawing.Point(440, 110);
            this.chkFixBaseClass.Margin = new System.Windows.Forms.Padding(4);
            this.chkFixBaseClass.Name = "chkFixBaseClass";
            this.chkFixBaseClass.Size = new System.Drawing.Size(118, 20);
            this.chkFixBaseClass.TabIndex = 10;
            this.chkFixBaseClass.Text = "Fix &Base Class";
            this.toolTip1.SetToolTip(this.chkFixBaseClass, "Substitute generic Base classes for Custom classes for HttpApplication and Servic" +
        "eBase.  Adds TLS Support, SOAP Logging, and Logging.");
            this.chkFixBaseClass.UseVisualStyleBackColor = true;
            // 
            // chkProcessEmptyFunctions
            // 
            this.chkProcessEmptyFunctions.AutoSize = true;
            this.chkProcessEmptyFunctions.Location = new System.Drawing.Point(717, 110);
            this.chkProcessEmptyFunctions.Margin = new System.Windows.Forms.Padding(4);
            this.chkProcessEmptyFunctions.Name = "chkProcessEmptyFunctions";
            this.chkProcessEmptyFunctions.Size = new System.Drawing.Size(180, 20);
            this.chkProcessEmptyFunctions.TabIndex = 13;
            this.chkProcessEmptyFunctions.Text = "&Process Empty Functions";
            this.toolTip1.SetToolTip(this.chkProcessEmptyFunctions, "If the Function has no active body (i.e., all comments or empty, still add the ne" +
        "w code.");
            this.chkProcessEmptyFunctions.UseVisualStyleBackColor = true;
            this.chkProcessEmptyFunctions.CheckedChanged += new System.EventHandler(this.Common_CheckedChanged);
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsslTotalFiles,
            this.tsslSelectedFiles,
            this.tspbSelectedFiles,
            this.tssbFileStatus,
            this.tsslStatus});
            this.statusStrip1.Location = new System.Drawing.Point(0, 480);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 19, 0);
            this.statusStrip1.Size = new System.Drawing.Size(979, 26);
            this.statusStrip1.TabIndex = 17;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tsslTotalFiles
            // 
            this.tsslTotalFiles.Name = "tsslTotalFiles";
            this.tsslTotalFiles.Size = new System.Drawing.Size(50, 20);
            this.tsslTotalFiles.Text = "0 Files";
            // 
            // tsslSelectedFiles
            // 
            this.tsslSelectedFiles.Name = "tsslSelectedFiles";
            this.tsslSelectedFiles.Size = new System.Drawing.Size(111, 20);
            this.tsslSelectedFiles.Text = "0 Files Selected";
            // 
            // tspbSelectedFiles
            // 
            this.tspbSelectedFiles.Name = "tspbSelectedFiles";
            this.tspbSelectedFiles.Size = new System.Drawing.Size(133, 18);
            // 
            // tssbFileStatus
            // 
            this.tssbFileStatus.Name = "tssbFileStatus";
            this.tssbFileStatus.Size = new System.Drawing.Size(133, 18);
            // 
            // tsslStatus
            // 
            this.tsslStatus.Name = "tsslStatus";
            this.tsslStatus.Size = new System.Drawing.Size(528, 20);
            this.tsslStatus.Spring = true;
            this.tsslStatus.Text = "Ready.";
            this.tsslStatus.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnProcess
            // 
            this.btnProcess.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnProcess.Enabled = false;
            this.btnProcess.Location = new System.Drawing.Point(864, 69);
            this.btnProcess.Margin = new System.Windows.Forms.Padding(4);
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.Size = new System.Drawing.Size(99, 28);
            this.btnProcess.TabIndex = 16;
            this.btnProcess.Text = "&Process ..";
            this.btnProcess.UseVisualStyleBackColor = true;
            this.btnProcess.Click += new System.EventHandler(this.BtnProcess_Click);
            // 
            // mnuMain
            // 
            this.mnuMain.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.mnuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFile,
            this.mnuEdit,
            this.mnuOptions,
            this.mnuView,
            this.mnuHelp});
            this.mnuMain.Location = new System.Drawing.Point(0, 0);
            this.mnuMain.Name = "mnuMain";
            this.mnuMain.Size = new System.Drawing.Size(979, 28);
            this.mnuMain.TabIndex = 0;
            this.mnuMain.Text = "menuStrip1";
            // 
            // mnuFile
            // 
            this.mnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFileSaveConfiguration,
            this.toolStripMenuItem2,
            this.exitToolStripMenuItem});
            this.mnuFile.Name = "mnuFile";
            this.mnuFile.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.mnuFile.Size = new System.Drawing.Size(46, 24);
            this.mnuFile.Text = "&File";
            // 
            // mnuFileSaveConfiguration
            // 
            this.mnuFileSaveConfiguration.Name = "mnuFileSaveConfiguration";
            this.mnuFileSaveConfiguration.Size = new System.Drawing.Size(218, 26);
            this.mnuFileSaveConfiguration.Text = "&Save Configuration";
            this.mnuFileSaveConfiguration.Click += new System.EventHandler(this.MnuFileSaveConfiguration_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(215, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(218, 26);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            // 
            // mnuEdit
            // 
            this.mnuEdit.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuEditSelectAll,
            this.mnuEditCheckSelected,
            this.mnuEditClear,
            this.toolStripMenuItem5,
            this.mnuEditResetUpdate});
            this.mnuEdit.Name = "mnuEdit";
            this.mnuEdit.Size = new System.Drawing.Size(49, 24);
            this.mnuEdit.Text = "&Edit";
            // 
            // mnuEditSelectAll
            // 
            this.mnuEditSelectAll.Name = "mnuEditSelectAll";
            this.mnuEditSelectAll.Size = new System.Drawing.Size(192, 26);
            this.mnuEditSelectAll.Text = "&Select All";
            this.mnuEditSelectAll.Click += new System.EventHandler(this.MnuFilesSelectAll_Click);
            // 
            // mnuEditCheckSelected
            // 
            this.mnuEditCheckSelected.Name = "mnuEditCheckSelected";
            this.mnuEditCheckSelected.Size = new System.Drawing.Size(192, 26);
            this.mnuEditCheckSelected.Text = "C&heck Selected";
            this.mnuEditCheckSelected.Click += new System.EventHandler(this.MnuFilesCheckSelected_Click);
            // 
            // mnuEditClear
            // 
            this.mnuEditClear.Name = "mnuEditClear";
            this.mnuEditClear.Size = new System.Drawing.Size(192, 26);
            this.mnuEditClear.Text = "&Clear";
            this.mnuEditClear.Click += new System.EventHandler(this.MnuFilesClear_Click);
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(189, 6);
            // 
            // mnuEditResetUpdate
            // 
            this.mnuEditResetUpdate.Enabled = false;
            this.mnuEditResetUpdate.Name = "mnuEditResetUpdate";
            this.mnuEditResetUpdate.Size = new System.Drawing.Size(192, 26);
            this.mnuEditResetUpdate.Text = "&Reset Update";
            this.mnuEditResetUpdate.Click += new System.EventHandler(this.MnuFilesResetUpdate_Click);
            // 
            // mnuOptions
            // 
            this.mnuOptions.Enabled = false;
            this.mnuOptions.Name = "mnuOptions";
            this.mnuOptions.Size = new System.Drawing.Size(75, 24);
            this.mnuOptions.Text = "&Options";
            // 
            // mnuView
            // 
            this.mnuView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuViewRetryFunctions});
            this.mnuView.Name = "mnuView";
            this.mnuView.Size = new System.Drawing.Size(55, 24);
            this.mnuView.Text = "&View";
            // 
            // mnuViewRetryFunctions
            // 
            this.mnuViewRetryFunctions.Name = "mnuViewRetryFunctions";
            this.mnuViewRetryFunctions.Size = new System.Drawing.Size(192, 26);
            this.mnuViewRetryFunctions.Text = "Retry &Functions";
            this.mnuViewRetryFunctions.Click += new System.EventHandler(this.MnuViewRetryFunctions_Click);
            // 
            // mnuHelp
            // 
            this.mnuHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuAbout});
            this.mnuHelp.Name = "mnuHelp";
            this.mnuHelp.Size = new System.Drawing.Size(55, 24);
            this.mnuHelp.Text = "&Help";
            // 
            // mnuAbout
            // 
            this.mnuAbout.Name = "mnuAbout";
            this.mnuAbout.Size = new System.Drawing.Size(146, 26);
            this.mnuAbout.Text = "&About ...";
            this.mnuAbout.Click += new System.EventHandler(this.AboutToolStripMenuItem_Click);
            // 
            // mnuFilesOpenFolder
            // 
            this.mnuFilesOpenFolder.Name = "mnuFilesOpenFolder";
            this.mnuFilesOpenFolder.Size = new System.Drawing.Size(210, 24);
            this.mnuFilesOpenFolder.Text = "Open &Folder ...";
            this.mnuFilesOpenFolder.Click += new System.EventHandler(this.MnuFilesOpenFolder_Click);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(979, 506);
            this.Controls.Add(this.chkProcessEmptyFunctions);
            this.Controls.Add(this.mnuMain);
            this.Controls.Add(this.chkFixBaseClass);
            this.Controls.Add(this.chkFixProperties);
            this.Controls.Add(this.btnProcess);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.chkAutoReTryHttp);
            this.Controls.Add(this.chkAutoReTrySQL);
            this.Controls.Add(this.chkFixMethods);
            this.Controls.Add(this.chkFixConstructors);
            this.Controls.Add(this.chkAddTryCatch);
            this.Controls.Add(this.chkAddAutoLog);
            this.Controls.Add(this.lvwFiles);
            this.Controls.Add(this.chkRecurseFolders);
            this.Controls.Add(this.chkBackup);
            this.Controls.Add(this.txtFolder);
            this.Controls.Add(this.btnFolder);
            this.Controls.Add(this.btnSearch);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.mnuMain;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimumSize = new System.Drawing.Size(994, 543);
            this.Name = "FormMain";
            this.Text = "Auto Coder";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormMain_FormClosed);
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.cmsFiles.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.mnuMain.ResumeLayout(false);
            this.mnuMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Button btnFolder;
        private System.Windows.Forms.TextBox txtFolder;
        private System.Windows.Forms.FolderBrowserDialog fbdCode;
        private System.Windows.Forms.CheckBox chkBackup;
        private System.Windows.Forms.CheckBox chkRecurseFolders;
        private System.Windows.Forms.ListView lvwFiles;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.CheckBox chkAddAutoLog;
        private System.Windows.Forms.CheckBox chkAddTryCatch;
        private System.Windows.Forms.CheckBox chkFixConstructors;
        private System.Windows.Forms.CheckBox chkFixMethods;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.CheckBox chkAutoReTrySQL;
        private System.Windows.Forms.CheckBox chkAutoReTryHttp;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tsslStatus;
        private System.Windows.Forms.Button btnProcess;
        private System.Windows.Forms.ToolStripStatusLabel tsslTotalFiles;
        private System.Windows.Forms.ToolStripStatusLabel tsslSelectedFiles;
        private System.Windows.Forms.ToolStripProgressBar tspbSelectedFiles;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ContextMenuStrip cmsFiles;
        private System.Windows.Forms.ToolStripMenuItem mnuFilesSelectAll;
        private System.Windows.Forms.ToolStripMenuItem mnuFilesClear;
        private System.Windows.Forms.ToolStripMenuItem mnuFilesCheckSelected;
        private System.Windows.Forms.CheckBox chkFixProperties;
        private System.Windows.Forms.CheckBox chkFixBaseClass;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem mnuFilesResetUpdate;
        private System.Windows.Forms.MenuStrip mnuMain;
        private System.Windows.Forms.ToolStripMenuItem mnuFile;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mnuEdit;
        private System.Windows.Forms.ToolStripMenuItem mnuOptions;
        private System.Windows.Forms.ToolStripMenuItem mnuView;
        private System.Windows.Forms.ToolStripMenuItem mnuViewRetryFunctions;
        private System.Windows.Forms.ToolStripMenuItem mnuHelp;
        private System.Windows.Forms.ToolStripMenuItem mnuEditSelectAll;
        private System.Windows.Forms.ToolStripMenuItem mnuEditCheckSelected;
        private System.Windows.Forms.ToolStripMenuItem mnuEditClear;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem5;
        private System.Windows.Forms.ToolStripMenuItem mnuEditResetUpdate;
        private System.Windows.Forms.ToolStripMenuItem mnuFileSaveConfiguration;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.CheckBox chkProcessEmptyFunctions;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem mnuFilesOpen;
        private System.Windows.Forms.ToolStripProgressBar tssbFileStatus;
        private System.Windows.Forms.ToolStripMenuItem mnuAbout;
        private System.Windows.Forms.ToolStripMenuItem mnuFilesOpenFolder;
    }
}

