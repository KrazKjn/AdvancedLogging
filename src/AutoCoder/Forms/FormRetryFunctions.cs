using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdvancedLogging.AutoCoder
{
    public partial class FormRetryFunctions : Form
    {
        private Dictionary<string, bool> m_dicHttpsMethods = new Dictionary<string, bool>();
        private Dictionary<string, bool> m_dicSqlMethods = new Dictionary<string, bool>();

        public Dictionary<string, bool> HttpsMethods
        {
            get
            {
                return m_dicHttpsMethods;
            }
            set
            {
                m_dicHttpsMethods = value;
            }
        }
        public Dictionary<string, bool> SqlMethods
        {
            get
            {
                return m_dicSqlMethods;
            }
            set
            {
                m_dicSqlMethods = value;
            }
        }
        public FormRetryFunctions()
        {
            InitializeComponent();
        }

        private void frmRetryFunctions_Load(object sender, EventArgs e)
        {
            ListViewGroup lvg = lvwRetryFunctions.Groups.Add("HTTPS", "HTTPS");
            foreach (string strItem in m_dicHttpsMethods.Keys)
            {
                ListViewItem lvi = lvwRetryFunctions.Items.Add(strItem);
                lvi.Group = lvg;
                lvi.ImageKey = "Function";
                lvi.Checked = m_dicHttpsMethods[strItem];
            }
            lvg = lvwRetryFunctions.Groups.Add("SQL", "SQL");
            foreach (string strItem in m_dicSqlMethods.Keys)
            {
                ListViewItem lvi = lvwRetryFunctions.Items.Add(strItem);
                lvi.Group = lvg;
                lvi.ImageKey = "Function";
                lvi.Checked = m_dicSqlMethods[strItem];
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in lvwRetryFunctions.Items)
            {
                if (lvi.Group.Header == "HTTPS")
                {
                    m_dicHttpsMethods[lvi.Text] = lvi.Checked;
                }
                else
                {
                    m_dicSqlMethods[lvi.Text] = lvi.Checked;
                }
            }
        }
    }
}
