using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Setup
{
    static class P
    {
        public static readonly Color BG = Color.FromArgb(28, 30, 35);
        public static readonly Color TXT = Color.FromArgb(233, 235, 239);
        public static readonly Color DIM = Color.FromArgb(138, 144, 153);
        public static readonly Color ACC = Color.FromArgb(242, 193, 78);
        public static readonly Color BTN = Color.FromArgb(43, 48, 57);

        public static Font F(float s, FontStyle st) { return new Font("微软雅黑", s, st); }
    }

    static class Shell
    {
        public static void CreateShortcut(string lnkPath, string target, string workDir, string desc, string icon)
        {
            object shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
            object lnk = shell.GetType().InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });
            lnk.GetType().InvokeMember("TargetPath", BindingFlags.SetProperty, null, lnk, new object[] { target });
            lnk.GetType().InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, lnk, new object[] { workDir });
            lnk.GetType().InvokeMember("Description", BindingFlags.SetProperty, null, lnk, new object[] { desc });
            lnk.GetType().InvokeMember("IconLocation", BindingFlags.SetProperty, null, lnk, new object[] { icon });
            lnk.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, lnk, null);
        }
    }

    public class SetupForm : Form
    {
        const string APP_NAME = "悬浮文件夹";
        const string VER = "1.5.1";

        string targetDir;
        CheckBox chkDesktop, chkAuto;
        Label lblStatus;
        Button btnGo, btnQuit;
        bool installed;

        public SetupForm()
        {
            Text = "安装 悬浮文件夹";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(560, 392);
            BackColor = P.BG;
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.None;
            SetRound();

            Label t = new Label();
            t.Text = "安装 悬浮文件夹";
            t.Font = P.F(15f, FontStyle.Bold);
            t.ForeColor = P.TXT;
            t.Bounds = new Rectangle(28, 24, 480, 34);
            Controls.Add(t);

            Label s = new Label();
            s.Text = "一个常驻桌面的小文件夹，直接看到里面的文件并可直接操作。";
            s.Font = P.F(9f, FontStyle.Regular);
            s.ForeColor = P.DIM;
            s.Bounds = new Rectangle(28, 60, 500, 24);
            Controls.Add(s);

            Panel line = new Panel();
            line.BackColor = Color.FromArgb(48, 53, 62);
            line.Bounds = new Rectangle(28, 94, 504, 1);
            Controls.Add(line);

            Label cap = new Label();
            cap.Text = "安装位置";
            cap.Font = P.F(8.5f, FontStyle.Regular);
            cap.ForeColor = P.DIM;
            cap.Bounds = new Rectangle(28, 110, 200, 20);
            Controls.Add(cap);

            targetDir = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"), APP_NAME);

            Label path = new Label();
            path.Text = targetDir;
            path.Font = P.F(9f, FontStyle.Regular);
            path.ForeColor = P.TXT;
            path.Bounds = new Rectangle(28, 132, 504, 24);
            path.AutoEllipsis = true;
            Controls.Add(path);

            chkDesktop = MkCheck("创建桌面快捷方式", true, 28, 176);
            Controls.Add(chkDesktop);
            chkAuto = MkCheck("开机自启动（随 Windows 启动，可随时在程序菜单里关掉）", false, 28, 206);
            Controls.Add(chkAuto);

            Label note = new Label();
            note.Text = "安装到当前用户目录，无需管理员权限；可在「设置 - 应用 - 已安装的应用」中卸载。\n你的文件放在「我的文档\\悬浮文件夹」，卸载不会删除它们。";
            note.Font = P.F(8.5f, FontStyle.Regular);
            note.ForeColor = P.DIM;
            note.Bounds = new Rectangle(28, 240, 504, 48);
            Controls.Add(note);

            lblStatus = new Label();
            lblStatus.Text = "";
            lblStatus.Font = P.F(8.5f, FontStyle.Regular);
            lblStatus.ForeColor = P.ACC;
            lblStatus.Bounds = new Rectangle(28, 296, 300, 22);
            Controls.Add(lblStatus);

            btnGo = MkButton("开始安装", P.ACC, Color.FromArgb(30, 26, 10), 28, 324, 150);
            btnGo.Click += delegate { if (installed) { Launch(); Close(); } else { DoInstall(); } };
            Controls.Add(btnGo);

            btnQuit = MkButton("退出", P.BTN, P.TXT, 190, 324, 104);
            btnQuit.Click += delegate { if (!installed) Close(); };
            Controls.Add(btnQuit);

            KeyPreview = true;
            KeyDown += delegate(object o, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) Close(); };
        }

        CheckBox MkCheck(string text, bool check, int x, int y)
        {
            CheckBox c = new CheckBox();
            c.Text = text;
            c.Font = P.F(9f, FontStyle.Regular);
            c.ForeColor = P.TXT;
            c.BackColor = P.BG;
            c.FlatStyle = FlatStyle.Flat;
            c.Checked = check;
            c.Bounds = new Rectangle(x, y, 500, 24);
            c.Cursor = Cursors.Hand;
            return c;
        }

        Button MkButton(string text, Color back, Color fore, int x, int y, int w)
        {
            Button b = new Button();
            b.Text = text;
            b.Font = P.F(9.5f, FontStyle.Bold);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = back;
            b.ForeColor = fore;
            b.Bounds = new Rectangle(x, y, w, 38);
            b.Cursor = Cursors.Hand;
            return b;
        }

        void SetRound()
        {
            int rad = 16;
            GraphicsPath gp = new GraphicsPath();
            gp.AddArc(0, 0, rad * 2, rad * 2, 180, 90);
            gp.AddArc(Width - rad * 2, 0, rad * 2, rad * 2, 270, 90);
            gp.AddArc(Width - rad * 2, Height - rad * 2, rad * 2, rad * 2, 0, 90);
            gp.AddArc(0, Height - rad * 2, rad * 2, rad * 2, 90, 90);
            gp.CloseFigure();
            Region = new Region(gp);
        }

        byte[] GetRes(string name)
        {
            Assembly a = Assembly.GetExecutingAssembly();
            using (Stream st = a.GetManifestResourceStream(name))
            {
                if (st == null) throw new Exception("资源缺失：" + name);
                MemoryStream ms = new MemoryStream();
                byte[] buf = new byte[8192];
                int n;
                while ((n = st.Read(buf, 0, buf.Length)) > 0) ms.Write(buf, 0, n);
                return ms.ToArray();
            }
        }

        public static void Log(string msg) { try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "ffsetup.log"), DateTime.Now.ToString("HH:mm:ss") + "  " + msg + Environment.NewLine); } catch { } }

        public void SilentInstall()
        {
            chkDesktop.Checked = true;
            chkAuto.Checked = false;
            DoInstall();
            Log("silent done, installed=" + installed);
        }

        void DoInstall()
        {
            Log("install start, target=" + targetDir);
            btnGo.Enabled = false;
            btnQuit.Enabled = false;
            chkDesktop.Enabled = false;
            chkAuto.Enabled = false;
            try
            {
                lblStatus.Text = "正在关闭旧版本…";
                Application.DoEvents();
                foreach (Process p in Process.GetProcessesByName(APP_NAME))
                {
                    try { p.Kill(); } catch { }
                }
                System.Threading.Thread.Sleep(350);

                lblStatus.Text = "正在复制程序文件…";
                Application.DoEvents();
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                string appExe = Path.Combine(targetDir, APP_NAME + ".exe");
                string appIco = Path.Combine(targetDir, APP_NAME + ".ico");
                string readme = Path.Combine(targetDir, "使用说明.txt");
                string uninst = Path.Combine(targetDir, "卸载悬浮文件夹.exe");

                File.WriteAllBytes(appExe, GetRes("app.exe"));
                Log("app.exe written");
                File.WriteAllBytes(appIco, GetRes("app.ico"));
                File.WriteAllBytes(readme, GetRes("readme.txt"));
                File.WriteAllBytes(uninst, GetRes("uninst.exe"));
                Log("all files written");

                lblStatus.Text = "正在创建快捷方式…";
                Application.DoEvents();
                if (chkDesktop.Checked)
                {
                    Shell.CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), APP_NAME + ".lnk"), appExe, targetDir, "桌面悬浮文件夹", appExe);
                }
                Shell.CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), APP_NAME + ".lnk"), appExe, targetDir, "桌面悬浮文件夹", appExe);

                string startupLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), APP_NAME + ".lnk");
                if (chkAuto.Checked)
                {
                    Shell.CreateShortcut(startupLnk, appExe, targetDir, "桌面悬浮文件夹", appExe);
                }
                else if (File.Exists(startupLnk))
                {
                    try { File.Delete(startupLnk); } catch { }
                }

                lblStatus.Text = "正在登记卸载信息…";
                Application.DoEvents();
                RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\FloatingFolder");
                k.SetValue("DisplayName", APP_NAME);
                k.SetValue("DisplayVersion", VER);
                k.SetValue("Publisher", APP_NAME);
                k.SetValue("InstallLocation", targetDir);
                k.SetValue("DisplayIcon", appExe);
                k.SetValue("UninstallString", "\"" + uninst + "\"");
                k.SetValue("QuietUninstallString", "\"" + uninst + "\"");
                k.SetValue("NoModify", 1, RegistryValueKind.DWord);
                k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                long sizeKb = 0;
                foreach (string f in Directory.GetFiles(targetDir)) sizeKb += new FileInfo(f).Length / 1024;
                k.SetValue("EstimatedSize", (int)sizeKb, RegistryValueKind.DWord);
                k.Close();

                installed = true;
                lblStatus.Text = "安装完成。";
                btnGo.Text = "启动悬浮文件夹";
                btnGo.Enabled = true;
                btnQuit.Text = "完成";
                btnQuit.Enabled = true;
                btnQuit.Click += delegate { Close(); };
                Launch();
            }
            catch (Exception ex)
            {
                Log("install failed: " + ex.ToString());
                lblStatus.Text = "安装失败：" + ex.Message;
                btnGo.Text = "重试";
                btnGo.Enabled = true;
                btnQuit.Enabled = true;
                chkDesktop.Enabled = true;
                chkAuto.Enabled = true;
            }
        }

        void Launch()
        {
            try { Process.Start(Path.Combine(targetDir, APP_NAME + ".exe")); }
            catch { }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool silent = false;
            foreach (string a in Environment.GetCommandLineArgs()) if (a.ToLower() == "/silent") silent = true;
            if (silent) { new SetupForm().SilentInstall(); return; }
            Application.Run(new SetupForm());
        }
    }
}
