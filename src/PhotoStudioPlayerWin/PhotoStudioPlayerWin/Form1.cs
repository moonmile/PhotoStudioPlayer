using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace PhotoStudioPlayerWin
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
            if ( hwnd == IntPtr.Zero )
            {
                MessageBox.Show("「接続」を先に起動してください");
                Application.Exit();
            }
        }
        IntPtr hwnd;

        private IntPtr seekwindow()
        {

            IntPtr h = FindWindow("Chrome_WidgetWin_1", null);
            if ( h != IntPtr.Zero )
            {
                IntPtr hh = FindWindowEx(h, IntPtr.Zero, "Intermediate D3D Window", null);
                return hh;
            }
            return IntPtr.Zero;
        }

        /// 定期的にキャプチャする
        private Bitmap catpture(IntPtr hWnd)
        {
#if false
            IntPtr winDC = GetWindowDC(hWnd);
            //ウィンドウの大きさを取得
            RECT winRect = new RECT();
            GetWindowRect(hWnd, ref winRect);
            //Bitmapの作成
            Bitmap bmp = new Bitmap(winRect.right - winRect.left,
                winRect.bottom - winRect.top);
            //Graphicsの作成
            Graphics g = Graphics.FromImage(bmp);
            //Graphicsのデバイスコンテキストを取得
            IntPtr hDC = g.GetHdc();
            //Bitmapに画像をコピーする
            BitBlt(hDC, 0, 0, bmp.Width, bmp.Height,  winDC, 0, 0, SRCCOPY | CAPTUREBLT);
            //解放
            g.ReleaseHdc(hDC);
            g.Dispose();
            ReleaseDC(hWnd, winDC);
#else
            RECT winRect = new RECT();
            GetWindowRect(hWnd, ref winRect);
            //Bitmapの作成
            Bitmap bmp = new Bitmap(winRect.right - winRect.left,
                winRect.bottom - winRect.top);
            //Graphicsの作成
            Graphics g = Graphics.FromImage(bmp);
            //画面全体からコピーする
            g.CopyFromScreen( new Point(winRect.left, winRect.top), new Point(0,0), bmp.Size);
            //解放
            g.Dispose();
#endif
            return bmp;
        }
        /// 背景を抜く
        /// 

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
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
        private static extern IntPtr GetDCEx(IntPtr hwnd, IntPtr hreg, int flags );

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

        const int cubeSize = 64;
        bool[,,] cubeData = null;
        /// <summary>
        /// キューブ型のフィルタを作成
        /// </summary>
        private void makeCubeData()
        {
            if (cubeData != null) return;
            double[] target = { 0.15, 0.48, 1.0 };
            double threshold = 0.2;
            double[] rgb = new double[3];
            double[] hsv = new double[3];
            double size = cubeSize;
            cubeData = new bool[cubeSize , cubeSize , cubeSize];

            int i = 0;
            for (int z = 0; z < size; z++)
            {
                rgb[2] = ((double)z) / (size - 1); // Blue value
                for (int y = 0; y < size; y++)
                {
                    rgb[1] = ((double)y) / (size - 1); // Green value
                    for (int x = 0; x < size; x++)
                    {
                        rgb[0] = ((double)x) / (size - 1); // Red value
                                                           // Convert RGB to HSV
                                                           // You can find publicly available rgbToHSV functions on the Internet
                                                           //                rgbToHSV(rgb, hsv);
                                                           // Use the hue value to determine which to make transparent
                                                           // The minimum and maximum hue angle depends on
                                                           // the color you want to remove
                                                           //                float alpha = (hsv[0] > minHueAngle && hsv[0] < maxHueAngle) ? 0.0f: 1.0f;
                        double distance = Math.Sqrt(Math.Pow(rgb[0] - target[0], 2)
                                              + Math.Pow(rgb[1] - target[1], 2)
                                              + Math.Pow(rgb[2] - target[2], 2));
                        double alpha = distance < threshold ? 0 : 1;
                        // Calculate premultiplied alpha values for the cube
                        // cubeData[i+0] = rgb[0] * alpha;
                        // cubeData[i+1] = rgb[1] * alpha;
                        // cubeData[i+2] = rgb[2] * alpha;
                        // cubeData[i+3] = alpha;
                        cubeData[x, y, z] = alpha == 0.0 ? true : false;
                        i += 4;
                    }
                }
            }
        }

        unsafe private Bitmap filter ( Bitmap bmp )
        {
            BitmapData bd = bmp.LockBits(new Rectangle(new Point(), bmp.Size), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            byte *ptr = (byte*)bd.Scan0.ToPointer();
            // var g = Graphics.FromImage(bmp);
            for ( int y = 0; y < bmp.Height; y++ )
            {
                for ( int x = 0; x < bmp.Width; x ++ )
                {
                    // var col = bmp.GetPixel(x, y);
                    // int r = col.R;
                    // int g = col.G;
                    // int b = col.B;

                    byte *p =  ptr + bd.Stride * y + x * 3;
                    int r = p[2];
                    int g = p[1];
                    int b = p[0];
                    // var i = (r+3)/4 + (g+3)/4 * cubeSize + (b+3)/4 * cubeSize * cubeSize;
                    var alpha = cubeData[r / 4, g / 4, b / 4];
                    if (alpha)
                    {

                        // bmp.SetPixel(x, y, Color.FromArgb(0, 0, 255));
                        p[0] = 0xff;
                        p[1] = 0x00;
                        p[2] = 0x00;
                    }
                }

            }
            bmp.UnlockBits(bd);

            return bmp;
        }

        private void cap()
        {
            var bmp = catpture(hwnd);
            makeCubeData();
            bmp = filter(bmp);
            this.BackgroundImage = bmp;
        }

        private void Form1_Click(object sender, EventArgs e)
        {
            if ( tm == null )
            {
                tm = new Timer();
                tm.Interval =　20;
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
    }
}
