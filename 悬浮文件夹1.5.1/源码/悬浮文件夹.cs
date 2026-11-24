using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace FloatingFolder
{
    public static class Ui
    {
        public static float Scale = 1f;
        public static int Radius = 14;
        public static float Opacity = 1f;
        public static int BgImg = 0;   // 0=纯色，1..4=内置背景图
        public static int OpLevel = 0; // 0=100% 1=70% 2=30% 3=0%
        public static readonly float[] OpLevelValue = new float[] { 1f, 0.7f, 0.3f, 0f };
        public static readonly string[] OpLevelName = new string[] { "100%", "70%", "30%", "0%" };
        static Bitmap[] bgCache = new Bitmap[5];
        static Bitmap[,] bakedCache = new Bitmap[5, 4];
        public static Color BG = Color.FromArgb(28, 30, 35);
        public static Color BG_HOVER = Color.FromArgb(40, 44, 53);
        public static Color BG_SEL = Color.FromArgb(49, 55, 66);
        public static Color LINE_C = Color.FromArgb(46, 50, 58);
        public static Color BORDER_C = Color.FromArgb(58, 63, 72);
        public static Color TXT = Color.FromArgb(233, 235, 239);
        public static Color DIM = Color.FromArgb(138, 144, 153);
        public static Color ACC = Color.FromArgb(242, 193, 78);
        public static Color CARD = Color.FromArgb(49, 55, 66);
        public static Color ON_ACC = Color.FromArgb(26, 28, 33);
        public static Color ACC_D = Color.FromArgb(214, 167, 58);
        public static Color TOAST = Color.FromArgb(42, 47, 57);
        public static Color BTN_BG = Color.FromArgb(43, 48, 58);

        public static Color Mix(Color a, Color b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return Color.FromArgb(
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }

        public static double Lum(Color c) { return (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0; }

        public static Bitmap GetBg(int idx)
        {
            if (idx < 1 || idx > 4) return null;
            if (bgCache[idx] == null)
            {
                try
                {
                    Assembly asm = Assembly.GetExecutingAssembly();
                    using (Stream st = asm.GetManifestResourceStream("bg" + idx + ".png"))
                    {
                        if (st != null) bgCache[idx] = new Bitmap(st);
                    }
                }
                catch { }
            }
            return bgCache[idx];
        }

        // 纯色模式下，档位对应的向白色淡化程度：100% 不淡、0% 最淡
        public static float Veil(int level)
        {
            float op = (level >= 0 && level < OpLevelValue.Length) ? OpLevelValue[level] : 1f;
            return 1f - op;
        }

        public static int LevelFromOpacity(float o)
        {
            int best = 0; float diff = float.MaxValue;
            for (int i = 0; i < OpLevelValue.Length; i++)
            {
                float d = Math.Abs(OpLevelValue[i] - o);
                if (d < diff) { diff = d; best = i; }
            }
            return best;
        }

        // 按档位把原照片以对应透明度叠在基准底色上，预烘焙成一张背景图；
        // 卡片和设置缩略图用同一张，避免色差。100%=原图，70%/30% 渐淡，0%=纯底色
        public static Bitmap GetBakedBg(int idx, int level)
        {
            Bitmap src = GetBg(idx);
            if (src == null) return null;
            if (level < 0) level = 0;
            if (level >= OpLevelValue.Length) level = OpLevelValue.Length - 1;
            if (bakedCache[idx, level] == null)
            {
                Bitmap bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    Color baseC = BgBaseColor(idx);
                    using (SolidBrush bb = new SolidBrush(baseC)) g.FillRectangle(bb, 0, 0, bmp.Width, bmp.Height);
                    float op = OpLevelValue[level];
                    if (op > 0f)
                    {
                        ColorMatrix cm = new ColorMatrix();
                        cm.Matrix33 = op;   // 照片透明度
                        using (ImageAttributes ia = new ImageAttributes())
                        {
                            ia.SetColorMatrix(cm);
                            g.DrawImage(src, new Rectangle(0, 0, bmp.Width, bmp.Height),
                                0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
                        }
                    }
                }
                bakedCache[idx, level] = bmp;
            }
            return bakedCache[idx, level];
        }

        // 每张背景图对应的基准底色：只用来推断文字/控件明暗，不直接铺底
        public static Color BgBaseColor(int idx)
        {
            switch (idx)
            {
                case 1: return Color.FromArgb(46, 58, 74);
                case 2: return Color.FromArgb(235, 224, 208);
                case 3: return Color.FromArgb(38, 78, 78);
                case 4: return Color.FromArgb(96, 74, 104);
                default: return Color.FromArgb(28, 30, 35);
            }
        }

        public static void ApplyTheme(Color bg, float opacity, int radius)
        {
            BG = bg;
            Opacity = opacity;
            Radius = radius;
            bool light = Lum(bg) > 0.58;
            TXT = light ? Color.FromArgb(24, 26, 31) : Color.FromArgb(233, 235, 239);
            DIM = Mix(TXT, BG, 0.42f);
            BG_HOVER = Mix(BG, TXT, 0.08f);
            BG_SEL = Mix(BG, TXT, 0.14f);
            LINE_C = Mix(BG, TXT, 0.12f);
            BORDER_C = Mix(BG, TXT, 0.22f);
            TOAST = Mix(BG, TXT, 0.10f);
            BTN_BG = Mix(BG, TXT, 0.10f);
            CARD = light ? Mix(BG, Color.White, 0.92f) : Mix(BG, TXT, 0.26f);
            ACC = light ? Color.FromArgb(219, 152, 40) : Color.FromArgb(242, 193, 78);
            ON_ACC = light ? Color.FromArgb(255, 253, 248) : Color.FromArgb(28, 30, 35);
            ACC_D = light ? Color.FromArgb(196, 132, 30) : Color.FromArgb(214, 167, 58);
        }

        public static Font FUI, FBold, FSmall, FSmallBold, FBadge, FBadgeS, FGlyph;

        // GDI 文字默认走 ClearType（子像素抗锯齿），在深色/半透明背景上每个笔画边缘都会带
        // 蓝橙彩色描边，看起来像“图标旁边一条蓝线”。这里统一把字体质量改成灰度抗锯齿。
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct LOGFONTW
        {
            public int lfHeight, lfWidth, lfEscapement, lfOrientation, lfWeight;
            public byte lfItalic, lfUnderline, lfStrikeOut, lfCharSet, lfOutPrecision, lfClipPrecision, lfQuality, lfPitchAndFamily;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string lfFaceName;
        }

        public static Font GrayAA(string family, float size, FontStyle style)
        {
            Font src = new Font(family, size, style);
            try
            {
                LOGFONTW lf = new LOGFONTW();
                src.ToLogFont(lf);
                lf.lfQuality = 4;   // ANTIALIASED_QUALITY：灰度抗锯齿，没有彩色描边
                Font f = Font.FromLogFont(lf);
                src.Dispose();
                return f;
            }
            catch (Exception) { return src; }
        }

        public static void Init()
        {
            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) Scale = Math.Max(1f, g.DpiX / 96f);
            }
            catch (Exception) { Scale = 1f; }
            FUI = GrayAA("Microsoft YaHei UI", 9.5f, FontStyle.Regular);
            FBold = GrayAA("Microsoft YaHei UI", 9.5f, FontStyle.Bold);
            FSmall = GrayAA("Microsoft YaHei UI", 7.5f, FontStyle.Regular);
            FSmallBold = GrayAA("Microsoft YaHei UI", 7.5f, FontStyle.Bold);
            FBadge = GrayAA("Segoe UI", 6.8f, FontStyle.Bold);
            FBadgeS = GrayAA("Segoe UI", 5.6f, FontStyle.Bold);
            FGlyph = GrayAA("Segoe UI Symbol", 10f, FontStyle.Regular);
        }

        public static int S(float v) { return (int)Math.Round(v * Scale); }

        public static GraphicsPath RoundPath(RectangleF r, float radius)
        {
            GraphicsPath p = new GraphicsPath();
            float d = radius * 2f;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // 只让“背景”半透明：把桌面壁纸按窗口位置贴上来，再用主题色按透明度覆盖一层。
    // 文字、图标等控件全部画在最后，因此始终是不透明的。
    public static class Glass
    {
        static Bitmap screen;      // 虚拟屏幕大小的壁纸
        static string sig = "";
        static int vsLeft, vsTop;
        static int lastCheck = 0;
        static readonly object wallLock = new object();
        static Color deskCache = Color.FromArgb(0, 96, 172);
        static int deskCheck = 0;

        // 桌面纯色（壁纸未覆盖 / 源矩形越界区域的兜底，避免留下黑条）
        static Color DeskColor()
        {
            int now = Environment.TickCount;
            if (now - deskCheck < 30000 && now > deskCheck) return deskCache;
            deskCheck = now;
            try
            {
                using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Colors"))
                {
                    object bg = (k == null) ? null : k.GetValue("Background", null);
                    if (bg != null)
                    {
                        string[] parts = Convert.ToString(bg).Split(' ');
                        if (parts.Length >= 3)
                            deskCache = Color.FromArgb(
                                Math.Min(255, Math.Max(0, int.Parse(parts[0]))),
                                Math.Min(255, Math.Max(0, int.Parse(parts[1]))),
                                Math.Min(255, Math.Max(0, int.Parse(parts[2]))));
                    }
                }
            }
            catch (Exception) { }
            return deskCache;
        }

        static string WallPath()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                {
                    if (k != null)
                    {
                        object v = k.GetValue("WallPaper", null);
                        if (v != null) return Convert.ToString(v);
                    }
                }
            }
            catch (Exception) { }
            return null;
        }

        static int WallStyle()
        {
            int style = 10;
            try
            {
                using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                {
                    if (k != null)
                    {
                        object st = k.GetValue("WallpaperStyle", null);
                        object tl = k.GetValue("TileWallpaper", null);
                        if (tl != null && Convert.ToString(tl) == "1") return 1;
                        if (st != null) int.TryParse(Convert.ToString(st), out style);
                    }
                }
            }
            catch (Exception) { }
            return style;
        }

        public static void Invalidate() { sig = ""; }

        public static int VsLeft { get { return vsLeft; } }
        public static int VsTop { get { return vsTop; } }

        public static Bitmap Wallpaper()
        {
            string path = WallPath();
            int style = WallStyle();
            System.Drawing.Rectangle vs = System.Windows.Forms.SystemInformation.VirtualScreen;
            string s = path + "|" + style + "|" + vs.Width + "x" + vs.Height + "|" + vs.X + "," + vs.Y;
            long stamp = 0;
            try { if (path != null && File.Exists(path)) stamp = File.GetLastWriteTime(path).Ticks; }
            catch (Exception) { }
            s += "|" + stamp;

            int now = Environment.TickCount;
            if (screen != null && sig == s && now - lastCheck < 30000 && now > lastCheck) return screen;
            lastCheck = now;

            if (screen != null) { screen.Dispose(); screen = null; }

            vsLeft = vs.X; vsTop = vs.Y;
            Bitmap bmp = new Bitmap(Math.Max(1, vs.Width), Math.Max(1, vs.Height));
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                Color desk = Color.FromArgb(0, 96, 172);
                try
                {
                    using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Colors"))
                    {
                        object bg = (k == null) ? null : k.GetValue("Background", null);
                        if (bg != null)
                        {
                            string[] parts = Convert.ToString(bg).Split(' ');
                            if (parts.Length >= 3)
                                desk = Color.FromArgb(
                                    Math.Min(255, Math.Max(0, int.Parse(parts[0]))),
                                    Math.Min(255, Math.Max(0, int.Parse(parts[1]))),
                                    Math.Min(255, Math.Max(0, int.Parse(parts[2]))));
                        }
                    }
                }
                catch (Exception) { }
                g.Clear(desk);

                Image img = null;
                try { if (path != null && File.Exists(path)) img = Image.FromFile(path); }
                catch (Exception) { }
                if (img != null)
                {
                    int sw = bmp.Width, sh = bmp.Height;
                    if (style == 2) g.DrawImage(img, 0, 0, sw, sh);
                    else if (style == 0) g.DrawImage(img, (sw - img.Width) / 2, (sh - img.Height) / 2, img.Width, img.Height);
                    else if (style == 1)
                    {
                        for (int y = 0; y < sh; y += img.Height)
                            for (int x = 0; x < sw; x += img.Width)
                                g.DrawImage(img, x, y, img.Width, img.Height);
                    }
                    else
                    {
                        // 6 适应 / 10 填充 / 22 跨屏：都按“保持比例铺满”处理
                        float sc = Math.Max((float)sw / img.Width, (float)sh / img.Height);
                        int dw = (int)Math.Round(img.Width * sc), dh = (int)Math.Round(img.Height * sc);
                        g.DrawImage(img, (sw - dw) / 2, (sh - dh) / 2, dw, dh);
                    }
                    img.Dispose();
                }
            }
            screen = bmp;
            sig = s;
            return screen;
        }

        // 只铺壁纸兜底（加锁：后台抓取线程和 UI 线程会同时读这张壁纸位图）
        public static void PaintWallpaper(Graphics g, Rectangle dest, Rectangle screenRect)
        {
            lock (wallLock)
            {
                // 先用桌面纯色把整块 dest 铺底：卡片贴屏幕边缘、外扩 1px 后源矩形越界，
                // 未被壁纸覆盖的区域也有确定颜色，不会残留黑/深色条。
                using (SolidBrush db = new SolidBrush(DeskColor())) g.FillRectangle(db, dest);
                Bitmap wall = Wallpaper();
                if (wall == null) return;
                int sx = screenRect.X - vsLeft, sy = screenRect.Y - vsTop;
                // 与虚拟屏幕求交，避免负坐标
                int dx0 = Math.Max(0, -sx), dy0 = Math.Max(0, -sy);
                int sx0 = sx + dx0, sy0 = sy + dy0;
                int w = Math.Min(dest.Width - dx0, wall.Width - sx0);
                int h = Math.Min(dest.Height - dy0, wall.Height - sy0);
                if (w > 0 && h > 0)
                {
                    g.DrawImage(wall,
                        new Rectangle(dest.X + dx0, dest.Y + dy0, w, h),
                        new Rectangle(sx0, sy0, w, h),
                        GraphicsUnit.Pixel);
                }
            }
        }

        // dest：控件的本地矩形；screenRect：同一块区域在屏幕上的坐标（保留参数以兼容旧调用）
        public static void Paint(Graphics g, Rectangle dest, Rectangle screenRect, Color tint, float alpha)
        {
            using (SolidBrush b = new SolidBrush(tint)) g.FillRectangle(b, dest);
        }

        public static void FillRaw(Control c, Graphics g)
        {
            if (c == null) return;
            Paint(g, new Rectangle(0, 0, c.ClientSize.Width, c.ClientSize.Height),
                Rectangle.Empty, Ui.BG, 1f);
        }

        public static void Fill(Control c, Graphics g)
        {
            if (c == null) return;
            Paint(g, new Rectangle(0, 0, c.ClientSize.Width, c.ClientSize.Height),
                Rectangle.Empty, Ui.BG, 1f);
        }
    }

    // 抓取卡片身后一层的真实窗口画面（PrintWindow 合成），作为半透明背景的来源；
    // 抓不到的区域仍由 Glass 的壁纸兜底。抓取放到后台线程，UI 线程只负责画，拖动不卡顿。
    static class Behind
    {
        delegate bool EnumProc(IntPtr h, IntPtr l);

        [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr l);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr hdcBlt, uint flags);
        [DllImport("user32.dll")] static extern IntPtr GetShellWindow();
        [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);

        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int L, T, R, B; }

        const uint PW_RENDERFULLCONTENT = 2;

        class Slot { public Bitmap Bmp; public Rectangle Rect; }

        static readonly object gate = new object();
        static readonly Dictionary<IntPtr, Slot> slots = new Dictionary<IntPtr, Slot>();
        static bool busy;
        static IntPtr pendingOwner = IntPtr.Zero;
        static Rectangle pendingRect = Rectangle.Empty;
        static IntPtr pendingSelf = IntPtr.Zero;
        static DateTime lastDone = DateTime.MinValue;
        static readonly List<Action> readyList = new List<Action>();

        public static void AttachReady(Action onReady) { lock (gate) readyList.Add(onReady); }
        public static void DetachReady(Action onReady) { lock (gate) readyList.Remove(onReady); }

        // 关闭某个窗口后释放它的快照槽位，避免陈旧位图被别的控件采样到
        public static void Release(IntPtr owner)
        {
            lock (gate)
            {
                Slot old;
                if (slots.TryGetValue(owner, out old) && old != null)
                {
                    if (old.Bmp != null) old.Bmp.Dispose();
                    slots.Remove(owner);
                }
            }
        }

        // UI 线程调用：登记一次后台抓取；重复请求会被合并，不会叠多个抓取任务
        public static void Request(IntPtr owner, Rectangle screenRect, IntPtr self)
        {
            if (owner == IntPtr.Zero || screenRect.Width <= 0 || screenRect.Height <= 0) return;
            lock (gate)
            {
                pendingOwner = owner;
                pendingRect = screenRect;
                pendingSelf = self;
                if (busy) return;
                busy = true;
            }
            System.Threading.ThreadPool.QueueUserWorkItem(delegate { Worker(); });
        }

        // 把所有相交槽位的快照叠到 dest 上
        public static void Draw(Graphics g, Rectangle dest, Rectangle screenRect)
        {
            List<Slot> copy;
            lock (gate) { copy = new List<Slot>(slots.Values); }
            foreach (Slot s in copy)
            {
                if (s.Bmp == null) continue;
                Rectangle ir = Rectangle.Intersect(screenRect, s.Rect);
                if (ir.IsEmpty) continue;
                try
                {
                    g.DrawImage(s.Bmp,
                        new Rectangle(dest.X + (ir.X - screenRect.X), dest.Y + (ir.Y - screenRect.Y), ir.Width, ir.Height),
                        new Rectangle(ir.X - s.Rect.X, ir.Y - s.Rect.Y, ir.Width, ir.Height),
                        GraphicsUnit.Pixel);
                }
                catch { }
            }
        }

        static void Worker()
        {
            while (true)
            {
                IntPtr owner; Rectangle rect; IntPtr self;
                lock (gate)
                {
                    Slot cur;
                    // 同一矩形且距上次完成不足 400ms，不重复抓
                    if (slots.TryGetValue(pendingOwner, out cur) && cur != null && cur.Rect == pendingRect
                        && (DateTime.Now - lastDone).TotalMilliseconds < 400)
                    {
                        busy = false;
                        return;
                    }
                    owner = pendingOwner;
                    rect = pendingRect;
                    self = pendingSelf;
                }

                Bitmap bmp = null;
                try { bmp = Build(rect, self); } catch { bmp = null; }

                List<Action> cb = null;
                lock (gate)
                {
                    if (bmp != null)
                    {
                        Slot old;
                        if (slots.TryGetValue(owner, out old) && old != null && old.Bmp != null) old.Bmp.Dispose();
                        slots[owner] = new Slot { Bmp = bmp, Rect = rect };
                    }
                    lastDone = DateTime.Now;
                    // 排队期间矩形又变了，稍候接着抓最新的一轮
                    if (pendingOwner != owner || pendingRect != rect)
                    {
                        System.Threading.Thread.Sleep(100);
                        continue;
                    }
                    busy = false;
                    cb = new List<Action>(readyList);
                }
                if (cb != null) foreach (Action a in cb) { try { a(); } catch { } }
                return;
            }
        }

        static Bitmap Build(Rectangle screenRect, IntPtr self)
        {
            uint selfPid = (uint)Process.GetCurrentProcess().Id;
            IntPtr shell = GetShellWindow();

            Bitmap bmp = new Bitmap(screenRect.Width, screenRect.Height, PixelFormat.Format32bppArgb);

            // 底层先铺壁纸兜底
            using (Graphics g0 = Graphics.FromImage(bmp))
            {
                Glass.PaintWallpaper(g0, new Rectangle(0, 0, bmp.Width, bmp.Height), screenRect);
            }

            var wins = new List<IntPtr>();
            EnumWindows((h, l) =>
            {
                if (h == self || h == shell) return true;
                if (!IsWindowVisible(h) || IsIconic(h)) return true;
                uint pid; GetWindowThreadProcessId(h, out pid);
                if (pid == selfPid) return true;   // 自己进程的菜单/设置面板在上方，不进身后快照
                RECT r; if (!GetWindowRect(h, out r)) return true;
                Rectangle wr = Rectangle.FromLTRB(r.L, r.T, r.R, r.B);
                if (!wr.IntersectsWith(screenRect)) return true;
                StringBuilder sb = new StringBuilder(256);
                GetWindowText(h, sb, 256);
                // 覆盖全屏、无标题的悬浮层 / 输入法外壳，跳过
                if (sb.Length == 0 && wr.Width >= screenRect.Width && wr.Height >= screenRect.Height) return true;
                wins.Add(h);
                return true;
            }, IntPtr.Zero);

            // EnumWindows 从上到下；反序从底层往上叠，贴近卡片的窗口最后盖上去
            for (int i = wins.Count - 1; i >= 0; i--)
            {
                IntPtr h = wins[i];
                RECT r; GetWindowRect(h, out r);
                Rectangle wr = Rectangle.FromLTRB(r.L, r.T, r.R, r.B);
                Rectangle inter = Rectangle.Intersect(wr, screenRect);
                if (inter.IsEmpty || wr.Width <= 0 || wr.Height <= 0) continue;
                Rectangle localInter = new Rectangle(inter.X - wr.X, inter.Y - wr.Y, inter.Width, inter.Height);

                Bitmap tmp = null;
                try
                {
                    tmp = new Bitmap(wr.Width, wr.Height, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(tmp))
                    {
                        IntPtr hdc = g.GetHdc();
                        bool ok = PrintWindow(h, hdc, PW_RENDERFULLCONTENT);
                        g.ReleaseHdc(hdc);
                        if (!ok) continue;
                    }
                    ForceOpaque(tmp);
                    int fl, ft, fr, fb;
                    MeasureFrame(tmp, out fl, out ft, out fr, out fb);
                    if (IsBlank(tmp)) continue;
                    int ix = inter.X - wr.X + fl;
                    int iy = inter.Y - wr.Y + ft;
                    int iw = inter.Width - fl - fr;
                    int ih = inter.Height - ft - fb;
                    if (iw <= 0 || ih <= 0) continue;
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.DrawImage(tmp,
                            new Rectangle(inter.X - screenRect.X, inter.Y - screenRect.Y, iw, ih),
                            new Rectangle(ix, iy, iw, ih),
                            GraphicsUnit.Pixel);
                    }
                }
                catch { }
                finally { if (tmp != null) tmp.Dispose(); }
            }

            return bmp;
        }

        // 统计窗口自身 alpha 是否有效：有效（多数像素不透明）就保留透明边距，圆角/阴影处不盖兜底底色；
        // alpha 整体失效（Electron 等常返回全 0）才统一补 255，避免把兜底壁纸擦成透明。
        static void ForceOpaque(Bitmap b)
        {
            BitmapData data = null;
            try
            {
                data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                int bytes = data.Stride * b.Height;
                byte[] buf = new byte[bytes];
                Marshal.Copy(data.Scan0, buf, 0, bytes);
                int opaque = 0, total = 0;
                for (int i = 3; i < bytes; i += 4) { total++; if (buf[i] > 128) opaque++; }
                if (total > 0 && opaque * 100 / total < 50)
                {
                    for (int i = 3; i < bytes; i += 4) buf[i] = 255;
                }
                Marshal.Copy(buf, 0, data.Scan0, bytes);
                b.UnlockBits(data);
            }
            catch { if (data != null) try { b.UnlockBits(data); } catch { } }
        }

        // 识别位图四周被 PrintWindow 抓成纯黑/透明的非客户边框（窗口框边、阴影列），返回各边要裁掉的像素数
        static void MeasureFrame(Bitmap b, out int left, out int top, out int right, out int bottom)
        {
            left = top = right = bottom = 0;
            BitmapData data = null;
            int W = b.Width, H = b.Height;
            if (W <= 4 || H <= 4) return;
            try
            {
                data = b.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte[] buf = new byte[data.Stride * H];
                Marshal.Copy(data.Scan0, buf, 0, buf.Length);
                int side = Math.Min(16, W / 4);
                for (int x = 0; x < side; x++) { if (ColFrame(buf, data.Stride, W, H, x)) left++; else break; }
                for (int x = W - 1; x >= W - side; x--) { if (ColFrame(buf, data.Stride, W, H, x)) right++; else break; }
                int vside = Math.Min(12, H / 4);
                for (int y = 0; y < vside; y++) { if (RowFrame(buf, data.Stride, W, H, y)) top++; else break; }
                for (int y = H - 1; y >= H - vside; y--) { if (RowFrame(buf, data.Stride, W, H, y)) bottom++; else break; }
                b.UnlockBits(data);
            }
            catch { if (data != null) try { b.UnlockBits(data); } catch { } }
        }

        static bool ColFrame(byte[] buf, int stride, int W, int H, int x)
        {
            int frame = 0, total = 0;
            for (int y = 0; y < H; y += 3)
            {
                int off = y * stride + x * 4;
                int a = buf[off + 3];
                int mx = Math.Max(buf[off], Math.Max(buf[off + 1], buf[off + 2]));
                total++;
                if (a < 128 || mx < 40) frame++;
            }
            return total > 0 && frame * 100 / total >= 70;
        }

        static bool RowFrame(byte[] buf, int stride, int W, int H, int y)
        {
            int frame = 0, total = 0;
            for (int x = 0; x < W; x += 3)
            {
                int off = y * stride + x * 4;
                int a = buf[off + 3];
                int mx = Math.Max(buf[off], Math.Max(buf[off + 1], buf[off + 2]));
                total++;
                if (a < 128 || mx < 40) frame++;
            }
            return total > 0 && frame * 100 / total >= 70;
        }

        static bool IsBlank(Bitmap b)
        {
            BitmapData data = null;
            int step = Math.Max(4, Math.Min(b.Width, b.Height) / 32);
            int total = 0, non = 0;
            try
            {
                data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte[] buf = new byte[data.Stride * b.Height];
                Marshal.Copy(data.Scan0, buf, 0, buf.Length);
                for (int y = 0; y < b.Height; y += step)
                    for (int x = 0; x < b.Width; x += step)
                    {
                        int off = y * data.Stride + x * 4;
                        if (buf[off + 3] < 128) continue;   // 透明边距不计入内容判定
                        int B = buf[off], G = buf[off + 1], R = buf[off + 2];
                        total++;
                        int mx = Math.Max(R, Math.Max(G, B)), mn = Math.Min(R, Math.Min(G, B));
                        bool dark = mx < 150;
                        bool colored = mx - mn > 25;
                        if (dark || colored) non++;
                    }
                b.UnlockBits(data);
            }
            catch { if (data != null) try { b.UnlockBits(data); } catch { } }
            return total == 0 || non * 100 / total < 1;
        }
    }

    public class DarkForm : Form
    {
        protected bool dragOn;
        protected Point dragStart;
        protected Point dragLoc;

        public DarkForm(int w, int h, int radius)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Ui.BG;
            ClientSize = new Size(w, h);
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            using (GraphicsPath p = Ui.RoundPath(new RectangleF(0, 0, w, h), radius))
            {
                Region old = Region;
                Region = new Region(p);
                if (old != null) old.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath p = Ui.RoundPath(new RectangleF(0, 0, ClientSize.Width, ClientSize.Height), Ui.S(14)))
            {
                using (SolidBrush b = new SolidBrush(Ui.BG)) g.FillPath(b, p);
                using (Pen pen = new Pen(Ui.BORDER_C, 1)) g.DrawPath(pen, p);
            }
        }
    }

    static class DragHelper
    {
        public static void Bind(Form f, params Control[] cs)
        {
            for (int i = 0; i < cs.Length; i++)
            {
                Control c = cs[i];
                c.MouseDown += delegate(object s, MouseEventArgs e)
                {
                    if (e.Button != MouseButtons.Left) return;
                    Form owner = f;
                    owner.Tag = new Point[2] { Cursor.Position, owner.Location };
                };
                c.MouseMove += delegate(object s, MouseEventArgs e)
                {
                    Form owner = f;
                    Point[] info = owner.Tag as Point[];
                    if (info == null || e.Button != MouseButtons.Left) return;
                    Point p = Cursor.Position;
                    owner.Location = new Point(info[1].X + p.X - info[0].X, info[1].Y + p.Y - info[0].Y);
                };
                c.MouseUp += delegate { f.Tag = null; };
            }
        }
    }

    static class ShellUtil
    {
        public static string StartupDir
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.Startup); }
        }

        public static string StartupLnk
        {
            get { return Path.Combine(StartupDir, "悬浮文件夹.lnk"); }
        }

        public static void CreateShortcut(string lnkPath, string target, string workDir, string desc, string icon)
        {
            object shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
            object lnk = shell.GetType().InvokeMember("CreateShortcut", BindingFlags.InvokeMethod,
                null, shell, new object[] { lnkPath });
            lnk.GetType().InvokeMember("TargetPath", BindingFlags.SetProperty, null, lnk, new object[] { target });
            lnk.GetType().InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, lnk, new object[] { workDir });
            if (!string.IsNullOrEmpty(desc))
                lnk.GetType().InvokeMember("Description", BindingFlags.SetProperty, null, lnk, new object[] { desc });
            if (!string.IsNullOrEmpty(icon))
                lnk.GetType().InvokeMember("IconLocation", BindingFlags.SetProperty, null, lnk, new object[] { icon });
            lnk.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, lnk, null);
        }

        public static bool AutostartEnabled
        {
            get { return File.Exists(StartupLnk); }
        }

        public static void SetAutostart(bool on)
        {
            if (on)
            {
                ShellUtil.CreateShortcut(ShellUtil.StartupLnk, Application.ExecutablePath,
                    Path.GetDirectoryName(Application.ExecutablePath), "悬浮文件夹（开机自启动）",
                    Application.ExecutablePath);
            }
            else
            {
                if (File.Exists(ShellUtil.StartupLnk)) File.Delete(ShellUtil.StartupLnk);
            }
        }
    }

    // 资源管理器的原生右键菜单（IContextMenu）：让卡片里的右键和真实文件夹完全一致
    static class ShellMenu
    {
        const uint CMF_NORMAL = 0x00000000;
        const uint CMF_EXPLORE = 0x00000004;
        const uint TPM_LEFTALIGN = 0x0000;
        const uint TPM_TOPALIGN = 0x0000;
        const uint TPM_RIGHTBUTTON = 0x0002;
        const uint TPM_RETURNCMD = 0x0100;
        const int CMIC_MASK_UNICODE = 0x00004000;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHParseDisplayName(string pszName, IntPtr pbc, out IntPtr ppidl, uint sfgaoIn, out uint psfgaoOut);

        [DllImport("shell32.dll")]
        static extern int SHBindToParent(IntPtr pidl, ref Guid riid, out IntPtr ppv, out IntPtr ppidlLast);

        [DllImport("shell32.dll")]
        static extern void ILFree(IntPtr pidl);

        [DllImport("user32.dll")]
        static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll")]
        static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        static extern int TrackPopupMenuEx(IntPtr hMenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        static readonly Guid IID_IShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
        static readonly Guid IID_IContextMenu = new Guid("000214E4-0000-0000-C000-000000000046");

        [ComImport, Guid("000214E6-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IShellFolder
        {
            [PreserveSig] int ParseDisplayName(IntPtr hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, out uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
            [PreserveSig] int EnumObjects(IntPtr hwnd, int grfFlags, out IntPtr ppenumIDList);
            [PreserveSig] int BindToObject(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);
            [PreserveSig] int BindToStorage(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);
            [PreserveSig] int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
            [PreserveSig] int CreateViewObject(IntPtr hwndOwner, ref Guid riid, out IntPtr ppv);
            [PreserveSig] int GetAttributesOf(uint cidl, IntPtr apidl, ref uint rgfInOut);
            [PreserveSig] int GetUIObjectOf(IntPtr hwndOwner, uint cidl, IntPtr apidl, ref Guid riid, IntPtr rgfReserved, out IntPtr ppv);
            [PreserveSig] int GetDisplayNameOf(IntPtr pidl, uint uFlags, IntPtr pName);
            [PreserveSig] int SetNameOf(IntPtr hwnd, IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string pszName, uint uFlags, out IntPtr ppidlOut);
        }

        [ComImport, Guid("000214E4-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IContextMenu
        {
            [PreserveSig] int QueryContextMenu(IntPtr hMenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, uint uFlags);
            void InvokeCommand(ref CMINVOKECOMMANDINFO pici);
            void GetCommandString(IntPtr idCmd, uint uFlags, IntPtr pwReserved, IntPtr pszName, uint cchMax);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        struct CMINVOKECOMMANDINFO
        {
            public int cbSize;
            public int fMask;
            public IntPtr hwnd;
            public IntPtr lpVerb;      // 命令序号（MAKEINTRESOURCE 形式，不占字符串）
            public IntPtr lpParameters;
            public IntPtr lpDirectory;
            public int nShow;
            public int dwHotKey;
            public IntPtr hIcon;
        }

        // 弹出资源管理器同款右键菜单；返回 true 表示已经处理（哪怕用户点了“取消”）
        public static bool Show(IntPtr hwnd, string path, Point screenPt)
        {
            IntPtr pidl = IntPtr.Zero, folderPtr = IntPtr.Zero, childPidl = IntPtr.Zero;
            IntPtr ctxPtr = IntPtr.Zero, menu = IntPtr.Zero, apidl = IntPtr.Zero;
            object folderObj = null, ctxObj = null;
            try
            {
                uint attrs = 0;
                if (SHParseDisplayName(path, IntPtr.Zero, out pidl, 0, out attrs) != 0 || pidl == IntPtr.Zero) return false;
                Guid iid = IID_IShellFolder;
                if (SHBindToParent(pidl, ref iid, out folderPtr, out childPidl) != 0 || folderPtr == IntPtr.Zero) return false;
                folderObj = Marshal.GetObjectForIUnknown(folderPtr);
                IShellFolder folder = (IShellFolder)folderObj;

                apidl = Marshal.AllocCoTaskMem(IntPtr.Size);
                Marshal.WriteIntPtr(apidl, childPidl);

                Guid iidCtx = IID_IContextMenu;
                if (folder.GetUIObjectOf(hwnd, 1, apidl, ref iidCtx, IntPtr.Zero, out ctxPtr) != 0 || ctxPtr == IntPtr.Zero) return false;
                ctxObj = Marshal.GetObjectForIUnknown(ctxPtr);
                IContextMenu ctx = (IContextMenu)ctxObj;

                menu = CreatePopupMenu();
                if (menu == IntPtr.Zero) return false;
                int nUsed = ctx.QueryContextMenu(menu, 0, 1, 0x7FFF, CMF_NORMAL | CMF_EXPLORE);
                if (nUsed < 0) return false;

                SetForegroundWindow(hwnd);
                int cmd = TrackPopupMenuEx(menu, TPM_LEFTALIGN | TPM_TOPALIGN | TPM_RIGHTBUTTON | TPM_RETURNCMD,
                    screenPt.X, screenPt.Y, hwnd, IntPtr.Zero);
                PostMessage(hwnd, 0x0000, IntPtr.Zero, IntPtr.Zero);
                if (cmd <= 0) return true;

                CMINVOKECOMMANDINFO info = new CMINVOKECOMMANDINFO();
                info.cbSize = Marshal.SizeOf(typeof(CMINVOKECOMMANDINFO));
                info.hwnd = hwnd;
                info.lpVerb = (IntPtr)(cmd - 1);
                info.nShow = 1;
                ctx.InvokeCommand(ref info);
                return true;
            }
            catch (Exception) { return false; }
            finally
            {
                if (menu != IntPtr.Zero) DestroyMenu(menu);
                if (apidl != IntPtr.Zero) Marshal.FreeCoTaskMem(apidl);
                if (ctxObj != null) { try { Marshal.ReleaseComObject(ctxObj); } catch (Exception) { } }
                if (folderObj != null) { try { Marshal.ReleaseComObject(folderObj); } catch (Exception) { } }
                if (ctxPtr != IntPtr.Zero) { try { Marshal.Release(ctxPtr); } catch (Exception) { } }
                if (folderPtr != IntPtr.Zero) { try { Marshal.Release(folderPtr); } catch (Exception) { } }
                if (pidl != IntPtr.Zero) { try { ILFree(pidl); } catch (Exception) { } }
            }
        }
    }

    // 自绘面板（内部绘制 Glass 背景，避免闪烁）
    public class BuffPanel : Panel
    {
        public BuffPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }
    }

    // 自绘列表：背景仍然走 Glass，保证“卡片半透明、文字不透明”
    public class RowsView : Control
    {
        public Widget owner;
        int itemH = 34;
        int top;
        int sel = -1;

        public RowsView()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = true;
            BackColor = Color.Transparent;
        }

        public int ItemHeight
        {
            get { return itemH; }
            set { if (value > 0 && value != itemH) { itemH = value; Invalidate(); } }
        }

        public int TopIndex
        {
            get { return top; }
            set
            {
                int v = Math.Max(0, value);
                if (v > MaxTop) v = MaxTop;
                if (v != top) { top = v; Invalidate(); }
            }
        }

        public int SelectedIndex
        {
            get { return sel; }
            set { if (sel != value) { sel = value; Invalidate(); } }
        }

        public int Count { get { return (owner == null) ? 0 : owner.ItemCount; } }

        public int VisibleRows { get { return Math.Max(1, ClientSize.Height / Math.Max(1, itemH)); } }

        public int MaxTop { get { return Math.Max(0, Count - VisibleRows); } }

        public int IndexFromPoint(Point p)
        {
            if (p.Y < 0) return -1;
            int i = top + p.Y / Math.Max(1, itemH);
            if (i < 0 || i >= Count) return -1;
            return i;
        }

        public void ClampTop()
        {
            int m = MaxTop;
            if (top > m) { top = m; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (owner != null) owner.PaintRows(this, e.Graphics);
        }
    }

    static class Program
    {
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            try { SetProcessDPIAware(); }
            catch (Exception) { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Ui.Init();
            Application.Run(new Widget());
        }
    }

    public class Item
    {
        public string Name;
        public string Path;
        public bool IsDir;
        public long Size;
        public DateTime Modified;
        public string Tag;
        public Color Badge;
    }

    public class MenuTheme : ProfessionalColorTable
    {
        public override Color MenuItemSelected { get { return Ui.BG_HOVER; } }
        public override Color MenuItemSelectedGradientBegin { get { return Ui.BG_HOVER; } }
        public override Color MenuItemSelectedGradientEnd { get { return Ui.BG_HOVER; } }
        public override Color MenuItemBorder { get { return Ui.LINE_C; } }
        public override Color ToolStripDropDownBackground { get { return Ui.BG; } }
        public override Color ImageMarginGradientBegin { get { return Ui.BG; } }
        public override Color ImageMarginGradientMiddle { get { return Ui.BG; } }
        public override Color ImageMarginGradientEnd { get { return Ui.BG; } }
        public override Color SeparatorDark { get { return Ui.LINE_C; } }
        public override Color SeparatorLight { get { return Ui.LINE_C; } }
        public override Color MenuBorder { get { return Ui.BORDER_C; } }
        public override Color ToolStripBorder { get { return Color.Transparent; } }
    }

    // 菜单背景也走 Glass：和卡片、设置面板同一套换色 / 透明度
    public class MenuRenderer : ToolStripProfessionalRenderer
    {
        public MenuRenderer() : base(new MenuTheme()) { RoundedEdges = false; }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            ToolStrip ts = e.ToolStrip;
            Graphics g = e.Graphics;
            Rectangle cr = new Rectangle(0, 0, ts.Width, ts.Height);
            Point sp = ts.PointToScreen(Point.Empty);
            float alpha = Ui.Opacity;
            if (alpha < 0.80f) alpha = 0.80f;   // 菜单太透会影响可读性
            Glass.Paint(g, cr, new Rectangle(sp.X, sp.Y, cr.Width, cr.Height), Ui.BG, alpha);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Ui.TXT : Ui.DIM;
            base.OnRenderItemText(e);
        }
    }

    public class ConfirmForm : DarkForm
    {
        bool answered;

        public static bool Ask(IWin32Window owner, string title, string message, string yesText, string noText)
        {
            ConfirmForm f = new ConfirmForm(title, message, yesText, noText);
            f.ShowDialog(owner);
            return f.answered;
        }

        ConfirmForm(string title, string message, string yesText, string noText)
            : base(Ui.S(404), Ui.S(196), Ui.S(14))
        {
            Label t = new Label();
            t.Text = title;
            t.Font = Ui.FBold;
            t.ForeColor = Ui.TXT;
            t.BackColor = Ui.BG;
            t.AutoSize = false;
            t.Bounds = new Rectangle(Ui.S(24), Ui.S(20), ClientSize.Width - Ui.S(48), Ui.S(22));
            Controls.Add(t);

            Label m = new Label();
            m.Text = message;
            m.Font = Ui.FUI;
            m.ForeColor = Ui.DIM;
            m.BackColor = Ui.BG;
            m.AutoSize = false;
            m.Bounds = new Rectangle(Ui.S(24), Ui.S(50), ClientSize.Width - Ui.S(48), Ui.S(72));
            Controls.Add(m);

            Button no = new Button();
            no.Text = noText;
            no.Font = Ui.FUI;
            no.FlatStyle = FlatStyle.Flat;
            no.FlatAppearance.BorderColor = Ui.BORDER_C;
            no.FlatAppearance.BorderSize = 1;
            no.BackColor = Ui.BTN_BG;
            no.ForeColor = Ui.TXT;
            no.Bounds = new Rectangle(ClientSize.Width - Ui.S(24) - Ui.S(88), ClientSize.Height - Ui.S(50), Ui.S(88), Ui.S(32));
            no.Click += delegate { answered = false; Close(); };
            Controls.Add(no);

            Button yes = new Button();
            yes.Text = yesText;
            yes.Font = Ui.FUI;
            yes.FlatStyle = FlatStyle.Flat;
            yes.FlatAppearance.BorderSize = 0;
            yes.BackColor = Ui.ACC;
            yes.ForeColor = Color.FromArgb(30, 32, 36);
            yes.Bounds = new Rectangle(ClientSize.Width - Ui.S(24) - Ui.S(88) * 2 - Ui.S(10), ClientSize.Height - Ui.S(50), Ui.S(88), Ui.S(32));
            yes.Click += delegate { answered = true; Close(); };
            Controls.Add(yes);

            AcceptButton = yes;
            CancelButton = no;
            DragHelper.Bind(this, t, this);
        }
    }

    public class RenameForm : DarkForm
    {
        TextBox box;

        public string Value { get { return box.Text; } }

        public RenameForm(string initial, string hint, string suffix)
            : base(Ui.S(404), Ui.S(214), Ui.S(14))
        {
            Label t = new Label();
            t.Text = "重命名";
            t.Font = Ui.FBold;
            t.ForeColor = Ui.TXT;
            t.BackColor = Ui.BG;
            t.AutoSize = false;
            t.Bounds = new Rectangle(Ui.S(24), Ui.S(18), ClientSize.Width - Ui.S(48), Ui.S(22));
            Controls.Add(t);

            Label h = new Label();
            h.Text = hint;
            h.Font = Ui.FSmall;
            h.ForeColor = Ui.DIM;
            h.BackColor = Ui.BG;
            h.AutoSize = false;
            h.AutoEllipsis = true;
            h.Bounds = new Rectangle(Ui.S(24), Ui.S(44), ClientSize.Width - Ui.S(48), Ui.S(18));
            Controls.Add(h);

            box = new TextBox();
            box.Text = initial;
            box.Font = Ui.FUI;
            box.BackColor = Ui.BTN_BG;
            box.ForeColor = Ui.TXT;
            box.BorderStyle = BorderStyle.None;
            box.Bounds = new Rectangle(Ui.S(26), Ui.S(74), ClientSize.Width - Ui.S(52), Ui.S(30));
            Controls.Add(box);

            Panel line = new Panel();
            line.BackColor = Ui.BORDER_C;
            line.Bounds = new Rectangle(Ui.S(24), Ui.S(104), ClientSize.Width - Ui.S(48), 1);
            Controls.Add(line);

            Label sfx = new Label();
            sfx.Text = suffix;
            sfx.Font = Ui.FSmall;
            sfx.ForeColor = Ui.DIM;
            sfx.BackColor = Ui.BG;
            sfx.AutoSize = false;
            sfx.Bounds = new Rectangle(Ui.S(24), Ui.S(112), ClientSize.Width - Ui.S(48), Ui.S(16));
            Controls.Add(sfx);

            Button cancel = new Button();
            cancel.Text = "取消";
            cancel.Font = Ui.FUI;
            cancel.FlatStyle = FlatStyle.Flat;
            cancel.FlatAppearance.BorderColor = Ui.BORDER_C;
            cancel.FlatAppearance.BorderSize = 1;
            cancel.BackColor = Ui.BTN_BG;
            cancel.ForeColor = Ui.TXT;
            cancel.Bounds = new Rectangle(ClientSize.Width - Ui.S(24) - Ui.S(88), ClientSize.Height - Ui.S(50), Ui.S(88), Ui.S(32));
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            Button ok = new Button();
            ok.Text = "确定";
            ok.Font = Ui.FUI;
            ok.FlatStyle = FlatStyle.Flat;
            ok.FlatAppearance.BorderSize = 0;
            ok.BackColor = Ui.ACC;
            ok.ForeColor = Color.FromArgb(30, 32, 36);
            ok.Bounds = new Rectangle(ClientSize.Width - Ui.S(24) - Ui.S(88) * 2 - Ui.S(10), ClientSize.Height - Ui.S(50), Ui.S(88), Ui.S(32));
            ok.Click += delegate { DialogResult = DialogResult.OK; Close(); };
            Controls.Add(ok);

            AcceptButton = ok;
            CancelButton = cancel;
            DragHelper.Bind(this, t, this);
            box.SelectAll();
            box.Focus();
        }
    }

    public class Widget : Form
    {
        const string APP_NAME = "悬浮文件夹";

        static readonly string CfgDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FloatingFolder");
        static readonly string CfgPath = Path.Combine(CfgDir, "config.ini");
        static readonly string DefaultDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "悬浮文件夹");

        float scale = 1f;

        RowsView list;
        BuffPanel iconView;
        BuffPanel slim;
        Label lblTitle, lblAdd, lblMore, lblFold, lblClose, lblEmpty, lblToast;
        BuffPanel grip;
        ToolTip tipPath;

        List<Item> items = new List<Item>();
        Dictionary<string, Image> iconCache = new Dictionary<string, Image>();
        string folder;
        string sortMode = "name";
        string viewMode = "list";
        string iconSize = "m";
        bool topmost = false;
        bool collapsed = false;

        int expandedW, expandedH;
        int posX = -1, posY = -1;

        int hover = -1;
        int iconHover = -1;
        int iconScroll = 0;
        string lastSelPath = null;

        bool dragging = false, moved = false;
        Point dragMouse, dragForm;

        bool resizing = false;
        Point resizeMouse;
        int resizeW, resizeH;
        bool freshStart = false;

        Color themeBg = Color.FromArgb(28, 30, 35);
        float themeOpacity = 1f;
        int themeRadius = 14;
        int bgImg = 0;
        int opLevel = 0;
        Size lastRegionSize = Size.Empty;
        bool suspendLayout = false;
        SettingsForm settingsDlg;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // 不开 WS_EX_COMPOSITED（0x02000000）：该复合样式会在移动时整体重排合成子控件，
                // 拖动容易掉帧；防闪烁交给双缓冲即可，流畅优先。
                return cp;
            }
        }

        const int WM_SYSCOMMAND = 0x0112;
        const int SC_MINIMIZE = 0xF020;
        const int GWL_HWNDPARENT = -8;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        // 把窗口的“所有者”挂到桌面窗口（Progman）：
        // 按下 Win+D 显示桌面时，桌面被提到最前，而被它拥有的窗口永远在它上面，
        // 于是卡片照样浮在桌面上；普通程序窗口仍然可以正常盖住它。
        void AttachToDesktop()
        {
            if (!IsHandleCreated) return;
            try
            {
                IntPtr desk = GetShellWindow();
                if (desk == IntPtr.Zero) return;
                if (IntPtr.Size == 8) SetWindowLongPtr64(Handle, GWL_HWNDPARENT, desk);
                else SetWindowLong32(Handle, GWL_HWNDPARENT, (int)desk);
            }
            catch { }
        }

        // 万一还是被最小化了（有的系统会走 SW_MINIMIZE 而不是 SC_MINIMIZE），立刻还原
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt32() & 0xFFF0) == SC_MINIMIZE) return;
            base.WndProc(ref m);
        }

        FileSystemWatcher watcher;
        System.Windows.Forms.Timer refreshTimer;
        System.Windows.Forms.Timer deskTimer;
        System.Windows.Forms.Timer toastTimer;
        bool dirty = false;

        public Widget()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Ui.BG;
            Text = APP_NAME;
            MinimumSize = new Size(Ui.S(250), Ui.S(210));

            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) scale = Math.Max(1f, g.DpiX / 96f);
            }
            catch (Exception) { scale = 1f; }

            LoadCfg();
            Ui.BgImg = bgImg;
            Ui.OpLevel = opLevel;
            if (bgImg > 0) themeBg = Ui.BgBaseColor(bgImg);
            Ui.ApplyTheme(themeBg, themeOpacity, themeRadius);
            bool startCollapsed = collapsed;
            collapsed = false;

            Size = new Size(expandedW, expandedH);
            if (posX >= 0 && posY >= 0)
            {
                Location = new Point(posX, posY);
            }
            else
            {
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                Location = new Point(wa.Right - expandedW - Ui.S(60), wa.Top + Ui.S(120));
            }

            BuildUi();
            ApplyLayout();
            UpdateRegion();
            RefreshItems();
            StartWatcher();
            TopMost = topmost;
            Opacity = 1.0;
            ClampToScreen();

            if (startCollapsed) SetCollapsed(true, false);

            if (freshStart && !collapsed)
            {
                int h = Ui.S(112) + items.Count * Ui.S(34);
                if (h < Ui.S(290)) h = Ui.S(290);
                if (h > Ui.S(540)) h = Ui.S(540);
                expandedH = h;
                Size = new Size(expandedW, expandedH);
                UpdateRegion();
                ApplyLayout();
                ClampToScreen();
            }
            SaveCfg();
            RepaintAll();

            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 700;
            refreshTimer.Tick += delegate
            {
                if (dirty)
                {
                    dirty = false;
                    RefreshItems();
                }
            };
            refreshTimer.Start();

            // 定期重新挂到桌面窗口：资源管理器重启后 Progman 会换句柄
            deskTimer = new System.Windows.Forms.Timer();
            deskTimer.Interval = 4000;
            deskTimer.Tick += delegate { AttachToDesktop(); };
            deskTimer.Start();

            toastTimer = new System.Windows.Forms.Timer();
            toastTimer.Interval = 2200;
            toastTimer.Tick += delegate
            {
                toastTimer.Stop();
                lblToast.Visible = false;
            };

            // 后台抓取完成后，回到 UI 线程重画一次；拖动/缩放中途不抢着重绘
            Behind.AttachReady(delegate
            {
                try { if (IsHandleCreated && !IsDisposed && !dragging && !resizing) BeginInvoke((Action)RepaintAll); } catch { }
            });

            // 背景改为内置图片/纯色，不再定时抓取身后窗口快照
        }

        int S(float v) { return Ui.S(v); }

        // ---------------- 配置 ----------------
        void LoadCfg()
        {
            folder = null;
            freshStart = !File.Exists(CfgPath);
            expandedW = Ui.S(310);
            expandedH = Ui.S(470);
            try
            {
                if (File.Exists(CfgPath))
                {
                    foreach (string line in File.ReadAllLines(CfgPath, Encoding.UTF8))
                    {
                        int idx = line.IndexOf('=');
                        if (idx <= 0) continue;
                        string k = line.Substring(0, idx).Trim();
                        string v = line.Substring(idx + 1).Trim();
                        if (k == "folder") folder = v;
                        else if (k == "sort") sortMode = v;
                        else if (k == "view") viewMode = v;
                        else if (k == "iconsize") iconSize = v;
                        else if (k == "topmost") topmost = (v == "1");
                        else if (k == "collapsed") collapsed = (v == "1");
                        else if (k == "x") int.TryParse(v, out posX);
                        else if (k == "y") int.TryParse(v, out posY);
                        else if (k == "w") { int w = 0; if (int.TryParse(v, out w) && w > 0) expandedW = w; }
                        else if (k == "h") { int h = 0; if (int.TryParse(v, out h) && h > 0) expandedH = h; }
                        else if (k == "bg")
                        {
                            string hx = v.TrimStart('#');
                            if (hx.Length == 6)
                            {
                                try
                                {
                                    int rv = Convert.ToInt32(hx.Substring(0, 2), 16);
                                    int gv = Convert.ToInt32(hx.Substring(2, 2), 16);
                                    int bv = Convert.ToInt32(hx.Substring(4, 2), 16);
                                    themeBg = Color.FromArgb(255, rv, gv, bv);
                                }
                                catch (Exception) { }
                            }
                        }
                        else if (k == "opacity")
                        {
                            // 透明度选项已从设置面板移除：背景恒为全不透明
                            themeOpacity = 1f;
                            opLevel = 0;
                        }
                        else if (k == "radius")
                        {
                            int rad = 14;
                            if (int.TryParse(v, out rad)) themeRadius = Math.Max(0, Math.Min(26, rad));
                        }
                        else if (k == "bgimg")
                        {
                            int bi = 0;
                            if (int.TryParse(v, out bi)) bgImg = Math.Max(0, Math.Min(4, bi));
                        }
                    }
                }
            }
            catch (Exception) { }

            if (expandedW < Ui.S(250)) expandedW = Ui.S(310);
            if (expandedH < Ui.S(220)) expandedH = Ui.S(470);
            if (viewMode != "icons") viewMode = "list";
            if (iconSize != "s" && iconSize != "l") iconSize = "m";

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) folder = DefaultDir;
            EnsureFolder();
        }

        void SaveCfg()
        {
            try
            {
                Directory.CreateDirectory(CfgDir);
                string[] lines = new string[]
                {
                    "folder=" + folder,
                    "sort=" + sortMode,
                    "view=" + viewMode,
                    "iconsize=" + iconSize,
                    "topmost=" + (topmost ? "1" : "0"),
                    "collapsed=" + (collapsed ? "1" : "0"),
                    "x=" + Location.X,
                    "y=" + Location.Y,
                    "w=" + expandedW,
                    "h=" + expandedH,
                    "bg=" + themeBg.R.ToString("X2") + themeBg.G.ToString("X2") + themeBg.B.ToString("X2"),
                    "opacity=" + Ui.OpLevelValue[opLevel].ToString("0.##"),
                    "radius=" + themeRadius,
                    "bgimg=" + bgImg
                };
                File.WriteAllLines(CfgPath, lines, Encoding.UTF8);
            }
            catch (Exception) { }
        }

        void EnsureFolder()
        {
            try
            {
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                if (Directory.GetFileSystemEntries(folder).Length == 0)
                {
                    File.WriteAllText(Path.Combine(folder, "把文件拖到这个文件夹.txt"),
                        "这里是「悬浮文件夹」的默认目录。\r\n\r\n" +
                        "· 把文件直接拖到桌面上的悬浮窗口，就会复制到这里\r\n" +
                        "· 双击任意条目即可打开\r\n" +
                        "· 点窗口右上角 ⋯ 可以切换成你自己的文件夹\r\n", Encoding.UTF8);
                }
            }
            catch (Exception) { }
        }

        // ---------------- 外观 / 设置 ----------------
        public Color ThemeBg { get { return themeBg; } }
        public float ThemeOpacity { get { return themeOpacity; } }
        public int ThemeRadius { get { return themeRadius; } }
        public int BgImgState { get { return bgImg; } }
        public int OpLevelState { get { return opLevel; } }
        public string ViewState { get { return viewMode; } }
        public string SortState { get { return sortMode; } }
        public bool AutoStartOn { get { return ShellUtil.AutostartEnabled; } }
        public bool TopMostOn { get { return topmost; } }

        public void SaveNow() { SaveCfg(); }

        public void OpenSettings()
        {
            if (settingsDlg != null && !settingsDlg.IsDisposed)
            {
                settingsDlg.Activate();
                return;
            }
            settingsDlg = new SettingsForm(this);
            settingsDlg.Show(this);
        }

        public void SetAppearance(Color bg, float opacity, int radius, int bgImg = -1, int opLevel = -1)
        {
            themeBg = bg;
            themeOpacity = opacity;
            themeRadius = radius;
            if (opLevel >= 0) this.opLevel = opLevel;
            Ui.OpLevel = this.opLevel;
            if (bgImg >= 0) { this.bgImg = bgImg; Ui.BgImg = bgImg; }
            Ui.ApplyTheme(bg, opacity, radius);
            BackColor = Ui.BG;
            if (list != null) { list.BackColor = Color.Transparent; }
            if (iconView != null) iconView.BackColor = Color.Transparent;
            if (slim != null) slim.BackColor = Color.Transparent;
            if (grip != null) grip.BackColor = Color.Transparent;
            if (lblToast != null) { lblToast.BackColor = Ui.TOAST; lblToast.ForeColor = Ui.TXT; }
            if (lblTitle != null) { lblTitle.ForeColor = Ui.TXT; lblTitle.BackColor = Color.Transparent; }
            if (lblEmpty != null) { lblEmpty.ForeColor = Ui.DIM; lblEmpty.BackColor = Color.Transparent; }
            Label[] bs = new Label[] { lblAdd, lblMore, lblFold, lblClose };
            for (int i = 0; i < bs.Length; i++)
            {
                if (bs[i] != null) { bs[i].ForeColor = Ui.DIM; bs[i].BackColor = Color.Transparent; }
            }
            Opacity = 1.0;   // 窗口整体不透明，只让背景半透明
            lastRegionSize = Size.Empty;
            UpdateRegion();
            ApplyLayout();
            RepaintAll();
            if (settingsDlg != null && !settingsDlg.IsDisposed) settingsDlg.RefreshTheme();
        }

        public void SetBgImg(int idx)
        {
            bgImg = Math.Max(0, Math.Min(4, idx));
            Ui.BgImg = bgImg;
            Color baseC = bgImg > 0 ? Ui.BgBaseColor(bgImg) : themeBg;
            themeBg = baseC;
            Ui.OpLevel = opLevel;
            Ui.ApplyTheme(baseC, themeOpacity, themeRadius);
            BackColor = Ui.BG;
            if (list != null) list.BackColor = Color.Transparent;
            if (iconView != null) iconView.BackColor = Color.Transparent;
            if (slim != null) slim.BackColor = Color.Transparent;
            if (grip != null) grip.BackColor = Color.Transparent;
            lastRegionSize = Size.Empty;
            UpdateRegion();
            ApplyLayout();
            RepaintAll();
            SaveCfg();
            if (settingsDlg != null && !settingsDlg.IsDisposed) settingsDlg.RefreshTheme();
        }

        public void SetViewFromSettings(string mode) { SetView(mode); }

        public string IconSizeState { get { return iconSize; } }

        public void SetIconSizeFromSettings(string s)
        {
            iconSize = (s == "s" || s == "l") ? s : "m";
            iconScroll = 0;
            RefreshItems();
            SaveCfg();
        }

        public void SetSortFromSettings(string mode) { SetSort(mode); }

        public void SetAutoStartFromSettings(bool on)
        {
            try
            {
                ShellUtil.SetAutostart(on);
                Toast(on ? "已开启开机自启动" : "已关闭开机自启动");
            }
            catch (Exception ex) { Toast("设置失败：" + ex.Message); }
        }

        public void SetTopMostFromSettings(bool on)
        {
            topmost = on;
            TopMost = on;
            SaveCfg();
        }

        // ---------------- 界面 ----------------
        Label MkLabel(string text, Font font, Color fore)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = font;
            l.ForeColor = fore;
            l.BackColor = Color.Transparent;
            l.AutoSize = false;
            l.TextAlign = ContentAlignment.MiddleLeft;
            return l;
        }

        Label MkBtn(string text, Font font)
        {
            Label b = MkLabel(text, font, Ui.DIM);
            b.TextAlign = ContentAlignment.MiddleCenter;
            b.Cursor = Cursors.Hand;
            b.MouseEnter += delegate { b.ForeColor = Ui.TXT; };
            b.MouseLeave += delegate { b.ForeColor = Ui.DIM; };
            return b;
        }

        void BuildUi()
        {
            lblTitle = MkLabel("", Ui.FBold, Ui.TXT);
            lblTitle.AutoEllipsis = true;
            lblTitle.DoubleClick += delegate { OpenPath(folder); };
            lblTitle.MouseDown += HeaderDown;
            lblTitle.MouseMove += HeaderMove;
            lblTitle.MouseUp += HeaderUp;
            Controls.Add(lblTitle);

            lblAdd = MkBtn("＋", Ui.FGlyph);
            lblAdd.Click += delegate { AddFiles(); };
            Controls.Add(lblAdd);

            lblMore = MkBtn("⋯", Ui.FGlyph);
            lblMore.Click += delegate
            {
                Point sp = lblMore.PointToScreen(new Point(0, lblMore.Height + Ui.S(4)));
                Rectangle wa = Screen.FromPoint(sp).WorkingArea;
                int x = Math.Max(wa.Left + Ui.S(8), Math.Min(sp.X - Ui.S(150), wa.Right - Ui.S(170)));
                ShowMainMenu(new Point(x, sp.Y));
            };
            Controls.Add(lblMore);

            lblFold = MkBtn("▾", Ui.FGlyph);
            lblFold.Click += delegate { SetCollapsed(true, true); };
            Controls.Add(lblFold);

            lblClose = MkBtn("✕", Ui.FGlyph);
            lblClose.Click += delegate { Close(); };
            Controls.Add(lblClose);

            list = new RowsView();
            list.owner = this;
            list.BackColor = Color.Transparent;
            list.ItemHeight = Ui.S(34);
            list.AllowDrop = true;
            list.MouseMove += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left && dragArm)
                {
                    int dx = Math.Abs(e.X - dragFrom.X), dy = Math.Abs(e.Y - dragFrom.Y);
                    if (dx > SystemInformation.DragSize.Width || dy > SystemInformation.DragSize.Height)
                    {
                        dragArm = false;
                        int i = list.SelectedIndex;
                        if (i >= 0 && i < items.Count) DragOut(items[i]);
                    }
                    return;
                }
                int idx = list.IndexFromPoint(list.PointToClient(Cursor.Position));
                if (idx != hover)
                {
                    hover = idx;
                    list.Invalidate();
                }
            };
            list.MouseLeave += delegate { hover = -1; list.Invalidate(); };
            list.MouseDoubleClick += delegate
            {
                int idx = list.IndexFromPoint(list.PointToClient(Cursor.Position));
                if (idx >= 0 && idx < items.Count) OpenPath(items[idx].Path);
            };
            list.MouseDown += delegate(object s, MouseEventArgs e)
            {
                list.Focus();
                int idx = list.IndexFromPoint(e.Location);
                if (e.Button == MouseButtons.Left && idx >= 0)
                {
                    bool already = lastSelPath != null && string.Equals(items[idx].Path, lastSelPath, StringComparison.OrdinalIgnoreCase);
                    bool onName = e.X > Ui.S(44);
                    list.SelectedIndex = idx;
                    lastSelPath = items[idx].Path;
                    dragArm = true;
                    dragFrom = e.Location;
                    if (already && onName && e.Clicks == 1) { RenameItem(items[idx]); return; }
                    if (slim != null) slim.Invalidate();
                }
                if (e.Button == MouseButtons.Right)
                {
                    if (idx >= 0 && idx < items.Count)
                    {
                        list.SelectedIndex = idx;
                        lastSelPath = items[idx].Path;
                        ShowRowMenu(items[idx]);
                    }
                    else ShowMainMenu(Cursor.Position);
                }
            };
            list.KeyDown += delegate(object s, KeyEventArgs e)
            {
                int idx = list.SelectedIndex;
                if (e.KeyCode == Keys.Delete && idx >= 0 && idx < items.Count)
                    RemoveItem(items[idx]);
                else if (e.KeyCode == Keys.Enter && idx >= 0 && idx < items.Count)
                    OpenPath(items[idx].Path);
                else if (e.KeyCode == Keys.Down && idx < items.Count - 1)
                {
                    list.SelectedIndex = idx + 1;
                    EnsureVisible(list.SelectedIndex);
                }
                else if (e.KeyCode == Keys.Up && idx > 0)
                {
                    list.SelectedIndex = idx - 1;
                    EnsureVisible(list.SelectedIndex);
                }
                if (slim != null) slim.Invalidate();
            };
            list.MouseWheel += delegate(object s, MouseEventArgs e)
            {
                int dir = e.Delta > 0 ? -3 : 3;
                list.TopIndex = list.TopIndex + dir;
                list.Invalidate();
                if (slim != null) slim.Invalidate();
            };
            Controls.Add(list);

            iconView = new BuffPanel();
            iconView.BackColor = Color.Transparent;
            iconView.Visible = false;
            iconView.MouseMove += IconMouseMove;
            iconView.MouseLeave += delegate
            {
                if (iconHover != -1)
                {
                    iconHover = -1;
                    iconView.Invalidate();
                }
            };
            iconView.MouseDown += IconMouseDown;
            iconView.MouseDoubleClick += delegate(object s, MouseEventArgs e)
            {
                int idx = IconIndexAt(e.Location);
                if (idx >= 0) OpenPath(items[idx].Path);
            };
            iconView.MouseWheel += IconWheel;
            iconView.Paint += IconPaint;
            Controls.Add(iconView);

            slim = new BuffPanel();
            slim.BackColor = Color.Transparent;
            slim.Cursor = Cursors.Hand;
            slim.Paint += delegate(object s, PaintEventArgs e) { PaintSlim(e.Graphics); };
            slim.MouseDown += delegate(object s, MouseEventArgs e) { slimDraggingTo(e.Y); };
            slim.MouseMove += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) slimDraggingTo(e.Y);
            };
            slim.MouseWheel += delegate(object s, MouseEventArgs e)
            {
                int dir = e.Delta > 0 ? -3 : 3;
                ScrollToRow(OffsetRows() + dir);
            };
            Controls.Add(slim);

            lblEmpty = MkLabel("", Ui.FSmall, Ui.DIM);
            lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
            lblEmpty.Text = "这个文件夹还是空的\n把文件拖到窗口上，或者点右上角的 ＋";
            Controls.Add(lblEmpty);

            lblToast = MkLabel("", Ui.FSmall, Ui.TXT);
            lblToast.BackColor = Ui.TOAST;
            lblToast.TextAlign = ContentAlignment.MiddleCenter;
            lblToast.AutoSize = false;
            lblToast.Visible = false;
            Controls.Add(lblToast);

            grip = new BuffPanel();
            grip.BackColor = Color.Transparent;
            grip.Cursor = Cursors.SizeNWSE;
            grip.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // 三道 45° 斜纹，右下角坐标缩放器：深浅配色下都看得清
                Color cc = Color.FromArgb(215, Ui.TXT);
                using (Pen pen = new Pen(cc, Math.Max(1f, scale * 1.15f)))
                {
                    float w = grip.ClientSize.Width, h = grip.ClientSize.Height;
                    for (int i = 0; i < 3; i++)
                    {
                        float len = S(4.5f) + i * S(4.5f);
                        e.Graphics.DrawLine(pen, w - S(3) - len, h - S(3), w - S(3), h - S(3) - len);
                    }
                }
            };
            grip.MouseDown += delegate
            {
                resizing = true;
                suspendLayout = true;
                resizeMouse = Cursor.Position;
                resizeW = Width;
                resizeH = Height;
            };
            grip.MouseMove += delegate
            {
                if (!resizing) return;
                Point p = Cursor.Position;
                int w = Math.Max(MinimumSize.Width, resizeW + p.X - resizeMouse.X);
                int h = Math.Max(MinimumSize.Height, resizeH + p.Y - resizeMouse.Y);
                if (w == Width && h == Height) return;
                Size = new Size(w, h);
                // 顶部一排按钮和手柄本身即时跟着走；只有列表等内容等松手后再整体重排
                ApplyHeader();
                expandedW = w;
                expandedH = h;
            };
            grip.MouseUp += delegate
            {
                if (!resizing) return;
                resizing = false;
                suspendLayout = false;
                ApplyLayout();
                CaptureBehind();
                RepaintAll();
                SaveCfg();
            };
            Controls.Add(grip);

            AllowDrop = true;
            DragEnter += DragEnterEv;
            DragOver += DragOverEv;
            DragDrop += DragDropEv;

            // 卡片里没有输入框，把键盘操作统一提到窗口层，列表/图标视图都能用
            KeyPreview = true;
            KeyDown += WidgetKeyDown;
            MouseDown += HeaderDown;
            MouseMove += HeaderMove;
            MouseUp += HeaderUp;

            Control[] drops = new Control[] { lblTitle, lblEmpty, lblToast, grip, slim, iconView, list };
            for (int i = 0; i < drops.Length; i++)
            {
                drops[i].AllowDrop = true;
                drops[i].DragEnter += DragEnterEv;
                drops[i].DragOver += DragOverEv;
                drops[i].DragDrop += DragDropEv;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // 整窗先铺“原样壁纸”，再把带主题色的半透明层按圆角路径填充（FillPath 走抗锯齿，
            // 圆角是平滑弧线）；圆角外侧就是桌面本身的像素，观感等同于透明。
            Glass.FillRaw(this, g);
            int rad = Ui.S(Ui.Radius);
            using (GraphicsPath p = Ui.RoundPath(new RectangleF(0, 0, Width, Height), rad))
            {
                Bitmap bmp = Ui.GetBakedBg(Ui.BgImg, Ui.OpLevel);
                if (bmp != null)
                {
                    g.SetClip(p);
                    g.DrawImage(bmp, 0, 0, Width, Height);
                    g.ResetClip();
                }
                else
                {
                    Color solid = Ui.Mix(Ui.BG, Color.White, Ui.Veil(Ui.OpLevel));
                    using (SolidBrush b = new SolidBrush(solid)) g.FillPath(b, p);
                }
            }
            using (GraphicsPath p = Ui.RoundPath(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), Math.Max(0f, rad - 0.5f)))
            {
                // 极浅的描边：只用来把卡片和壁纸区分开，太重会让圆角在缩放下显得一圈锯齿
                using (Pen pen = new Pen(Color.FromArgb(70, Ui.TXT), 1)) g.DrawPath(pen, p);
            }

            if (collapsed)
            {
                float cw = Width * 0.56f, ch = Height * 0.40f;
                RectangleF fr = new RectangleF((Width - cw) / 2f, (Height - ch) / 2f - Ui.S(3), cw, ch);
                DrawFolderGlyph(g, fr, Ui.ACC);
                int n = items.Count;
                string txt = n > 99 ? "99+" : n.ToString();
                float d = Math.Min(Ui.S(26), Width * 0.30f);
                float bx = fr.Right - Ui.S(2);
                float by = fr.Bottom - Ui.S(1);
                using (SolidBrush b = new SolidBrush(Ui.BG)) g.FillEllipse(b, bx - d / 2, by - d / 2, d, d);
                using (Pen pen = new Pen(Ui.ACC, 1)) g.DrawEllipse(pen, bx - d / 2, by - d / 2, d, d);
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                using (SolidBrush b = new SolidBrush(Ui.ACC))
                    g.DrawString(txt, Ui.FSmallBold, b, new RectangleF(bx - d / 2, by - d / 2, d, d), sf);
                return;
            }

            int pad = Ui.S(8), top = Ui.S(6), hh = Ui.S(46);
            RectangleF iconR = new RectangleF(pad + Ui.S(9), top + (hh - Ui.S(16)) / 2f, Ui.S(20), Ui.S(16));
            DrawFolderGlyph(g, iconR, Ui.ACC);

            using (Pen pen = new Pen(Ui.LINE_C, 1))
                g.DrawLine(pen, Ui.S(14), top + hh + Ui.S(2), Width - Ui.S(14), top + hh + Ui.S(2));
        }

        void DrawFolderGlyph(Graphics g, RectangleF r, Color body)
        {
            float tabW = r.Width * 0.42f;
            float tabH = r.Height * 0.26f;
            using (GraphicsPath p = Ui.RoundPath(new RectangleF(r.X, r.Y, tabW, tabH + r.Height * 0.10f), Ui.S(4)))
            using (SolidBrush b = new SolidBrush(Ui.ACC_D)) g.FillPath(b, p);
            using (GraphicsPath p = Ui.RoundPath(new RectangleF(r.X, r.Y + tabH, r.Width, r.Height - tabH), Ui.S(5)))
            using (SolidBrush b = new SolidBrush(body)) g.FillPath(b, p);
        }

        void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            Region old = Region;
            using (GraphicsPath path = Ui.RoundPath(new RectangleF(0, 0, Width, Height), Ui.S(Ui.Radius)))
            {
                Region = new Region(path);
            }
            if (old != null) old.Dispose();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (slim == null) return;
            // 只有尺寸真的变了才重建圆角区域，避免拖动时每一帧都换 Region 造成闪烁
            if (lastRegionSize.Width != Width || lastRegionSize.Height != Height)
            {
                lastRegionSize = Size;
                UpdateRegion();
            }
            // 拖动右下角期间不做全量布局，松手后再统一重排，彻底消除拖动闪烁；
            // 但顶部一排按钮和右下角手柄必须每一帧都跟着窗口边缘走。
            if (!suspendLayout) ApplyLayout(); else ApplyHeader();
            // 缩放过程中不重抓背景、不全量重绘，松手后由手柄 MouseUp 补一轮
            if (!resizing) { CaptureBehind(); RepaintAll(); }
        }

        // 背景取自壁纸：位置/尺寸一变，父窗口与每个子控件的背景切片都必须重画。
        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            if (!IsHandleCreated) return;
            // 拖动过程中不重抓背景、不强制全量重绘，保证窗口跟手；松手后由 HeaderUp 补抓一轮
            if (dragging) return;
            CaptureBehind();
            Invalidate(true);
        }

        void CaptureBehind()
        {
            // 不再抓取身后窗口：背景由内置图片/纯色提供
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            AttachToDesktop();
            CaptureBehind();
            RepaintAll();
        }

        // 顶部标题 + 右上角四个按钮 + 右下角缩放手柄的位置（只依赖窗口宽高，可单独调用）
        // 手柄必须整个落在圆角弧线内，否则它自己那块方形底会在圆角外露出一小块（圆角调大时最明显）
        int GripSize() { return Ui.S(20); }
        int GripInset()
        {
            int r = Ui.S(Ui.Radius);
            int d = (int)Math.Ceiling(r * 0.30f) + Ui.S(2);
            return Math.Max(Ui.S(6), d);
        }

        void ApplyHeader()
        {
            if (collapsed) return;
            if (lblTitle == null || grip == null) return;
            int pad = Ui.S(8), top = Ui.S(6), hh = Ui.S(46);
            int bw = Ui.S(26), bh = Ui.S(28);
            int titleX = pad + Ui.S(38);
            int buttonsW = bw * 4 + Ui.S(6);
            lblTitle.Bounds = new Rectangle(titleX, top + Ui.S(8),
                Math.Max(Ui.S(40), Width - titleX - buttonsW - Ui.S(10)), hh - Ui.S(14));

            int x = Width - Ui.S(10) - bw;
            int y = top + (hh - bh) / 2;
            lblClose.Bounds = new Rectangle(x, y, bw, bh);
            x -= bw;
            lblFold.Bounds = new Rectangle(x, y, bw, bh);
            x -= bw;
            lblMore.Bounds = new Rectangle(x, y, bw, bh);
            x -= bw;
            lblAdd.Bounds = new Rectangle(x, y, bw, bh);

            grip.Bounds = new Rectangle(Width - GripInset() - GripSize(), Height - GripInset() - GripSize(),
                GripSize(), GripSize());
            grip.BringToFront();
        }

        void ApplyLayout()
        {
            if (collapsed)
            {
                lblTitle.Visible = false;
                lblAdd.Visible = false;
                lblMore.Visible = false;
                lblFold.Visible = false;
                lblClose.Visible = false;
                list.Visible = false;
                iconView.Visible = false;
                lblEmpty.Visible = false;
                lblToast.Visible = false;
                grip.Visible = false;
                slim.Visible = false;
                return;
            }

            int pad = Ui.S(8), top = Ui.S(6), hh = Ui.S(46);
            lblTitle.Visible = true;
            lblAdd.Visible = true;
            lblMore.Visible = true;
            lblFold.Visible = true;
            lblClose.Visible = true;
            grip.Visible = true;
            ApplyHeader();

            int listY = top + hh + Ui.S(5);
            // 底部给缩放手柄留位（手柄内缩多少随圆角变化，列表不能让手压住）
            int bottomRes = GripInset() + GripSize() + Ui.S(3);
            int listH = Math.Max(Ui.S(40), Height - listY - bottomRes);
            int listW = Math.Max(Ui.S(60), Width - Ui.S(12));
            Rectangle area = new Rectangle(Ui.S(6), listY, listW, listH);
            list.Bounds = area;
            list.ItemHeight = Ui.S(34);
            list.ClampTop();
            list.Visible = (viewMode == "list");

            bool needScroll = items.Count > 0 && TotalRows() > VisRows();
            // 出现滚动条时给右侧留出宽度，免得文件名/大小被滚动条压住
            if (needScroll) list.Bounds = new Rectangle(area.X, area.Y, Math.Max(Ui.S(40), area.Width - Ui.S(18)), area.Height);

            iconView.Bounds = new Rectangle(area.X, area.Y, Math.Max(Ui.S(40), area.Width - (needScroll ? Ui.S(18) : Ui.S(8))), area.Height);
            iconView.Visible = (viewMode == "icons");

            lblEmpty.Bounds = new Rectangle(Ui.S(16), listY + Ui.S(28), Width - Ui.S(32), Ui.S(60));
            lblEmpty.Visible = items.Count == 0;

            slim.Visible = needScroll;
            slim.Bounds = new Rectangle(area.Right - Ui.S(17), area.Top, Ui.S(17), area.Height);
            if (needScroll) slim.BringToFront();
            grip.BringToFront();
        }

        // ---------------- 滚动（列表 / 图标共用） ----------------
        int IconCols()
        {
            int w = Math.Max(1, iconView.ClientSize.Width - Ui.S(6));
            int cols = w / Ui.S(86);
            if (cols < 2) cols = 2;
            if (cols > 6) cols = 6;
            return cols;
        }

        int IconSizePx() { return iconSize == "s" ? Ui.S(24) : iconSize == "l" ? Ui.S(44) : Ui.S(32); }

        int IconCellH() { return iconSize == "s" ? Ui.S(76) : iconSize == "l" ? Ui.S(108) : Ui.S(92); }

        int TotalRows()
        {
            if (viewMode == "icons")
            {
                int cols = IconCols();
                return (items.Count + cols - 1) / cols;
            }
            return items.Count;
        }

        int VisRows()
        {
            if (viewMode == "icons")
            {
                return Math.Max(1, iconView.ClientSize.Height / Math.Max(1, IconCellH()));
            }
            return list.VisibleRows;
        }

        int OffsetRows()
        {
            if (viewMode == "icons") return iconScroll / Math.Max(1, IconCellH());
            return list.TopIndex;
        }

        int MaxIconScroll()
        {
            int rows = TotalRows();
            int need = rows * IconCellH() - iconView.ClientSize.Height;
            return Math.Max(0, need);
        }

        void ScrollToRow(int row)
        {
            if (viewMode == "icons")
            {
                iconScroll = Math.Max(0, Math.Min(MaxIconScroll(), row * IconCellH()));
                iconView.Invalidate();
            }
            else
            {
                if (items.Count == 0) return;
                list.TopIndex = Math.Max(0, Math.Min(items.Count - 1, row));
            }
            if (slim != null) slim.Invalidate();
        }

        void slimDraggingTo(int y)
        {
            int total = TotalRows();
            int vis = VisRows();
            int h = slim.Height;
            if (total <= vis || h <= 0) return;
            int thumbH = Math.Max(Ui.S(24), (int)(h * (double)vis / total));
            if (thumbH > h) thumbH = h;
            double ratio = (double)(y - thumbH / 2) / Math.Max(1, h - thumbH);
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;
            ScrollToRow((int)Math.Round(ratio * (total - vis)));
            slim.Invalidate();
        }

        void PaintSlim(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int total = TotalRows();
            int vis = VisRows();
            int h = slim.Height;
            if (total <= vis || h <= 0) return;
            int thumbH = Math.Max(Ui.S(24), (int)(h * (double)vis / total));
            if (thumbH > h) thumbH = h;
            int maxTop = Math.Max(1, total - vis);
            int top = (int)((h - thumbH) * (double)Math.Min(OffsetRows(), maxTop) / maxTop);
            RectangleF r = new RectangleF(1, top, slim.Width - 2, thumbH);
            using (GraphicsPath p = Ui.RoundPath(r, (slim.Width - 2) / 2f))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(88, 95, 107))) g.FillPath(b, p);
        }

        // ---------------- 图标视图 ----------------
        int IconIndexAt(Point p)
        {
            int cols = IconCols();
            int w = Math.Max(1, iconView.ClientSize.Width - Ui.S(6));
            int cellW = Math.Max(1, w / cols);
            int cellH = IconCellH();
            int c = (p.X - Ui.S(3)) / cellW;
            int r = (p.Y + iconScroll) / cellH;
            if (c < 0 || c >= cols || p.Y < 0) return -1;
            int i = r * cols + c;
            if (i < 0 || i >= items.Count) return -1;
            return i;
        }

        Point dragFrom = Point.Empty;
        bool dragArm = false;

        void IconMouseMove(object s, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && dragArm)
            {
                int dx = Math.Abs(e.X - dragFrom.X), dy = Math.Abs(e.Y - dragFrom.Y);
                if (dx > SystemInformation.DragSize.Width || dy > SystemInformation.DragSize.Height)
                {
                    dragArm = false;
                    int di = IconIndexAt(dragFrom);
                    if (di >= 0 && di < items.Count) DragOut(items[di]);
                }
                return;
            }
            int idx = IconIndexAt(e.Location);
            if (idx != iconHover)
            {
                iconHover = idx;
                iconView.Invalidate();
            }
        }

        void IconMouseDown(object s, MouseEventArgs e)
        {
            int idx = IconIndexAt(e.Location);
            if (e.Button == MouseButtons.Right)
            {
                if (idx >= 0)
                {
                    lastSelPath = items[idx].Path;
                    iconView.Invalidate();
                    ShowRowMenu(items[idx]);
                }
                else ShowMainMenu(Cursor.Position);
                return;
            }
            if (idx >= 0)
            {
                bool already = lastSelPath != null && string.Equals(items[idx].Path, lastSelPath, StringComparison.OrdinalIgnoreCase);
                if (already && IconNameHit(e.Location) && e.Clicks == 1)
                {
                    RenameItem(items[idx]);
                    return;
                }
                lastSelPath = items[idx].Path;
                dragArm = true;
                dragFrom = e.Location;
                iconView.Invalidate();
            }
        }

        bool IconNameHit(Point p)
        {
            int cols = IconCols();
            int w = Math.Max(1, iconView.ClientSize.Width - Ui.S(6));
            int cellW = Math.Max(1, w / cols);
            int cellH = IconCellH();
            int ry = (p.Y + iconScroll) - ((p.Y + iconScroll) / cellH) * cellH;
            return ry > Ui.S(12) + IconSizePx() + Ui.S(2);
        }

        void IconWheel(object s, MouseEventArgs e)
        {
            int h = IconCellH();
            int delta = e.Delta > 0 ? -h : h;
            iconScroll = Math.Max(0, Math.Min(MaxIconScroll(), iconScroll + delta));
            iconView.Invalidate();
            if (slim != null) slim.Invalidate();
        }

        Image ShellIcon(Item it)
        {
            string key = it.IsDir ? "<dir>" : Path.GetExtension(it.Name).ToLowerInvariant();
            if (string.IsNullOrEmpty(key)) key = "<none>";
            Image img;
            if (iconCache.TryGetValue(key, out img)) return img;
            img = ExtractIcon(it.Path);
            iconCache[key] = img;
            return img;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SHGetFileInfo(string path, uint attr, ref SHFILEINFO info, uint size, uint flags);

        [DllImport("user32.dll")]
        static extern bool DestroyIcon(IntPtr hIcon);

        const uint SHGFI_ICON = 0x100;
        const uint SHGFI_LARGEICON = 0x0;
        const uint SHGFI_USEFILEATTRIBUTES = 0x10;

        static Image ExtractIcon(string path)
        {
            try
            {
                SHFILEINFO info = new SHFILEINFO();
                uint flags = SHGFI_ICON | SHGFI_LARGEICON;
                if (!File.Exists(path) && !Directory.Exists(path)) flags |= SHGFI_USEFILEATTRIBUTES;
                SHGetFileInfo(path, 0, ref info, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), flags);
                if (info.hIcon != IntPtr.Zero)
                {
                    using (Icon ic = Icon.FromHandle(info.hIcon))
                    {
                        Bitmap bmp = new Bitmap(ic.ToBitmap());
                        DestroyIcon(info.hIcon);
                        return bmp;
                    }
                }
            }
            catch (Exception) { }
            return null;
        }

        void IconPaint(object s, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (items.Count == 0) return;
            int cols = IconCols();
            int cellW = Math.Max(1, (iconView.ClientSize.Width - Ui.S(6)) / cols);
            int cellH = IconCellH();
            int ico = IconSizePx();
            int nameY = Ui.S(12) + ico + Ui.S(6);
            int nameH = iconSize == "s" ? Ui.S(30) : Ui.S(36);
            for (int i = 0; i < items.Count; i++)
            {
                int row = i / cols, col = i % cols;
                Rectangle cell = new Rectangle(Ui.S(3) + col * cellW, -iconScroll + row * cellH, cellW, cellH);
                if (cell.Bottom < 0 || cell.Top > iconView.ClientSize.Height) continue;

                bool sel = lastSelPath != null && string.Equals(items[i].Path, lastSelPath, StringComparison.OrdinalIgnoreCase);
                bool hov = (i == iconHover);
                if (sel || hov)
                {
                    RectangleF rr = new RectangleF(cell.X + Ui.S(2), cell.Y + Ui.S(3), cell.Width - Ui.S(4), cell.Height - Ui.S(8));
                    using (GraphicsPath p = Ui.RoundPath(rr, Ui.S(6)))
                    using (SolidBrush b = new SolidBrush(sel ? Ui.BG_SEL : Ui.BG_HOVER)) g.FillPath(b, p);
                }

                Image img = ShellIcon(items[i]);
                if (img != null)
                {
                    g.DrawImage(img, cell.X + (cellW - ico) / 2, cell.Y + Ui.S(12), ico, ico);
                }
                else
                {
                    RectangleF br = new RectangleF(cell.X + (cellW - ico) / 2f, cell.Y + Ui.S(12), ico, ico);
                    using (GraphicsPath p = Ui.RoundPath(br, Ui.S(6)))
                    using (SolidBrush b = new SolidBrush(items[i].Badge)) g.FillPath(b, p);
                }

                Rectangle nameRect = new Rectangle(cell.X + Ui.S(5), cell.Y + nameY,
                    cell.Width - Ui.S(10), nameH);
                string name = FitName(g, items[i].Name, nameRect.Width, nameRect.Height);
                TextRenderer.DrawText(g, name, Ui.FSmall, nameRect, Ui.TXT,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top
                    | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        // ---------------- 数据 ----------------
        static string FitName(Graphics g, string text, int w, int h)
        {
            int measured = TextRenderer.MeasureText(g, text, Ui.FSmall, new Size(w, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
            if (measured <= h) return text;
            string s = text;
            while (s.Length > 1)
            {
                s = s.Substring(0, s.Length - 1);
                string cand = s + "…";
                int mh = TextRenderer.MeasureText(g, cand, Ui.FSmall, new Size(w, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
                if (mh <= h) return cand;
            }
            return "…";
        }

        static string HumanSize(long n)
        {
            double v = n;
            string[] u = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            while (v >= 1024 && i < u.Length - 1)
            {
                v /= 1024;
                i++;
            }
            return i == 0 ? ((long)v).ToString() + " B" : v.ToString("0.0") + " " + u[i];
        }

        static int NatCompare(string a, string b)
        {
            int i = 0, j = 0;
            while (i < a.Length && j < b.Length)
            {
                char ca = a[i], cb = b[j];
                if (char.IsDigit(ca) && char.IsDigit(cb))
                {
                    int si = i, sj = j;
                    while (i < a.Length && char.IsDigit(a[i])) i++;
                    while (j < b.Length && char.IsDigit(b[j])) j++;
                    string na = a.Substring(si, i - si).TrimStart('0');
                    string nb = b.Substring(sj, j - sj).TrimStart('0');
                    if (na.Length != nb.Length) return na.Length - nb.Length;
                    int c = string.CompareOrdinal(na, nb);
                    if (c != 0) return c;
                }
                else
                {
                    ca = char.ToLowerInvariant(ca);
                    cb = char.ToLowerInvariant(cb);
                    if (ca != cb) return ca - cb;
                    i++;
                    j++;
                }
            }
            return (a.Length - i) - (b.Length - j);
        }

        static bool InArr(string[] arr, string v)
        {
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] == v) return true;
            return false;
        }

        static void StyleFor(string name, bool isDir, out string tag, out Color color)
        {
            if (isDir)
            {
                tag = "DIR";
                color = Color.FromArgb(224, 169, 47);
                return;
            }
            string ext = Path.GetExtension(name).TrimStart('.').ToLowerInvariant();
            string[] doc = { "doc", "docx", "wps", "rtf", "odt", "pages" };
            string[] xls = { "xls", "xlsx", "xlsm", "csv", "et", "numbers" };
            string[] ppt = { "ppt", "pptx", "dps", "key" };
            string[] img = { "png", "jpg", "jpeg", "gif", "bmp", "webp", "svg", "heic", "ico", "psd", "tif", "tiff" };
            string[] vid = { "mp4", "mov", "avi", "mkv", "wmv", "flv", "webm", "m4v", "mpg" };
            string[] aud = { "mp3", "wav", "flac", "aac", "m4a", "ogg", "wma" };
            string[] zip = { "zip", "rar", "7z", "tar", "gz", "bz2", "xz" };
            string[] app = { "exe", "msi", "bat", "cmd", "ps1", "lnk" };
            string[] cad = { "dwg", "dxf", "dwt", "rvt", "ifc", "skp", "mdb", "gcz", "gqi" };
            string[] txt = { "txt", "md", "log", "ini", "json", "xml", "yaml", "yml", "conf" };
            string[] dev = { "py", "js", "ts", "java", "cpp", "c", "h", "go", "rs", "html", "css", "sql", "sh", "php" };

            if (InArr(doc, ext)) { tag = "DOC"; color = Color.FromArgb(76, 141, 255); return; }
            if (InArr(xls, ext)) { tag = "XLS"; color = Color.FromArgb(63, 178, 127); return; }
            if (InArr(ppt, ext)) { tag = "PPT"; color = Color.FromArgb(232, 102, 61); return; }
            if (ext == "pdf") { tag = "PDF"; color = Color.FromArgb(224, 90, 90); return; }
            if (InArr(img, ext)) { tag = "IMG"; color = Color.FromArgb(87, 182, 229); return; }
            if (InArr(vid, ext)) { tag = "VID"; color = Color.FromArgb(217, 113, 143); return; }
            if (InArr(aud, ext)) { tag = "AUD"; color = Color.FromArgb(103, 199, 192); return; }
            if (InArr(zip, ext)) { tag = "ZIP"; color = Color.FromArgb(169, 128, 63); return; }
            if (InArr(app, ext)) { tag = "APP"; color = Color.FromArgb(154, 161, 170); return; }
            if (InArr(cad, ext)) { tag = "CAD"; color = Color.FromArgb(118, 176, 67); return; }
            if (InArr(txt, ext)) { tag = "TXT"; color = Color.FromArgb(139, 144, 153); return; }
            if (InArr(dev, ext)) { tag = "DEV"; color = Color.FromArgb(99, 176, 111); return; }
            if (ext.Length > 0)
            {
                tag = ext.Length > 3 ? ext.Substring(0, 3).ToUpperInvariant() : ext.ToUpperInvariant();
                color = Color.FromArgb(127, 135, 145);
                return;
            }
            tag = "FILE";
            color = Color.FromArgb(127, 135, 145);
        }

        int CompareItems(Item a, Item b)
        {
            if (a.IsDir != b.IsDir) return a.IsDir ? -1 : 1;
            if (sortMode == "time")
            {
                int c = b.Modified.CompareTo(a.Modified);
                if (c != 0) return c;
            }
            else if (sortMode == "size")
            {
                int c = b.Size.CompareTo(a.Size);
                if (c != 0) return c;
            }
            return NatCompare(a.Name, b.Name);
        }

        List<Item> LoadItems()
        {
            List<Item> res = new List<Item>();
            try
            {
                foreach (string p in Directory.GetFileSystemEntries(folder))
                {
                    string nm = Path.GetFileName(p);
                    if (nm.StartsWith(".")) continue;
                    if (nm.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                    if (nm.Equals("thumbs.db", StringComparison.OrdinalIgnoreCase)) continue;
                    Item it = new Item();
                    it.Name = nm;
                    it.Path = p;
                    try
                    {
                        if (Directory.Exists(p))
                        {
                            it.IsDir = true;
                            it.Size = 0;
                            it.Modified = Directory.GetLastWriteTime(p);
                        }
                        else
                        {
                            FileInfo fi = new FileInfo(p);
                            it.Size = fi.Length;
                            it.Modified = fi.LastWriteTime;
                        }
                    }
                    catch (Exception) { }
                    string tag;
                    Color color;
                    StyleFor(nm, it.IsDir, out tag, out color);
                    it.Tag = tag;
                    it.Badge = color;
                    res.Add(it);
                }
            }
            catch (Exception) { }
            res.Sort(CompareItems);
            return res;
        }

        void RefreshItems()
        {
            string keep = lastSelPath;
            items = LoadItems();

            if (list != null)
            {
                int sel = -1;
                if (keep != null)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (string.Equals(items[i].Path, keep, StringComparison.OrdinalIgnoreCase))
                        {
                            sel = i;
                            break;
                        }
                    }
                }
                list.SelectedIndex = sel;
                list.ClampTop();
                hover = -1;
                list.Invalidate();
            }

            if (iconView != null)
            {
                iconHover = -1;
                if (iconScroll > MaxIconScroll()) iconScroll = MaxIconScroll();
                iconView.Invalidate();
            }

            UpdateTitle();
            if (lblEmpty != null) lblEmpty.Visible = items.Count == 0;
            ApplyLayout();
            if (slim != null) slim.Invalidate();
            if (collapsed) Invalidate();
        }

        void UpdateTitle()
        {
            string name = Path.GetFileName(folder.TrimEnd('\\'));
            if (string.IsNullOrEmpty(name)) name = folder;
            lblTitle.Text = name + "  ·  " + items.Count;
            if (tipPath == null)
            {
                tipPath = new ToolTip();
                tipPath.SetToolTip(lblTitle, folder);
                tipPath.SetToolTip(lblEmpty, "把文件拖到窗口上即可复制进来");
            }
            else
            {
                tipPath.SetToolTip(lblTitle, folder);
            }
        }

        public int ItemCount { get { return items == null ? 0 : items.Count; } }

        void EnsureVisible(int idx)
        {
            if (idx < 0) return;
            int top = list.TopIndex;
            int rows = list.VisibleRows;
            if (idx < top) list.TopIndex = idx;
            else if (idx >= top + rows) list.TopIndex = idx - rows + 1;
        }

        public void PaintRows(RowsView rv, Graphics g)
        {
            int rowH = Math.Max(1, rv.ItemHeight);
            int top = rv.TopIndex;
            int sel = rv.SelectedIndex;
            int vis = rv.VisibleRows;
            for (int i = top; i < items.Count && i < top + vis; i++)
            {
                Rectangle b = new Rectangle(0, (i - top) * rowH, rv.ClientSize.Width, rowH);
                if (b.Bottom > rv.ClientSize.Height) break;
                DrawRow(g, b, i, i == sel, i == hover);
            }
        }

        void DrawRow(Graphics g, Rectangle bounds, int index, bool selected, bool hov)
        {
            if (index < 0 || index >= items.Count) return;
            Item it = items[index];

            int rowH = Math.Max(1, bounds.Height);
            Rectangle hl = new Rectangle(bounds.X + Ui.S(2), bounds.Y + Ui.S(2), bounds.Width - Ui.S(4), rowH - Ui.S(4));
            if (selected || hov)
            {
                int a = selected ? 26 : 14;
                using (GraphicsPath p = Ui.RoundPath(hl, Ui.S(6)))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(a, Ui.TXT))) g.FillPath(b, p);
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;

            int pad = Ui.S(8), bw = Ui.S(27), bh = Ui.S(19);
            Rectangle badge = new Rectangle(bounds.X + pad, bounds.Y + (bounds.Height - bh) / 2, bw, bh);
            using (GraphicsPath p = Ui.RoundPath(badge, Ui.S(5)))
            using (SolidBrush b = new SolidBrush(it.Badge)) g.FillPath(b, p);

            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;
            Font bf = it.Tag.Length > 2 ? Ui.FBadgeS : Ui.FBadge;
            g.DrawString(it.Tag, bf, Brushes.White, badge, sf);

            string meta = it.IsDir ? "文件夹" : HumanSize(it.Size);
            int metaW = TextRenderer.MeasureText(meta, Ui.FSmall).Width;
            Rectangle metaRect = new Rectangle(bounds.Right - metaW - Ui.S(10),
                bounds.Y, metaW + Ui.S(2), bounds.Height);
            TextRenderer.DrawText(g, meta, Ui.FSmall, metaRect, Ui.DIM,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            Rectangle nameRect = new Rectangle(badge.Right + Ui.S(9), bounds.Y,
                Math.Max(10, metaRect.X - badge.Right - Ui.S(12)), bounds.Height);
            TextRenderer.DrawText(g, it.Name, Ui.FUI, nameRect, Ui.TXT,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        // ---------------- 动作 ----------------
        void OpenPath(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex) { Toast("打不开：" + ex.Message); }
        }

        void Reveal(string path)
        {
            try { Process.Start("explorer.exe", "/select,\"" + path + "\""); }
            catch (Exception) { }
        }

        void CopyPath(string path)
        {
            try
            {
                Clipboard.SetText(path);
                Toast("路径已复制");
            }
            catch (Exception) { }
        }

        void Toast(string msg)
        {
            if (collapsed) return;
            lblToast.Text = msg;
            int w = Math.Min(Width - Ui.S(24), TextRenderer.MeasureText(msg, Ui.FSmall).Width + Ui.S(24));
            lblToast.Size = new Size(w, Ui.S(26));
            lblToast.Location = new Point((Width - w) / 2, Height - Ui.S(44));
            lblToast.Visible = true;
            lblToast.BringToFront();
            toastTimer.Stop();
            toastTimer.Start();
        }

        void ChooseFolder()
        {
            using (FolderBrowserDialog d = new FolderBrowserDialog())
            {
                d.Description = "选择要悬浮的文件夹";
                d.SelectedPath = folder;
                if (d.ShowDialog(this) == DialogResult.OK && Directory.Exists(d.SelectedPath))
                {
                    folder = d.SelectedPath;
                    lastSelPath = null;
                    iconScroll = 0;
                    StartWatcher();
                    RefreshItems();
                    SaveCfg();
                    Toast("已切换到 " + Path.GetFileName(folder));
                }
            }
        }

        void AddFiles()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Title = "添加到悬浮文件夹";
                d.Multiselect = true;
                d.InitialDirectory = folder;
                if (d.ShowDialog(this) == DialogResult.OK)
                {
                    int n = CopyIn(d.FileNames);
                    RefreshItems();
                    Toast(n > 0 ? "已添加 " + n + " 个文件" : "添加失败");
                }
            }
        }

        static string UniquePath(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path)) return path;
            string dir = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            int i = 2;
            while (true)
            {
                string p = Path.Combine(dir, name + " (" + i + ")" + ext);
                if (!File.Exists(p) && !Directory.Exists(p)) return p;
                i++;
                if (i > 999) return p;
            }
        }

        static void CopyDir(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (string f in Directory.GetFiles(src))
                File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), false);
            foreach (string d in Directory.GetDirectories(src))
                CopyDir(d, Path.Combine(dst, Path.GetFileName(d)));
        }

        int CopyIn(string[] paths)
        {
            int n = 0;
            if (paths == null) return 0;
            string here = Path.GetFullPath(folder).TrimEnd('\\');
            foreach (string p in paths)
            {
                try
                {
                    if (string.IsNullOrEmpty(p)) continue;
                    if (!File.Exists(p) && !Directory.Exists(p)) continue;
                    string parent = Path.GetDirectoryName(Path.GetFullPath(p));
                    if (parent != null && string.Equals(parent.TrimEnd('\\'), here, StringComparison.OrdinalIgnoreCase))
                        continue;
                    string dst = UniquePath(Path.Combine(folder, Path.GetFileName(p)));
                    if (Directory.Exists(p)) CopyDir(p, dst);
                    else File.Copy(p, dst, false);
                    n++;
                }
                catch (Exception) { }
            }
            return n;
        }

        // ---------------- 拖拽 ----------------
        static bool MoveOne(string src, string dst)
        {
            try
            {
                if (Directory.Exists(src)) Directory.Move(src, dst);
                else File.Move(src, dst);
                return true;
            }
            catch (Exception)
            {
                // 跨磁盘 / 目标被占用时退回“复制 + 删除”
                try
                {
                    if (Directory.Exists(src))
                    {
                        CopyDir(src, dst);
                        Directory.Delete(src, true);
                    }
                    else
                    {
                        File.Copy(src, dst, false);
                        File.Delete(src);
                    }
                    return true;
                }
                catch (Exception) { return false; }
            }
        }

        int MoveIn(string[] paths)
        {
            int n = 0;
            if (paths == null) return 0;
            string here = Path.GetFullPath(folder).TrimEnd('\\');
            foreach (string p in paths)
            {
                try
                {
                    if (string.IsNullOrEmpty(p)) continue;
                    if (!File.Exists(p) && !Directory.Exists(p)) continue;
                    string parent = Path.GetDirectoryName(Path.GetFullPath(p));
                    if (parent != null && string.Equals(parent.TrimEnd('\\'), here, StringComparison.OrdinalIgnoreCase))
                        continue;
                    string dst = UniquePath(Path.Combine(folder, Path.GetFileName(p)));
                    if (MoveOne(p, dst)) n++;
                }
                catch (Exception) { }
            }
            return n;
        }

        void DragEnterEv(object sender, DragEventArgs e)
        {
            // 只给 Move：资源管理器拖进来会变成“移动”，而不是复制
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Move;
            else e.Effect = DragDropEffects.None;
        }

        // 拖动过程中必须持续声明“允许移动”，否则系统会把效果收回为“不允许”，松手就不会触发 Drop
        void DragOverEv(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Move;
            else e.Effect = DragDropEffects.None;
        }

        void DragDropEv(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            int n = MoveIn(paths);
            // 自己搬成功了才让资源管理器按“移动”收尾；没搬成功就报 None，避免源文件被误删
            e.Effect = (n > 0) ? DragDropEffects.Move : DragDropEffects.None;
            RefreshItems();
            Toast(n > 0 ? "已移入 " + n + " 项" : "这些已经在文件夹里了");
        }

        // 把条目从卡片里拖出去（拖到资源管理器/其它程序里），同样是移动
        void DragOut(Item it)
        {
            if (it == null) return;
            DataObject data = new DataObject();
            data.SetData(DataFormats.FileDrop, new string[] { it.Path });
            DragDropEffects eff = DoDragDrop(data, DragDropEffects.Move);
            if ((eff & DragDropEffects.Move) != 0) RefreshItems();
        }

        // ---------------- 剪贴板与快捷键 ----------------
        Item SelectedItem()
        {
            if (items == null) return null;
            if (lastSelPath != null)
            {
                for (int i = 0; i < items.Count; i++)
                    if (items[i].Path == lastSelPath) return items[i];
            }
            int idx = (list != null) ? list.SelectedIndex : -1;
            if (idx >= 0 && idx < items.Count) return items[idx];
            return null;
        }

        // 复制/剪切到剪贴板：带 CF_HDROP + Preferred DropEffect，
        // 所以粘贴到资源管理器（或本卡片）时会按“复制 / 剪切”各自的语义走
        void SetClipboard(bool cut)
        {
            Item it = SelectedItem();
            if (it == null) { Toast("先选中一个文件"); return; }
            try
            {
                DataObject d = new DataObject();
                d.SetData(DataFormats.FileDrop, new string[] { it.Path });
                byte[] eff = BitConverter.GetBytes(cut ? 2 : 1);
                MemoryStream ms = new MemoryStream();
                ms.Write(eff, 0, eff.Length);
                d.SetData("Preferred DropEffect", ms);
                Clipboard.SetDataObject(d, true);
                Toast(cut ? "已剪切：" + it.Name : "已复制：" + it.Name);
            }
            catch (Exception ex) { Toast("操作失败：" + ex.Message); }
        }

        bool ClipboardDropIsMove(IDataObject o)
        {
            try
            {
                if (!o.GetDataPresent("Preferred DropEffect")) return false;
                object v = o.GetData("Preferred DropEffect");
                byte[] raw = null;
                MemoryStream ms = v as MemoryStream;
                if (ms != null) raw = ms.ToArray();
                else if (v is byte[]) raw = (byte[])v;
                if (raw == null || raw.Length < 4) return false;
                return (BitConverter.ToInt32(raw, 0) & 2) != 0;
            }
            catch (Exception) { return false; }
        }

        void PasteFromClipboard()
        {
            string[] paths = null;
            bool cut = false;
            try
            {
                IDataObject o = Clipboard.GetDataObject();
                if (o == null || !o.GetDataPresent(DataFormats.FileDrop)) { Toast("剪贴板里没有文件"); return; }
                object raw = o.GetData(DataFormats.FileDrop);
                if (raw is string[]) paths = (string[])raw;
                if (raw is string) paths = new string[] { (string)raw };
                cut = ClipboardDropIsMove(o);
            }
            catch (Exception ex) { Toast("读剪贴板失败：" + ex.Message); return; }

            if (paths == null || paths.Length == 0) { Toast("剪贴板里没有文件"); return; }
            int n = cut ? MoveIn(paths) : CopyIn(paths);
            RefreshItems();
            if (n > 0) Toast(cut ? "已粘贴（移动 " + n + " 项）" : "已粘贴（复制 " + n + " 项）");
            else Toast("这些已经在文件夹里了");
        }

        // Ctrl+X / Ctrl+C / Ctrl+V / Delete / F2 / F5 / Enter
        void WidgetKeyDown(object s, KeyEventArgs e)
        {
            if (e.Control)
            {
                if (e.KeyCode == Keys.C) { e.Handled = true; e.SuppressKeyPress = true; SetClipboard(false); }
                else if (e.KeyCode == Keys.X) { e.Handled = true; e.SuppressKeyPress = true; SetClipboard(true); }
                else if (e.KeyCode == Keys.V) { e.Handled = true; e.SuppressKeyPress = true; PasteFromClipboard(); }
                return;
            }
            if (e.KeyCode == Keys.Delete)
            {
                Item it = SelectedItem();
                if (it == null) return;
                e.Handled = true; e.SuppressKeyPress = true;
                RemoveItem(it);
            }
            else if (e.KeyCode == Keys.F2)
            {
                Item it = SelectedItem();
                if (it == null) return;
                e.Handled = true; e.SuppressKeyPress = true;
                RenameItem(it);
            }
            else if (e.KeyCode == Keys.F5)
            {
                e.Handled = true; e.SuppressKeyPress = true;
                RefreshItems();
                Toast("已刷新");
            }
            else if (e.KeyCode == Keys.Enter)
            {
                Item it = SelectedItem();
                if (it == null) return;
                e.Handled = true; e.SuppressKeyPress = true;
                OpenPath(it.Path);
            }
        }

        void NewItem(bool isFolder)
        {
            try
            {
                string baseName = isFolder ? "新建文件夹" : "新建文本.txt";
                string p = UniquePath(Path.Combine(folder, baseName));
                if (isFolder) Directory.CreateDirectory(p);
                else File.WriteAllText(p, "", Encoding.UTF8);
                RefreshItems();
                Toast("已创建 " + Path.GetFileName(p));
            }
            catch (Exception ex) { Toast("创建失败：" + ex.Message); }
        }

        void RemoveItem(Item it)
        {
            if (!ConfirmForm.Ask(this, "删除", "把「" + it.Name + "」移到回收站？\n移到回收站后可以从回收站还原。", "删除", "取消"))
                return;
            try
            {
                if (Recycle(it.Path))
                {
                    RefreshItems();
                    Toast("已移到回收站");
                }
                else Toast("删除失败");
            }
            catch (Exception ex) { Toast("删除失败：" + ex.Message); }
        }

        void RenameItem(Item it)
        {
            string initial = it.IsDir ? it.Name : Path.GetFileNameWithoutExtension(it.Name);
            string suffix = it.IsDir ? "" : "扩展名保持不变：" + Path.GetExtension(it.Name);
            using (RenameForm d = new RenameForm(initial, it.Name, suffix))
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                string nn = d.Value.Trim();
                if (nn.Length == 0) return;
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    if (nn.IndexOf(c) >= 0)
                    {
                        Toast("名称里有非法字符");
                        return;
                    }
                }
                string target = Path.Combine(folder, nn + (it.IsDir ? "" : Path.GetExtension(it.Name)));
                if (File.Exists(target) || Directory.Exists(target))
                {
                    Toast("同名项目已存在");
                    return;
                }
                try
                {
                    if (it.IsDir) Directory.Move(it.Path, target);
                    else File.Move(it.Path, target);
                    lastSelPath = target;
                    RefreshItems();
                    Toast("已重命名");
                }
                catch (Exception ex) { Toast("重命名失败：" + ex.Message); }
            }
        }

        // ---------------- 菜单 ----------------
        ContextMenuStrip MakeMenu()
        {
            ContextMenuStrip m = new ContextMenuStrip();
            m.Renderer = new MenuRenderer();
            m.BackColor = Ui.BG;
            m.ForeColor = Ui.TXT;
            m.ShowImageMargin = false;
            m.Font = Ui.FUI;
            m.Padding = new Padding(Ui.S(6), Ui.S(6), Ui.S(6), Ui.S(6));
            m.ShowItemToolTips = false;
            m.Opened += delegate { RoundMenu(m); };
            m.SizeChanged += delegate { RoundMenu(m); };
            return m;
        }

        void RoundMenu(ContextMenuStrip m)
        {
            if (m.Width <= 0 || m.Height <= 0) return;
            using (GraphicsPath p = Ui.RoundPath(new RectangleF(0, 0, m.Width, m.Height), Ui.S(10)))
            {
                Region old = m.Region;
                m.Region = new Region(p);
                if (old != null) old.Dispose();
            }
        }

        ToolStripMenuItem MI(string text, EventHandler handler)
        {
            ToolStripMenuItem m = new ToolStripMenuItem(text, null, handler);
            m.ForeColor = Ui.TXT;
            m.Padding = new Padding(0, Ui.S(4), 0, Ui.S(4));
            return m;
        }

        void ShowMainMenu(Point p)
        {
            ContextMenuStrip m = MakeMenu();
            m.Items.Add(MI("设置…", delegate { OpenSettings(); }));
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(MI("打开文件夹", delegate { OpenPath(folder); }));
            m.Items.Add(MI("切换文件夹…", delegate { ChooseFolder(); }));
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(MI("添加文件…", delegate { AddFiles(); }));
            m.Items.Add(MI("新建文件夹", delegate { NewItem(true); }));
            m.Items.Add(MI("新建文本文件", delegate { NewItem(false); }));
            m.Items.Add(new ToolStripSeparator());

            m.Items.Add(MI("收起成小图标", delegate { SetCollapsed(true, true); }));
            m.Items.Add(MI("退出悬浮文件夹", delegate { Close(); }));
            m.Show(p);
        }

        void ShowRowMenu(Item it)
        {
            if (it == null) return;
            // 直接弹资源管理器同款原生菜单：打开 / 打开方式 / 发送到 / 剪切 / 复制 / 创建快捷方式 / 删除 / 重命名 / 属性…
            if (ShellMenu.Show(this.Handle, it.Path, Cursor.Position))
            {
                RefreshItems();
                return;
            }

            // 兜底：个别环境下系统菜单取不到时，仍给出基本操作
            ContextMenuStrip m = MakeMenu();
            m.Items.Add(MI("打开", delegate { OpenPath(it.Path); }));
            m.Items.Add(MI("在资源管理器中显示", delegate { Reveal(it.Path); }));
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(MI("复制路径", delegate { CopyPath(it.Path); }));
            m.Items.Add(MI("重命名…", delegate { RenameItem(it); }));
            m.Items.Add(MI("删除（移到回收站）", delegate { RemoveItem(it); }));
            m.Show(Cursor.Position);
        }

        void SetSort(string mode)
        {
            sortMode = mode;
            RefreshItems();
            SaveCfg();
        }

        void SetView(string mode)
        {
            if (viewMode == mode) return;
            viewMode = mode;
            iconScroll = 0;
            RefreshItems();
            SaveCfg();
        }

        void ToggleTop()
        {
            topmost = !topmost;
            TopMost = topmost;
            SaveCfg();
            Toast(topmost ? "已开启置顶" : "已取消置顶，会被其他窗口遮挡");
        }

        void ToggleAutostart()
        {
            try
            {
                if (ShellUtil.AutostartEnabled)
                {
                    ShellUtil.SetAutostart(false);
                    Toast("已关闭开机自启动");
                }
                else
                {
                    ShellUtil.SetAutostart(true);
                    Toast("已开启开机自启动");
                }
            }
            catch (Exception ex) { Toast("设置失败：" + ex.Message); }
        }

        // ---------------- 拖动 / 收起 ----------------
        void HeaderDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            dragging = true;
            moved = false;
            dragMouse = Cursor.Position;
            dragForm = Location;
        }

        void HeaderMove(object sender, MouseEventArgs e)
        {
            if (!dragging) return;
            Point p = Cursor.Position;
            if (Math.Abs(p.X - dragMouse.X) > 3 || Math.Abs(p.Y - dragMouse.Y) > 3) moved = true;
            Location = new Point(dragForm.X + p.X - dragMouse.X, dragForm.Y + p.Y - dragMouse.Y);
        }

        void HeaderUp(object sender, MouseEventArgs e)
        {
            if (!dragging) return;
            dragging = false;
            if (!moved)
            {
                if (collapsed) SetCollapsed(false, true);
                return;
            }
            SnapToEdge();
            SaveCfg();
            CaptureBehind();
            RepaintAll();
        }

        void SnapToEdge()
        {
            Rectangle wa = Screen.FromPoint(Location).WorkingArea;
            int snap = Ui.S(14);
            int x = Location.X, y = Location.Y;
            if (Math.Abs(x - wa.Left) < snap) x = wa.Left;
            if (Math.Abs(y - wa.Top) < snap) y = wa.Top;
            if (Math.Abs(x + Width - wa.Right) < snap) x = wa.Right - Width;
            if (Math.Abs(y + Height - wa.Bottom) < snap) y = wa.Bottom - Height;
            Location = new Point(x, y);
        }

        void ClampToScreen()
        {
            Rectangle wa = Screen.FromPoint(Location).WorkingArea;
            int x = Location.X, y = Location.Y;
            if (x + Ui.S(60) > wa.Right) x = wa.Right - Width;
            if (y + Ui.S(60) > wa.Bottom) y = wa.Bottom - Height;
            if (x < wa.Left - Width + Ui.S(60)) x = wa.Left;
            if (y < wa.Top) y = wa.Top;
            Location = new Point(x, y);
        }

        void SetCollapsed(bool v, bool save)
        {
            if (collapsed == v) return;
            if (v)
            {
                expandedW = Width;
                expandedH = Height;
                MinimumSize = new Size(1, 1);
                Size = new Size(Ui.S(84), Ui.S(84));
            }
            else
            {
                Size = new Size(expandedW, expandedH);
                MinimumSize = new Size(Ui.S(250), Ui.S(210));
                iconScroll = 0;
            }
            collapsed = v;
            UpdateRegion();
            ApplyLayout();
            RefreshItems();
            RepaintAll();
            if (save) SaveCfg();
        }

        // 背景取自壁纸：位置/尺寸一变，父窗口与每个子控件的背景切片都必须重画。
        // 只调 Invalidate(true) 时，子控件之间那 1px 的“缝”会留着上一次的像素，
        // 半透明下看起来就是一条亮蓝色的细线，所以这里再强制 Update 一遍。
        public void RepaintAll()
        {
            if (!IsHandleCreated) return;
            Invalidate(true);
            foreach (Control c in Controls)
            {
                if (c == null || c.IsDisposed) continue;
                c.Invalidate();
            }
            Update();
        }

        // ---------------- 监听 ----------------
        void StartWatcher()
        {
            try
            {
                if (watcher != null)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                    watcher = null;
                }
                watcher = new FileSystemWatcher(folder);
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
                    | NotifyFilters.LastWrite | NotifyFilters.Size;
                watcher.IncludeSubdirectories = false;
                watcher.Created += FsEvent;
                watcher.Deleted += FsEvent;
                watcher.Changed += FsEvent;
                watcher.Renamed += FsEvent;
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception) { }
        }

        void FsEvent(object sender, FileSystemEventArgs e)
        {
            dirty = true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveCfg();
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            base.OnFormClosing(e);
        }

        // ---------------- 回收站 ----------------
        [StructLayout(LayoutKind.Sequential)]
        struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            public IntPtr pFrom;
            public IntPtr pTo;
            public ushort fFlags;
            [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public IntPtr lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHFileOperation(ref SHFILEOPSTRUCT op);

        static bool Recycle(string path)
        {
            SHFILEOPSTRUCT op = new SHFILEOPSTRUCT();
            op.wFunc = 3;
            op.fFlags = (ushort)(0x40 | 0x10 | 0x400 | 0x4);
            op.pFrom = Marshal.StringToHGlobalUni(path + "\0");
            op.pTo = IntPtr.Zero;
            int r = SHFileOperation(ref op);
            Marshal.FreeHGlobal(op.pFrom);
            return r == 0;
        }
    }

    // ======================= 白色风格设置窗口 =======================
    // 设置面板 / 菜单 / 卡片共用 Ui 这一套配色，换背景色时一起变
    static class W
    {
        public static Color BG { get { return Ui.BG; } }
        public static Color CARD { get { return Ui.CARD; } }
        public static Color TXT { get { return Ui.TXT; } }
        public static Color DIM { get { return Ui.DIM; } }
        public static Color LINE { get { return Ui.LINE_C; } }
        public static Color SOFT { get { return Ui.BTN_BG; } }
        public static Color ACC { get { return Ui.ACC; } }

        public static GraphicsPath Round(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d <= 0) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    sealed class SwatchBox : Control
    {
        public Color Fill;
        public bool Chosen;

        public SwatchBox(Color c)
        {
            Fill = c;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = W.Round(r, Ui.S(9)))
            {
                using (SolidBrush b = new SolidBrush(Fill)) g.FillPath(b, p);
                Color ring = Chosen ? W.ACC : Ui.BORDER_C;
                using (Pen pen = new Pen(ring, Chosen ? 2f : 1f)) g.DrawPath(pen, p);
            }
            if (Chosen)
            {
                Color markColor = Ui.Lum(Fill) > 0.62 ? Color.FromArgb(70, 74, 82) : Color.White;
                TextRenderer.DrawText(g, "✓", Ui.FSmallBold, new Rectangle(0, 0, Width, Height), markColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }

    sealed class BgThumb : Control
    {
        public int Img;   // 0=纯色，1..4=内置背景图
        public bool Chosen;

        public BgThumb(int img)
        {
            Img = img;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = W.Round(r, Ui.S(9)))
            {
                Bitmap bmp = Img > 0 ? Ui.GetBakedBg(Img, Ui.OpLevel) : null;
                if (bmp != null)
                {
                    g.SetClip(p);
                    g.DrawImage(bmp, 0, 0, Width, Height);
                    g.ResetClip();
                }
                else
                {
                    Color solid = Ui.Mix(Ui.BG, Color.White, Ui.Veil(Ui.OpLevel));
                    using (SolidBrush b = new SolidBrush(solid)) g.FillPath(b, p);
                }
                Color ring = Chosen ? W.ACC : Ui.BORDER_C;
                using (Pen pen = new Pen(ring, Chosen ? 2f : 1f)) g.DrawPath(pen, p);
            }
            if (Img == 0)
            {
                TextRenderer.DrawText(g, "纯色", Ui.FSmall, r, Ui.DIM,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            else if (Chosen)
            {
                TextRenderer.DrawText(g, "✓", Ui.FSmallBold, r, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }

    sealed class OpacitySlider : Control
    {
        public float Value = 1f;
        public event EventHandler Changed;
        bool dragging;

        public OpacitySlider()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
        }

        void SetFromX(int x)
        {
            float t = (float)(x - Ui.S(8)) / Math.Max(1, Width - Ui.S(16));
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            float v = 0.55f + t * 0.45f;
            if (Math.Abs(v - Value) < 0.005f) return;
            Value = v;
            Invalidate();
            if (Changed != null) Changed(this, EventArgs.Empty);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { dragging = true; SetFromX(e.X); }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging) SetFromX(e.X);
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Glass.Fill(this, g);
            int cy = Height / 2;
            Rectangle track = new Rectangle(Ui.S(8), cy - Ui.S(3), Width - Ui.S(16), Ui.S(6));
            using (GraphicsPath p = W.Round(track, Ui.S(3)))
            using (SolidBrush b = new SolidBrush(W.SOFT)) g.FillPath(b, p);
            float t = (Value - 0.55f) / 0.45f;
            int fillW = (int)Math.Round(track.Width * t);
            if (fillW > 2)
            {
                Rectangle fr = new Rectangle(track.X, track.Y, fillW, track.Height);
                using (GraphicsPath p = W.Round(fr, Ui.S(3)))
                using (SolidBrush b = new SolidBrush(W.ACC)) g.FillPath(b, p);
            }
            int kx = track.X + fillW;
            int kd = Ui.S(16);
            using (SolidBrush b = new SolidBrush(Color.White)) g.FillEllipse(b, kx - kd / 2, cy - kd / 2, kd, kd);
            using (Pen pen = new Pen(W.ACC, 2f)) g.DrawEllipse(pen, kx - kd / 2, cy - kd / 2, kd, kd);
        }
    }

    sealed class ChipButton : Control
    {
        public bool Chosen;
        public bool Primary;

        public ChipButton(string text)
        {
            Text = text;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = W.Round(r, Ui.S(8)))
            {
                Color fill = Primary ? W.ACC : (Chosen ? Color.FromArgb(96, W.CARD) : Color.FromArgb(56, W.SOFT));
                using (SolidBrush b = new SolidBrush(fill)) g.FillPath(b, p);
                if (Chosen || Primary)
                {
                    using (Pen pen = new Pen(Chosen ? W.ACC : Ui.ACC_D, 1f)) g.DrawPath(pen, p);
                }
            }
            TextRenderer.DrawText(g, Text, Font, r, Primary ? Ui.ON_ACC : (Chosen ? W.TXT : W.DIM),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    sealed class ToggleSwitch : Control
    {
        public bool On;
        public event EventHandler Changed;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnClick(EventArgs e)
        {
            On = !On;
            Invalidate();
            if (Changed != null) Changed(this, EventArgs.Empty);
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int h = Math.Min(Height, Ui.S(24));
            Rectangle r = new Rectangle(0, (Height - h) / 2, Width, h);
            using (GraphicsPath p = W.Round(r, h / 2))
            {
                using (SolidBrush b = new SolidBrush(On ? W.ACC : Color.FromArgb(70, W.SOFT))) g.FillPath(b, p);
            }
            int d = h - Ui.S(6);
            int kx = On ? Width - d - Ui.S(3) : Ui.S(3);
            using (SolidBrush b = new SolidBrush(Color.White)) g.FillEllipse(b, kx, r.Y + Ui.S(3), d, d);
        }
    }

    sealed class Segmented : Control
    {
        public string[] Items = new string[0];
        public int Index;
        public event EventHandler Changed;

        public Segmented()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        int Hit(MouseEventArgs e)
        {
            if (Items.Length == 0) return -1;
            int w = Width / Items.Length;
            int i = e.X / Math.Max(1, w);
            if (i < 0) i = 0;
            if (i >= Items.Length) i = Items.Length - 1;
            return i;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int i = Hit(e);
            if (i >= 0 && i != Index)
            {
                Index = i;
                Invalidate();
                if (Changed != null) Changed(this, EventArgs.Empty);
            }
            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle all = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = W.Round(all, Ui.S(9)))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(56, W.SOFT))) g.FillPath(b, p);
            if (Items.Length == 0) return;
            int w = Width / Items.Length;
            for (int i = 0; i < Items.Length; i++)
            {
                Rectangle seg = new Rectangle(i * w + Ui.S(3), Ui.S(3), w - Ui.S(6), Height - Ui.S(6));
                if (i == Index)
                {
                    using (GraphicsPath p = W.Round(seg, Ui.S(7)))
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(96, W.CARD))) g.FillPath(b, p);
                }
                TextRenderer.DrawText(g, Items[i], Font, seg, i == Index ? W.TXT : W.DIM,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }

    public class SettingsForm : Form
    {
        Widget owner;
        SwatchBox[] swatches;
        BgThumb[] bgThumbs;
        ChipButton[] radiusChips;
        Segmented segView, segSort, segSize;
        ToggleSwitch tgAuto, tgTop;
        Color curBg;
        int curRadius;
        int curBgImg;
        int dragging;
        Point dragStart;
        Action readyAction;

        static readonly Color[] Presets = new Color[]
        {
            Color.FromArgb(28, 30, 35),    // 石墨
            Color.FromArgb(15, 17, 19),    // 曜黑
            Color.FromArgb(27, 39, 56),    // 午夜蓝
            Color.FromArgb(20, 33, 28),    // 松绿
            Color.FromArgb(42, 27, 34),    // 酒红
            Color.FromArgb(50, 38, 25),    // 咖褐
            Color.FromArgb(242, 243, 245), // 雾白
            Color.FromArgb(250, 246, 236), // 米白
            Color.FromArgb(231, 239, 248), // 淡蓝
            Color.FromArgb(232, 241, 233), // 淡绿
            Color.FromArgb(247, 236, 239), // 淡粉
            Color.FromArgb(110, 123, 138)  // 蓝灰
        };

        public SettingsForm(Widget w)
        {
            owner = w;
            Text = "设置";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            BackColor = W.BG;
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(Ui.S(408), Ui.S(708));
            curBg = w.ThemeBg;
            curRadius = w.ThemeRadius;
            curBgImg = w.BgImgState;
            SetRound();

            int pad = Ui.S(24);

            Label title = Lab("设置", Ui.FBold, W.TXT, pad, Ui.S(22), Ui.S(200), Ui.S(28));
            title.Font = new Font("Microsoft YaHei UI", 13f, FontStyle.Bold);
            Controls.Add(title);
            Drag(title);

            Label close = Lab("✕", Ui.FGlyph,  W.DIM, Ui.S(408) - Ui.S(52), Ui.S(20), Ui.S(28), Ui.S(28));
            close.TextAlign = ContentAlignment.MiddleCenter;
            close.Cursor = Cursors.Hand;
            close.MouseEnter += delegate { close.ForeColor = W.TXT; };
            close.MouseLeave += delegate { close.ForeColor = W.DIM; };
            close.Click += delegate { Close(); };
            Controls.Add(close);

            Label s1 = Section("外观设置", pad, Ui.S(66));
            Controls.Add(s1);

            Label imgLab = Lab("背景图片", Ui.FUI, W.TXT, pad, Ui.S(92), Ui.S(160), Ui.S(20));
            Controls.Add(imgLab);

            bgThumbs = new BgThumb[5];
            int tbW = Ui.S(60), tbGap = Ui.S(15);
            int tbGridW = 5 * tbW + 4 * tbGap;
            int tbx0 = (Ui.S(408) - tbGridW) / 2;
            for (int i = 0; i < 5; i++)
            {
                BgThumb tb = new BgThumb(i);
                tb.Size = new Size(tbW, Ui.S(38));
                tb.Location = new Point(tbx0 + i * (tbW + tbGap), Ui.S(112));
                tb.Chosen = (i == curBgImg);
                int idx = i;
                tb.Click += delegate
                {
                    curBgImg = idx;
                    owner.SetBgImg(idx);
                    for (int j = 0; j < bgThumbs.Length; j++) { bgThumbs[j].Chosen = (j == idx); bgThumbs[j].Invalidate(); }
                };
                bgThumbs[i] = tb;
                Controls.Add(tb);
            }

            Label bgLab = Lab("背景颜色", Ui.FUI, W.TXT, pad, Ui.S(166), Ui.S(160), Ui.S(20));
            Controls.Add(bgLab);

            swatches = new SwatchBox[Presets.Length];
            int sw = Ui.S(34), gap = Ui.S(10), perRow = 6;
            int gridW = perRow * sw + (perRow - 1) * gap;
            int gx0 = (Ui.S(408) - gridW) / 2;
            for (int i = 0; i < Presets.Length; i++)
            {
                SwatchBox sb = new SwatchBox(Presets[i]);
                sb.Size = new Size(sw, sw);
                sb.Location = new Point(gx0 + (i % perRow) * (sw + gap), Ui.S(186) + (i / perRow) * (sw + gap));
                sb.Chosen = (Presets[i].ToArgb() == curBg.ToArgb());
                sb.Click += delegate(object s, EventArgs e)
                {
                    SwatchBox c = (SwatchBox)s;
                    Pick(c.Fill, true);
                };
                swatches[i] = sb;
                Controls.Add(sb);
            }

            ChipButton custom = new ChipButton("自定义颜色…");
            custom.Font = Ui.FSmall;
            custom.Bounds = new Rectangle(gx0, Ui.S(262), Ui.S(108), Ui.S(30));
            custom.Click += delegate
            {
                using (ColorDialog d = new ColorDialog())
                {
                    d.FullOpen = true;
                    d.Color = curBg;
                    if (d.ShowDialog(this) == DialogResult.OK) Pick(d.Color, false);
                }
            };
            Controls.Add(custom);

            Label radLab = Lab("圆角", Ui.FUI, W.TXT, pad, Ui.S(318), Ui.S(120), Ui.S(20));
            Controls.Add(radLab);

            radiusChips = new ChipButton[3];
            int[] vals = new int[] { 6, 14, 22 };
            string[] names = new string[] { "小", "中", "大" };
            for (int i = 0; i < 3; i++)
            {
                ChipButton c = new ChipButton(names[i]);
                c.Font = Ui.FUI;
                c.Bounds = new Rectangle(pad + i * Ui.S(80), Ui.S(342), Ui.S(72), Ui.S(30));
                c.Chosen = NearestRadius(vals, curRadius) == i;
                int idx = i;
                c.Click += delegate { curRadius = vals[idx]; RecheckRadius(); owner.SetAppearance(curBg, 1f, curRadius, curBgImg, 0); };
                radiusChips[i] = c;
                Controls.Add(c);
            }

            AddDivider(Ui.S(384));

            Label s2 = Section("显示", pad, Ui.S(398));
            Controls.Add(s2);

            Label vLab = Lab("显示方式", Ui.FUI, W.TXT, pad, Ui.S(426), Ui.S(150), Ui.S(24));
            Controls.Add(vLab);
            segView = new Segmented();
            segView.Items = new string[] { "列表", "小图标" };
            segView.Index = owner.ViewState == "icons" ? 1 : 0;
            segView.Font = Ui.FUI;
            segView.Bounds = new Rectangle(Ui.S(408) - Ui.S(24) - Ui.S(168), Ui.S(422), Ui.S(168), Ui.S(32));
            segView.Changed += delegate { owner.SetViewFromSettings(segView.Index == 1 ? "icons" : "list"); };
            Controls.Add(segView);

            Label szLab = Lab("图标大小（小图标视图）", Ui.FUI, W.TXT, pad, Ui.S(466), Ui.S(180), Ui.S(24));
            Controls.Add(szLab);
            segSize = new Segmented();
            segSize.Items = new string[] { "小", "中", "大" };
            segSize.Index = owner.IconSizeState == "s" ? 0 : (owner.IconSizeState == "l" ? 2 : 1);
            segSize.Font = Ui.FUI;
            segSize.Bounds = new Rectangle(Ui.S(408) - Ui.S(24) - Ui.S(168), Ui.S(462), Ui.S(168), Ui.S(32));
            segSize.Changed += delegate
            {
                string s = segSize.Index == 0 ? "s" : (segSize.Index == 2 ? "l" : "m");
                owner.SetIconSizeFromSettings(s);
            };
            Controls.Add(segSize);

            Label skLab = Lab("排序", Ui.FUI, W.TXT, pad, Ui.S(506), Ui.S(120), Ui.S(24));
            Controls.Add(skLab);
            segSort = new Segmented();
            segSort.Items = new string[] { "名称", "时间", "大小" };
            segSort.Index = owner.SortState == "time" ? 1 : (owner.SortState == "size" ? 2 : 0);
            segSort.Font = Ui.FUI;
            segSort.Bounds = new Rectangle(Ui.S(408) - Ui.S(24) - Ui.S(228), Ui.S(502), Ui.S(228), Ui.S(32));
            segSort.Changed += delegate
            {
                string m = segSort.Index == 1 ? "time" : (segSort.Index == 2 ? "size" : "name");
                owner.SetSortFromSettings(m);
            };
            Controls.Add(segSort);

            AddDivider(Ui.S(544));

            Label s3 = Section("行为", pad, Ui.S(558));
            Controls.Add(s3);

            Label aLab = Lab("开机自启动", Ui.FUI, W.TXT, pad, Ui.S(588), Ui.S(180), Ui.S(24));
            Controls.Add(aLab);
            tgAuto = new ToggleSwitch();
            tgAuto.On = owner.AutoStartOn;
            tgAuto.Bounds = new Rectangle(Ui.S(408) - Ui.S(24) - Ui.S(46), Ui.S(588), Ui.S(46), Ui.S(24));
            tgAuto.Changed += delegate { owner.SetAutoStartFromSettings(tgAuto.On); };
            Controls.Add(tgAuto);

            Label tLab = Lab("窗口置顶（默认关闭，可被其他窗口遮挡）", Ui.FUI, W.TXT, pad, Ui.S(626), Ui.S(280), Ui.S(24));
            Controls.Add(tLab);
            tgTop = new ToggleSwitch();
            tgTop.On = owner.TopMostOn;
            tgTop.Bounds = new Rectangle(Ui.S(408) - Ui.S(24) - Ui.S(46), Ui.S(626), Ui.S(46), Ui.S(24));
            tgTop.Changed += delegate { owner.SetTopMostFromSettings(tgTop.On); };
            Controls.Add(tgTop);

            ChipButton def = new ChipButton("恢复默认");
            def.Font = Ui.FSmall;
            def.Bounds = new Rectangle(pad, Ui.S(664), Ui.S(104), Ui.S(34));
            def.Click += delegate { ResetAll(); };
            Controls.Add(def);

            ChipButton done = new ChipButton("完成");
            done.Primary = true;
            done.Font = Ui.FBold;
            done.Bounds = new Rectangle(Ui.S(408) - Ui.S(24) - Ui.S(96), Ui.S(664), Ui.S(96), Ui.S(34));
            done.Click += delegate { Close(); };
            Controls.Add(done);

            KeyPreview = true;
            KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) Close(); };
            MouseDown += delegate(object s, MouseEventArgs e) { dragging = 1; dragStart = Cursor.Position; };
            MouseMove += delegate(object s, MouseEventArgs e)
            {
                if (dragging == 1) { Point p = Cursor.Position; Location = new Point(Left + p.X - dragStart.X, Top + p.Y - dragStart.Y); dragStart = p; }
            };
            MouseUp += delegate { dragging = 0; CaptureBehind(); Invalidate(true); };

            PositionNear(owner);
        }

        void PositionNear(Form o)
        {
            Rectangle wa = Screen.FromControl(o).WorkingArea;
            int x = o.Right + Ui.S(12);
            if (x + Width > wa.Right) x = o.Left - Width - Ui.S(12);
            if (x < wa.Left) x = wa.Left + Ui.S(20);
            int y = o.Top;
            if (y + Height > wa.Bottom) y = wa.Bottom - Height - Ui.S(8);
            if (y < wa.Top) y = wa.Top + Ui.S(20);
            Location = new Point(x, y);
        }

        int NearestRadius(int[] vals, int cur)
        {
            int best = 0;
            int diff = int.MaxValue;
            for (int i = 0; i < vals.Length; i++)
            {
                int d = Math.Abs(vals[i] - cur);
                if (d < diff) { diff = d; best = i; }
            }
            return best;
        }

        void RecheckRadius()
        {
            int[] vals = new int[] { 6, 14, 22 };
            int near = NearestRadius(vals, curRadius);
            for (int i = 0; i < radiusChips.Length; i++) { radiusChips[i].Chosen = (i == near); radiusChips[i].Invalidate(); }
        }

        void Pick(Color c, bool fromPreset)
        {
            curBg = c;
            curBgImg = 0;
            for (int i = 0; i < swatches.Length; i++)
            {
                swatches[i].Chosen = fromPreset && swatches[i].Fill.ToArgb() == c.ToArgb();
                swatches[i].Invalidate();
            }
            for (int i = 0; i < bgThumbs.Length; i++)
            {
                bgThumbs[i].Chosen = (i == 0);
                bgThumbs[i].Invalidate();
            }
            owner.SetAppearance(curBg, 1f, curRadius, 0, 0);
        }

        void ResetAll()
        {
            curBg = Presets[0];
            curRadius = 14;
            curBgImg = 0;
            for (int i = 0; i < swatches.Length; i++) { swatches[i].Chosen = (i == 0); swatches[i].Invalidate(); }
            for (int i = 0; i < bgThumbs.Length; i++) { bgThumbs[i].Chosen = (i == 0); bgThumbs[i].Invalidate(); }
            RecheckRadius();
            owner.SetAppearance(curBg, 1f, curRadius, 0, 0);
        }

        Label Lab(string text, Font f, Color c, int x, int y, int w, int h)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = f;
            l.ForeColor = c;
            l.BackColor = Color.Transparent;
            l.Tag = (c.ToArgb() == W.DIM.ToArgb()) ? "D" : "T";
            l.AutoSize = false;
            l.Bounds = new Rectangle(x, y, w, h);
            l.TextAlign = ContentAlignment.MiddleLeft;
            return l;
        }

        Label Section(string text, int x, int y)
        {
            Label l = Lab(text, Ui.FSmall, W.DIM, x, y, Ui.S(200), Ui.S(18));
            return l;
        }

        void AddDivider(int y)
        {
            Panel p = new Panel();
            p.BackColor = W.LINE;
            p.Bounds = new Rectangle(Ui.S(24), y, Ui.S(408) - Ui.S(48), 1);
            Controls.Add(p);
        }

        void Drag(Control c)
        {
            c.MouseDown += delegate(object s, MouseEventArgs e) { dragging = 1; dragStart = Cursor.Position; };
            c.MouseMove += delegate(object s, MouseEventArgs e)
            {
                if (dragging == 1) { Point p = Cursor.Position; Location = new Point(Left + p.X - dragStart.X, Top + p.Y - dragStart.Y); dragStart = p; }
            };
            c.MouseUp += delegate { dragging = 0; CaptureBehind(); Invalidate(true); };
        }

        void SetRound()
        {
            // 用圆角 Region 把四角裁掉，圆角外不再露出方形底色
            Region old = Region;
            using (GraphicsPath path = Ui.RoundPath(new RectangleF(0, 0, Width, Height), Ui.S(Math.Max(10, Ui.Radius))))
                Region = new Region(path);
            if (old != null) old.Dispose();
        }

        // 主界面换色 / 透明度 / 圆角时，设置面板同步跟着变
        public void RefreshTheme()
        {
            BackColor = W.BG;
            SetRound();
            ThemeChild(this);
            Invalidate(true);
        }

        void ThemeChild(Control parent)
        {
            for (int i = 0; i < parent.Controls.Count; i++)
            {
                Control c = parent.Controls[i];
                Label l = c as Label;
                if (l != null)
                {
                    l.BackColor = Color.Transparent;
                    l.ForeColor = (l.Tag != null && Convert.ToString(l.Tag) == "D") ? W.DIM : W.TXT;
                    l.Invalidate();
                }
                else
                {
                    c.Invalidate();
                }
                if (c.HasChildren) ThemeChild(c);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Glass.FillRaw(this, g);
            int rad = Ui.S(Math.Max(10, Ui.Radius));
            using (GraphicsPath p = W.Round(new Rectangle(0, 0, Width, Height), rad))
            {
                Bitmap bmp = Ui.GetBakedBg(Ui.BgImg, Ui.OpLevel);
                if (bmp != null)
                {
                    g.SetClip(p);
                    g.DrawImage(bmp, 0, 0, Width, Height);
                    g.ResetClip();
                }
                else
                {
                    Color solid = Ui.Mix(Ui.BG, Color.White, Ui.Veil(Ui.OpLevel));
                    using (SolidBrush b = new SolidBrush(solid)) g.FillPath(b, p);
                }
            }
            using (GraphicsPath p = W.Round(new Rectangle(1, 1, Width - 2, Height - 2), Math.Max(1, rad - 1)))
            {
                using (Pen pen = new Pen(Color.FromArgb(70, Ui.TXT), 1)) g.DrawPath(pen, p);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            owner.SaveNow();
            base.OnFormClosed(e);
            try { Behind.DetachReady(readyAction); Behind.Release(Handle); } catch { }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            readyAction = delegate
            {
                try { if (IsHandleCreated && !IsDisposed && dragging == 0) BeginInvoke((Action)delegate { Invalidate(true); }); } catch { }
            };
            Behind.AttachReady(readyAction);
            CaptureBehind();
        }

        void CaptureBehind()
        {
            // 不再抓取身后窗口：背景由内置图片/纯色提供
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            if (!IsHandleCreated) return;
            if (dragging != 0) return;   // 拖动中不重抓背景，松手后由 MouseUp 补一轮
            CaptureBehind();
            Invalidate(true);
        }
    }
}
