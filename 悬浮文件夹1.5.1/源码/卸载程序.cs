using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Uninst
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
        public static void DeleteShortcut(string lnkPath)
        {
            try { if (File.Exists(lnkPath)) File.Delete(lnkPath); }
            catch { }
        }
    }

    public class UninstallForm : Form
    {
        const string APP_NAME = "悬浮文件夹";
        Label lblStatus;
        Button btnGo, btnCancel;
        bool busy;

        public UninstallForm()
        {
            Text = "卸载 悬浮文件夹";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(460, 250);
            BackColor = P.BG;
            DoubleBuffered = true;
            ShowInTaskbar = false;
            SetRound();

            Label t = new Label();
            t.Text = "卸载 悬浮文件夹";
            t.Font = P.F(14f, FontStyle.Bold);
            t.ForeColor = P.TXT;
            t.Bounds = new Rectangle(26, 22, 400, 30);
            Controls.Add(t);

            Label d = new Label();
            d.Text = "将移除程序文件、桌面与开始菜单快捷方式、开机自启动设置。\n你放在「悬浮文件夹」目录里的文件不会被删除。";
            d.Font = P.F(9f, FontStyle.Regular);
            d.ForeColor = P.DIM;
            d.Bounds = new Rectangle(26, 62, 410, 60);
            Controls.Add(d);

            Label loc = new Label();
            loc.Text = "安装位置：" + Application.StartupPath;
            loc.Font = P.F(8.5f, FontStyle.Regular);
            loc.ForeColor = P.DIM;
            loc.Bounds = new Rectangle(26, 132, 410, 22);
            loc.AutoEllipsis = true;
            Controls.Add(loc);

            lblStatus = new Label();
            lblStatus.Text = "";
            lblStatus.Font = P.F(8.5f, FontStyle.Regular);
            lblStatus.ForeColor = P.ACC;
            lblStatus.Bounds = new Rectangle(26, 160, 410, 22);
            Controls.Add(lblStatus);

            btnGo = MkButton("卸载", P.ACC, Color.FromArgb(30, 26, 10), 26, 194, 130);
            btnGo.Click += delegate { DoUninstall(); };
            Controls.Add(btnGo);

            btnCancel = MkButton("取消", P.BTN, P.TXT, 172, 194, 110);
            btnCancel.Click += delegate { if (!busy) Close(); };
            Controls.Add(btnCancel);

            KeyPreview = true;
            KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Escape && !busy) Close(); };
        }

        void SetRound()
        {
            int rad = 14;
            GraphicsPath gp = new GraphicsPath();
            gp.AddArc(0, 0, rad * 2, rad * 2, 180, 90);
            gp.AddArc(Width - rad * 2, 0, rad * 2, rad * 2, 270, 90);
            gp.AddArc(Width - rad * 2, Height - rad * 2, rad * 2, rad * 2, 0, 90);
            gp.AddArc(0, Height - rad * 2, rad * 2, rad * 2, 90, 90);
            gp.CloseFigure();
            Region = new Region(gp);
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

        void DoUninstall()
        {
            busy = true;
            btnGo.Enabled = false;
            btnCancel.Enabled = false;
            lblStatus.Text = "正在卸载…";
            Application.DoEvents();

            string dir = Application.StartupPath;
            foreach (Process p in Process.GetProcessesByName(APP_NAME))
            {
                try { p.Kill(); } catch { }
            }
            System.Threading.Thread.Sleep(400);

            try
            {
                Shell.DeleteShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), APP_NAME + ".lnk"));
                Shell.DeleteShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), APP_NAME + ".lnk"));
                Shell.DeleteShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), APP_NAME + ".lnk"));
                string groupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), APP_NAME);
                if (Directory.Exists(groupDir))
                {
                    try { Directory.Delete(groupDir, true); } catch { }
                }
            }
            catch { }

            try
            {
                RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\FloatingFolder", true);
                if (k != null) { k.Close(); Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\FloatingFolder"); }
            }
            catch { }

            try
            {
                foreach (string f in Directory.GetFiles(dir))
                {
                    if (string.Compare(f, Application.ExecutablePath, true) != 0)
                    {
                        try { File.Delete(f); } catch { }
                    }
                }
            }
            catch { }

            lblStatus.Text = "卸载完成，正在清理剩余文件…";
            Application.DoEvents();

            string cmd = "/c timeout /t 2 >nul & del /f /q \"" + Application.ExecutablePath + "\" & rmdir /s /q \"" + dir + "\"";
            try
            {
                Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"), cmd) { CreateNoWindow = true, UseShellExecute = false });
            }
            catch { }
            Application.Exit();
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UninstallForm());
        }
    }
}
