using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using CV = OpenCvSharp.CPlusPlus;
using CVEx = OpenCvSharp.Extensions;

namespace PhotoStudioPlayerBackSubtractor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            hwnd = seekwindow();
            if (hwnd == IntPtr.Zero)
            {
                MessageBox.Show("Vysor を先に起動してください");
                Application.Exit();
            }

            this.TransparencyKey = Color.Blue;
        }
        IntPtr hwnd;

        private IntPtr seekwindow()
        {

            IntPtr h = FindWindow("Chrome_WidgetWin_1", null);
            if (h != IntPtr.Zero)
            {
                IntPtr hh = FindWindowEx(h, IntPtr.Zero, "Intermediate D3D Window", null);
                return hh;
            }
            return IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }


        /// 定期的にキャプチャする
        private Bitmap catpture(IntPtr hWnd)
        {
            RECT winRect = new RECT();
            GetWindowRect(hWnd, ref winRect);
            //Bitmapの作成
            Bitmap bmp = new Bitmap(winRect.right - winRect.left,
                winRect.bottom - winRect.top);
            //Graphicsの作成
            Graphics g = Graphics.FromImage(bmp);
            //画面全体からコピーする
            g.CopyFromScreen(new Point(winRect.left, winRect.top), new Point(0, 0), bmp.Size);
            //解放
            g.Dispose();
            return bmp;
        }

        Bitmap blue;
        CV.Mat _back;
        CV.Mat _mask = new CV.Mat();
        CV.Mat _dest = new CV.Mat();
        CV.BackgroundSubtractorMOG _bs = new CV.BackgroundSubtractorMOG();

        private Bitmap filter(Bitmap bmp)
        {
            // フォーマット変更
            var bmp2 = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);
            var g2 = Graphics.FromImage(bmp2);
            g2.DrawImage(bmp, new Point());
            bmp.Dispose();
            g2.Dispose();
            bmp = bmp2;


            // 初回だけ青色の背景を作る
            if ( blue == null )
            {
                blue = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);
                var g = Graphics.FromImage(blue);
                g.FillRectangle(new SolidBrush(Color.Blue), 0, 0, blue.Width, blue.Height);
                _back = CVEx.BitmapConverter.ToMat(blue);
                bmp = blue;
            }
            var frame = CVEx.BitmapConverter.ToMat(bmp);
            var dest = fillterBackMask(frame);
            CVEx.BitmapConverter.ToBitmap(dest, bmp);

            return bmp;
        }
        CV.Mat fillterBackMask(CV.Mat src)
        {
            _back.CopyTo(_dest);
            _bs.Run(src, _mask);
            src.CopyTo(_dest, _mask);
            return _dest;
        }


        private void cap()
        {
            var bmp = catpture(hwnd);
            bmp = filter(bmp);
            var old = this.BackgroundImage;
            this.BackgroundImage = bmp;
            if ( old != null )
            {
                old.Dispose();
            }
        }

        private void Form1_Click(object sender, EventArgs e)
        {
            if (tm == null)
            {
                tm = new Timer();
                tm.Interval = 20;
                tm.Tick += (_, __) => { cap(); };
                tm.Enabled = true;
                this.FormBorderStyle = FormBorderStyle.None;
            }
            else
            {
                tm.Enabled = false;
                tm = null;
                this.FormBorderStyle = FormBorderStyle.Sizable;
            }
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr FindWindow(
            string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr FindWindowEx(
            IntPtr hwndParent, IntPtr hwndChildAfter,
            string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern int GetWindowRect(IntPtr hwnd,
            ref RECT lpRect);
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hwnd);
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")]
        private static extern IntPtr GetDCEx(IntPtr hwnd, IntPtr hreg, int flags);

        [DllImport("gdi32.dll")]
        private static extern int BitBlt(IntPtr hDestDC,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hSrcDC,
            int xSrc,
            int ySrc,
            int dwRop);
        private const int SRCCOPY = 13369376;
        private const int CAPTUREBLT = 1073741824;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;


        [DllImport("user32.dll")]
        private static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);

        Timer tm = null;
    }
}
