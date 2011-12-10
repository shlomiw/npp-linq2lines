using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;
using System.Linq;

namespace NppLinqPlugin
{
    public partial class frmMyDlg : Form
    {
        private string m_DefaultQuery;

        public frmMyDlg()
        {
            InitializeComponent();

            m_DefaultQuery = textBoxQuery.Text;
        }

        private void buttonExecute_Click(object sender, EventArgs e)
        {
            IEnumerable<string> lines = GetTabLines();

            string[] resLines;
            try
            {
                resLines = LinqExecutor.LinqExecute(lines, textBoxQuery.Text, textBoxHelpers.Text).ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Query Compilation Error");
                return;
            }                    

            // Open a new document
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_FILE_NEW);
            IntPtr scintilla = PluginBase.GetCurrentScintilla();

            foreach (var line in resLines)
            {
                string _line = line + "\r\n";
                Win32.SendMessage(scintilla, SciMsg.SCI_APPENDTEXT, _line.Length, _line);
            }
            
            
            
        }

        private IEnumerable<string> GetTabLines()
        {
            IntPtr scintilla = PluginBase.GetCurrentScintilla();

            int linesCount = (int)Win32.SendMessage(scintilla, SciMsg.SCI_GETLINECOUNT, 0, 0);

            for (int i = 0; i < linesCount; i++)
            {
                int lineLength = (int) Win32.SendMessage(scintilla, SciMsg.SCI_LINELENGTH, i, 0);                
                StringBuilder sb = new StringBuilder(lineLength);                
                Win32.SendMessage(scintilla, SciMsg.SCI_GETLINE, i, sb);              
                yield return sb.ToString().Substring(0, lineLength).TrimEnd('\r','\n'); // trim carriage return
            }
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            textBoxQuery.Text = m_DefaultQuery;
        }

        public string QueryText
        {
            get { return textBoxQuery.Text; }
            set { textBoxQuery.Lines = value.Split('\n'); }
        }
        public string HelpersText
        {
            get { return textBoxHelpers.Text; }
            set { textBoxHelpers.Lines = value.Split('\n'); }
        }
        
    }
}
