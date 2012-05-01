using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using THOK.MCP;
using Microsoft.Win32;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace THOK.AS.Stocking.MCS
{
    public partial class MainForm : Form
    {
        private Context context = null;
        
        public MainForm()
        {
            InitializeComponent();
        }

        private void CreateDirectory(string directoryName)
        {
            if (!System.IO.Directory.Exists(directoryName))
                System.IO.Directory.CreateDirectory(directoryName);
        }

        private void WriteLoggerFile(string text)
        {
            try
            {
                string path = "";
                CreateDirectory("日志");
                path = "日志";
                path = path + @"/" + DateTime.Now.ToString().Substring(0, 4).Trim();
                CreateDirectory(path);
                path = path + @"/" + DateTime.Now.ToString().Substring(0, 7).Trim();
                path = path.TrimEnd(new char[] { '-'});
                CreateDirectory(path);
                path = path + @"/" + DateTime.Now.ToShortDateString() + ".txt";
                System.IO.File.AppendAllText(path, string.Format("{0} {1}", DateTime.Now, text + "\r\n"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        void Logger_OnLog(THOK.MCP.LogEventArgs args)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new LogEventHandler(Logger_OnLog), args);
            }
            else
            {
                lock (lbLog)
                {
                    string msg = string.Format("[{0}] {1} {2}", args.LogLevel, DateTime.Now, args.Message);
                    lbLog.Items.Insert(0, msg);
                    WriteLoggerFile(msg);
                }
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            context.Release();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
                e.Cancel = true;
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            lblTitle.Left = (pnlTitle.Width - lblTitle.Width) / 2;
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            try
            {
                Logger.OnLog += new LogEventHandler(Logger_OnLog);

                context = new Context();

                ContextInitialize initialize = new ContextInitialize();
                context.RegisterProcessControl(buttonArea);
                initialize.InitializeContext(context);
                context.RegisterProcessControl(monitorView);

                //context.Processes["DynamicShowProcess"].Resume();
            }
            catch (Exception ee)
            {
                Logger.Error("初始化处理失败请检查配置，原因：" + ee.Message);
            }
        }

        #region  程序运行控制只允许一个进程运行。

        [DllImport("user32")]
        public static extern long ShowWindow(long hwnd, long nCmdShow);
        [DllImport("user32")]
        public static extern long SetForegroundWindow(long hwnd);
        public const uint WM_SYSCOMMAND = 0x112;
        public const uint SC_RESTORE = 0xF120;
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(
        IntPtr hWnd, // handle to destination window 
        uint Msg,    // message 
        uint wParam, // first message parameter 
        uint lParam  // second message parameter 
        );
        private void Server_Load(object sender, EventArgs e)
        {
            if (Process.GetProcessesByName("LabelServer").Length > 1)
            {
                if (MessageBox.Show("电子标签服务已启动，将自动退出本程序！", "电子标签服务", MessageBoxButtons.OK).ToString() == "OK")
                {
                    foreach (Process p in Process.GetProcessesByName("LabelServer"))
                    {
                        if (this.Handle != ReadHandle())
                        {
                            SendMessage(ReadHandle(), WM_SYSCOMMAND, SC_RESTORE, 0);

                            SetForegroundWindow((long)ReadHandle());
                        }
                    }
                    System.Windows.Forms.Application.Exit();
                    return;
                }
            }
            else
            {
                WriteReg();
            }

        }
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == (int)WM_SYSCOMMAND && m.WParam.ToInt64() == SC_RESTORE)
            {
                this.Visible = true;
            }
            base.WndProc(ref m);
        }

        private void WriteReg()
        {
            // Create a subkey named Test9999 under HKEY_CURRENT_USER.
            RegistryKey test9999 = Registry.CurrentUser.CreateSubKey("LabelServer");
            // Create two subkeys under HKEY_CURRENT_USER\Test9999. The
            // keys are disposed when execution exits the using statement.
            using (RegistryKey testSettings = test9999.CreateSubKey("Server"))
            {
                // Create data for the TestSettings subkey.
                testSettings.SetValue("handle", this.Handle);
            }
        }
        private IntPtr ReadHandle()
        {
            // Create a subkey named Test9999 under HKEY_CURRENT_USER.
            RegistryKey test9999 = Registry.CurrentUser.OpenSubKey("LabelServer");
            // Create two subkeys under HKEY_CURRENT_USER\Test9999. The
            // keys are disposed when execution exits the using statement.
            using (RegistryKey testSettings = test9999.OpenSubKey("Server"))
            {
                // Create data for the TestSettings subkey.
                return (IntPtr)Convert.ToInt32(testSettings.GetValue("handle").ToString());
            }
        }
        #endregion
    }
}