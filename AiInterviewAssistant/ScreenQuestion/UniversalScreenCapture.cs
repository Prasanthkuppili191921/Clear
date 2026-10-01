using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AiInterviewAssistant.ScreenQuestion
{
    public sealed class UniversalScreenCapture : IDisposable
    {
        private readonly object _lock =
            new object();

        private Bitmap _lastFrame;

        // =========================================================
        // PUBLIC CAPTURE API
        // =========================================================

        public async Task<Bitmap> CaptureAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    try
                    {
                        Bitmap currentFrame =
                            CaptureDesktopFrame();

                        if (currentFrame == null)
                            return null;

                        _lastFrame?.Dispose();

                        _lastFrame =
                            new Bitmap(currentFrame);

                        return currentFrame;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "UNIVERSAL CAPTURE ERROR:");

                        System.Diagnostics.Debug.WriteLine(
                            ex.ToString());

                        return null;
                    }
                }
            });
        }

        // =========================================================
        // DESKTOP CAPTURE
        // =========================================================

        private Bitmap CaptureDesktopFrame()
        {
            int width =
                GetSystemMetrics(
                    SM_CXVIRTUALSCREEN);

            int height =
                GetSystemMetrics(
                    SM_CYVIRTUALSCREEN);

            int left =
                GetSystemMetrics(
                    SM_XVIRTUALSCREEN);

            int top =
                GetSystemMetrics(
                    SM_YVIRTUALSCREEN);

            if (width <= 0 ||
                height <= 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    "UNIVERSAL CAPTURE: Invalid screen bounds.");

                return null;
            }

            Bitmap bitmap =
                new Bitmap(
                    width,
                    height,
                    PixelFormat.Format32bppArgb);

            IntPtr sourceDc =
                IntPtr.Zero;

            try
            {
                // =====================================================
                // GET DESKTOP DC
                // =====================================================

                sourceDc =
                    GetDC(IntPtr.Zero);

                if (sourceDc == IntPtr.Zero)
                {
                    bitmap.Dispose();

                    System.Diagnostics.Debug.WriteLine(
                        "UNIVERSAL CAPTURE: GetDC failed.");

                    return null;
                }

                // =====================================================
                // DESTINATION = OUR BITMAP
                // =====================================================

                using (Graphics graphics =
                       Graphics.FromImage(bitmap))
                {
                    IntPtr destinationDc =
                        graphics.GetHdc();

                    try
                    {
                        // =================================================
                        // COPY CURRENT DESKTOP FRAME
                        // =================================================

                        bool success =
                            BitBlt(
                                destinationDc,
                                0,
                                0,
                                width,
                                height,
                                sourceDc,
                                left,
                                top,
                                SRCCOPY |
                                CAPTUREBLT);

                        if (!success)
                        {
                            int error =
                                Marshal.GetLastWin32Error();

                            System.Diagnostics.Debug.WriteLine(
                                "UNIVERSAL CAPTURE: BitBlt failed. " +
                                "Win32 Error = " +
                                error);

                            bitmap.Dispose();

                            return null;
                        }
                    }
                    finally
                    {
                        graphics.ReleaseHdc(
                            destinationDc);
                    }
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "UNIVERSAL DESKTOP CAPTURE ERROR:");

                System.Diagnostics.Debug.WriteLine(
                    ex.ToString());

                bitmap.Dispose();

                return null;
            }
            finally
            {
                if (sourceDc != IntPtr.Zero)
                {
                    ReleaseDC(
                        IntPtr.Zero,
                        sourceDc);
                }
            }
        }

        // =========================================================
        // CLONE
        // =========================================================

        private Bitmap CloneBitmap(
            Bitmap source)
        {
            if (source == null)
                return null;

            return new Bitmap(source);
        }

        // =========================================================
        // DISPOSE
        // =========================================================

        public void Dispose()
        {
            lock (_lock)
            {
                try
                {
                    _lastFrame?.Dispose();
                }
                catch
                {
                }

                _lastFrame = null;
            }
        }

        // =========================================================
        // SCREEN METRICS
        // =========================================================

        private const int SM_XVIRTUALSCREEN = 76;

        private const int SM_YVIRTUALSCREEN = 77;

        private const int SM_CXVIRTUALSCREEN = 78;

        private const int SM_CYVIRTUALSCREEN = 79;

        // =========================================================
        // BITBLT FLAGS
        // =========================================================

        private const int SRCCOPY =
            0x00CC0020;

        private const int CAPTUREBLT =
            0x40000000;

        // =========================================================
        // USER32
        // =========================================================

        [DllImport(
            "user32.dll",
            SetLastError = true)]
        private static extern int GetSystemMetrics(
            int nIndex);

        [DllImport(
            "user32.dll",
            SetLastError = true)]
        private static extern IntPtr GetDC(
            IntPtr hWnd);

        [DllImport(
            "user32.dll",
            SetLastError = true)]
        private static extern int ReleaseDC(
            IntPtr hWnd,
            IntPtr hDC);

        // =========================================================
        // GDI32
        // =========================================================

        [DllImport(
            "gdi32.dll",
            SetLastError = true)]
        private static extern bool BitBlt(
            IntPtr hdcDest,
            int nXDest,
            int nYDest,
            int nWidth,
            int nHeight,
            IntPtr hdcSrc,
            int nXSrc,
            int nYSrc,
            int dwRop);
    }
}