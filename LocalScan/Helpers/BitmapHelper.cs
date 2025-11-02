using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LocalScan.Helpers
{
    public static class BitmapHelper
    {
        public static Bitmap GetWindowCapture(IntPtr hWnd, int _width, int _height, int? cutWidth = null, int? cutHeight = null)
        {
            if (hWnd == IntPtr.Zero)
                return null;
            var img = PrintWindowImproved(hWnd);
            if (img == null)
                return null;
            int x = 0, y = 0, width = img.Width, height = img.Height;
            if (img.Width > _width)
            {
                x = img.Width - _width;
                width = _width;
            }
            if (img.Height > _height)
            {
                y = img.Height - _height;
                height = _height;
            }

            if (cutWidth.HasValue && cutWidth.Value < width)
                width = cutWidth.Value;

            if (cutHeight.HasValue && cutHeight.Value < height)
                height = cutHeight.Value;

            return img.Clone(new Rectangle(new Point(x, y), new Size(width, height)), PixelFormat.Format32bppArgb);
        }

        public static bool GetEnemies(IntPtr hWnd, int _width, int _height, int? cutWidth = null, int? cutHeight = null)
        {
            var bitmap = GetWindowCapture(hWnd, _width, _height, cutWidth, cutHeight);
            bool res = true;

            if (bitmap != null)
            {
                res = false;
                var array = bitmap.GetPixels();

                for (int i = 0; i < array.Length; i++)
                {
                    for (int j = 0; j < array[i].Length; j++)
                    {
                        if (array[i][j].IsOrange() || array[i][j].IsRed())
                            res = true;

                        //  если вокруг серого пикселя много серых пикселей, считаем что это квадрат нейтрала
                        if (array[i][j].IsGrey())
                        {
                            var check = new List<Pixel>();
                            for (int ki = -1; ki < 2; ki++)
                                for (int kj = -1; kj < 2; kj++)
                                    if (i + ki >= 0 && i + ki < array.Length && j + kj >= 0 && j + kj < array[i].Length)
                                        check.Add(array[i + ki][j + kj]);
                            if (check.Count(x => x.IsGrey()) > check.Count / 2)
                                res = true;
                        }
                    }
                }
            }

            return res;
        }

        public static void TestPrint(IntPtr hWnd, int _width, int _height, int? cutWidth = null, int? cutHeight = null)
        {
            var bitmap = GetWindowCapture(hWnd, _width, _height, cutWidth, cutHeight);
            if (bitmap != null)
            {
                var path = $"{DateTime.Now:HH.mm.ss}.png";
                bitmap.Save(path);
                Process.Start(path);
            }
        }

        #region Pixel work

        public struct Pixel : IEquatable<Pixel>
        {
            public byte Blue;
            public byte Green;
            public byte Red;
            public byte Alpha;

            public bool Equals(Pixel other)
            {
                return Red == other.Red && Green == other.Green && Blue == other.Blue && Alpha == other.Alpha;
            }
        }

        private static Pixel[][] GetPixels(this Bitmap bmp)
        {
            return ProcessBitmap(bmp, pixel => pixel);
        }

        private static Color[][] GetColors(this Bitmap bmp)
        {
            return ProcessBitmap(bmp, pixel => Color.FromArgb(pixel.Red, pixel.Green, pixel.Blue));
        }

        private static unsafe T[][] ProcessBitmap<T>(this Bitmap bitmap, Func<Pixel, T> func)
        {
            var lockBits = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                                       bitmap.PixelFormat);
            int padding = lockBits.Stride - (bitmap.Width * sizeof(Pixel));

            int width = bitmap.Width;
            int height = bitmap.Height;

            var result = new T[height][];

            var ptr = (byte*)lockBits.Scan0;

            for (int i = 0; i < height; i++)
            {
                result[i] = new T[width];
                for (int j = 0; j < width; j++)
                {
                    var pixel = (Pixel*)ptr;
                    result[i][j] = func(*pixel);
                    ptr += sizeof(Pixel);
                }
                ptr += padding;
            }

            bitmap.UnlockBits(lockBits);

            return result;
        }

        private static readonly Color Red = Color.FromArgb(157, 17, 21);
        private static readonly Color Orange = Color.FromArgb(199, 76, 9);
        private static readonly Color Grey = Color.FromArgb(149, 149, 149);
        private static bool IsRed(this Pixel pixel) => PixelCompare(pixel, Red) < 50;
        private static bool IsOrange(this Pixel pixel) => PixelCompare(pixel, Orange) < 50;
        private static bool IsGrey(this Pixel pixel) => PixelCompare(pixel, Grey) < 30;

        public static double Orange1(this Pixel pixel)
        {
            return PixelCompare(pixel, Orange);
        }

        public static double Red1(this Pixel pixel)
        {
            return PixelCompare(pixel, Red);
        }

        public static double Grey1(this Pixel pixel)
        {
            return PixelCompare(pixel, Grey);
        }

        private static double PixelCompare(Pixel one, Color two)
        {
            var diffA = Math.Abs(one.Alpha - two.A);
            var diffR = Math.Abs(one.Red - two.R);
            var diffG = Math.Abs(one.Green - two.G);
            var diffB = Math.Abs(one.Blue - two.B);

            return Math.Sqrt(diffR * diffR + diffG * diffG + diffB * diffB);
        }

        #endregion

        #region Make Screen

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll")]
        static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        private const uint SRCCOPY = 0x00CC0020;
        private const uint PW_RENDERFULLCONTENT = 0x00000002; // Важно для современных приложений

        // Метод 1: Улучшенная версия PrintWindow
        private static Bitmap PrintWindowImproved(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return null;

            RECT rc;
            if (!GetWindowRect(hwnd, out rc) || rc.Width <= 0 || rc.Height <= 0)
                return null;

            try
            {
                Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
                Graphics gfxBmp = Graphics.FromImage(bmp);
                IntPtr hdcBitmap = gfxBmp.GetHdc();

                try
                {
                    // Пробуем с флагом PW_RENDERFULLCONTENT для современных приложений
                    bool succeeded = PrintWindow(hwnd, hdcBitmap, PW_RENDERFULLCONTENT);

                    if (!succeeded)
                    {
                        // Пробуем без флага
                        succeeded = PrintWindow(hwnd, hdcBitmap, 0);
                    }

                    if (!succeeded)
                    {
                        // Альтернативный метод, если PrintWindow не работает
                        return CaptureWindowAlternative(hwnd);
                    }
                }
                finally
                {
                    gfxBmp.ReleaseHdc(hdcBitmap);
                    gfxBmp.Dispose();
                }

                return bmp;
            }
            catch
            {
                return null;
            }
        }

        // Метод 2: Альтернативный способ через BitBlt
        private static Bitmap CaptureWindowAlternative(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return null;

            RECT rc;
            if (!GetWindowRect(hwnd, out rc) || rc.Width <= 0 || rc.Height <= 0)
                return null;

            IntPtr hdcSrc = GetWindowDC(hwnd);
            IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
            IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, rc.Width, rc.Height);
            IntPtr hOld = SelectObject(hdcDest, hBitmap);

            try
            {
                bool success = BitBlt(hdcDest, 0, 0, rc.Width, rc.Height, hdcSrc, 0, 0, SRCCOPY);

                if (!success)
                    return null;

                Bitmap bmp = Image.FromHbitmap(hBitmap);
                return bmp;
            }
            finally
            {
                SelectObject(hdcDest, hOld);
                DeleteObject(hBitmap);
                DeleteDC(hdcDest);
                ReleaseDC(hwnd, hdcSrc);
            }
        }

        // Метод 3: Комбинированный подход
        public static Bitmap CaptureProcessWindow(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                throw new ArgumentException("Invalid window handle");

            // Сначала пробуем улучшенный PrintWindow
            Bitmap result = PrintWindowImproved(windowHandle);

            // Проверяем, не черное ли изображение
            if (result != null && !IsBlackImage(result))
                return result;

            // Если черное, пробуем альтернативный метод
            result?.Dispose();
            return CaptureWindowAlternative(windowHandle);
        }

        // Проверка на черное изображение
        private static bool IsBlackImage(Bitmap bmp)
        {
            if (bmp == null) return true;

            // Быстрая проверка первых нескольких пикселей
            for (int x = 0; x < Math.Min(10, bmp.Width); x += 2)
            {
                for (int y = 0; y < Math.Min(10, bmp.Height); y += 2)
                {
                    if (bmp.GetPixel(x, y).GetBrightness() > 0.1f)
                        return false;
                }
            }
            return true;
        }

        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public RECT(System.Drawing.Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

            public int X
            {
                get { return Left; }
                set { Right -= (Left - value); Left = value; }
            }

            public int Y
            {
                get { return Top; }
                set { Bottom -= (Top - value); Top = value; }
            }

            public int Height
            {
                get { return Bottom - Top; }
                set { Bottom = value + Top; }
            }

            public int Width
            {
                get { return Right - Left; }
                set { Right = value + Left; }
            }

            public System.Drawing.Point Location
            {
                get { return new System.Drawing.Point(Left, Top); }
                set { X = value.X; Y = value.Y; }
            }

            public System.Drawing.Size Size
            {
                get { return new System.Drawing.Size(Width, Height); }
                set { Width = value.Width; Height = value.Height; }
            }

            public static implicit operator System.Drawing.Rectangle(RECT r)
            {
                return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
            }

            public static implicit operator RECT(System.Drawing.Rectangle r)
            {
                return new RECT(r);
            }

            public static bool operator ==(RECT r1, RECT r2)
            {
                return r1.Equals(r2);
            }

            public static bool operator !=(RECT r1, RECT r2)
            {
                return !r1.Equals(r2);
            }

            public bool Equals(RECT r)
            {
                return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
            }

            public override bool Equals(object obj)
            {
                if (obj is RECT)
                    return Equals((RECT)obj);
                else if (obj is System.Drawing.Rectangle)
                    return Equals(new RECT((System.Drawing.Rectangle)obj));
                return false;
            }

            public override int GetHashCode()
            {
                return ((System.Drawing.Rectangle)this).GetHashCode();
            }

            public override string ToString()
            {
                return string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
            }
        }

        #endregion
    }
}
