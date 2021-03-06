﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using NppPluginNET;

namespace NppLinqPlugin
{
    class Main
    {
        #region " Fields "
        internal const string PluginName = "Linq2Lines";        
        private static string xmlConfigFilePath = null;
        private static XElement xmlConfig;        
        static frmMyDlg frmMyDlg = null;
        static int idMyDlg = -1;
        static Bitmap tbBmp = Properties.Resources.star;
        static Bitmap tbBmp_tbTab = Properties.Resources.star_bmp;
        static Icon tbIcon = null;
        #endregion

        #region " StartUp/CleanUp "
        internal static void CommandMenuInit()
        {            
            LoadXmlConfigFile();

            //PluginBase.SetCommand(0, "MyMenuCommand", myMenuFunction, new ShortcutKey(false, false, false, Keys.None));
            //PluginBase.SetCommand(1, "MyDockableDialog", myDockableDialog); idMyDlg = 1;
            PluginBase.SetCommand(0, "Linq2Lines Query", myDockableDialog); 
            idMyDlg = 0;            
        }
        internal static void SetToolBarIcon()
        {
            /*
            toolbarIcons tbIcons = new toolbarIcons();
            tbIcons.hToolbarBmp = tbBmp.GetHbitmap();
            IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_ADDTOOLBARICON, PluginBase._funcItems.Items[idMyDlg]._cmdID, pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
             * */
        }
        internal static void PluginCleanUp()
        {
            // save ini
            // Win32.WritePrivateProfileString("SomeSection", "SomeKey", someSetting ? "1" : "0", iniFilePath);            
            SaveXmlConfigFile();
        }
        #endregion

        #region " Menu functions "
        /*
        internal static void myMenuFunction()
        {
            MessageBox.Show("Hello N++!");
        }
         * */
        internal static void myDockableDialog()
        {
            if (frmMyDlg == null)
            {
                frmMyDlg = new frmMyDlg();

                using (Bitmap newBmp = new Bitmap(16, 16))
                {
                    Graphics g = Graphics.FromImage(newBmp);
                    ColorMap[] colorMap = new ColorMap[1];
                    colorMap[0] = new ColorMap();
                    colorMap[0].OldColor = Color.Fuchsia;
                    colorMap[0].NewColor = Color.FromKnownColor(KnownColor.ButtonFace);
                    ImageAttributes attr = new ImageAttributes();
                    attr.SetRemapTable(colorMap);
                    g.DrawImage(tbBmp_tbTab, new Rectangle(0, 0, 16, 16), 0, 0, 16, 16, GraphicsUnit.Pixel, attr);
                    tbIcon = Icon.FromHandle(newBmp.GetHicon());
                }

                NppTbData _nppTbData = new NppTbData();
                _nppTbData.hClient = frmMyDlg.Handle;
                _nppTbData.pszName = "Linq2Lines query";
                _nppTbData.dlgID = idMyDlg;
                _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_BOTTOM | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
                _nppTbData.hIconTab = (uint)tbIcon.Handle;
                _nppTbData.pszModuleName = PluginName;
                IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
                Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);

                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);

                // init config
                if (xmlConfig != null)
                {
                    try
                    {
                        frmMyDlg.QueryText = xmlConfig.Element("query").Value;
                        frmMyDlg.HelpersText = xmlConfig.Element("helpers").Value;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Linq2Lines error: " + ex.Message);
                    }
                }
            }
            else
            {
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DMMSHOW, 0, frmMyDlg.Handle);
            }
        }
        #endregion

        internal static void LoadXmlConfigFile()
        {
            StringBuilder sbXmlConfigFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbXmlConfigFilePath);
            xmlConfigFilePath = sbXmlConfigFilePath.ToString();
            if (!Directory.Exists(xmlConfigFilePath)) Directory.CreateDirectory(xmlConfigFilePath);
            xmlConfigFilePath = Path.Combine(xmlConfigFilePath, PluginName + ".xml");

            if (File.Exists(xmlConfigFilePath))
            {
                try
                {
                    xmlConfig = XElement.Parse(File.ReadAllText(xmlConfigFilePath, Encoding.UTF8));                                        
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Linq2Lines error loading config: " + ex.Message);
                    xmlConfig = null;
                    // do nothing                    
                }
                
            }
        }

        internal static void SaveXmlConfigFile()
        {
            if (frmMyDlg != null)
            {
                xmlConfig = new XElement("config",
                                         new XElement("query", frmMyDlg.QueryText),
                                         new XElement("helpers", frmMyDlg.HelpersText));
                File.WriteAllText(xmlConfigFilePath, xmlConfig.ToString(), Encoding.UTF8);
            }
        }
    }
}