// MainWindow.OnlineTests.Capture.cs

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // SCREEN CAPTURE
        // =========================================================

        private Bitmap CaptureVisionScreen()
        {
            try
            {
                Rectangle bounds =
                    Screen.PrimaryScreen.Bounds;

                Bitmap bitmap =
                    new Bitmap(
                        bounds.Width,
                        bounds.Height,
                        PixelFormat.Format24bppRgb);

                using (Graphics graphics =
                       Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(
                        bounds.Left,
                        bounds.Top,
                        0,
                        0,
                        bounds.Size,
                        CopyPixelOperation.SourceCopy);
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                DebugLog(
                    "CaptureVisionScreen ERROR: " +
                    ex);

                return null;
            }
        }


        // =========================================================
        // BITMAP -> BASE64 JPEG
        // =========================================================

        private string ConvertBitmapToBase64(
            Bitmap bitmap)
        {
            if (bitmap == null)
            {
                return string.Empty;
            }

            try
            {
                using (MemoryStream stream =
                       new MemoryStream())
                {
                    bitmap.Save(
                        stream,
                        ImageFormat.Jpeg);

                    return Convert.ToBase64String(
                        stream.ToArray());
                }
            }
            catch (Exception ex)
            {
                DebugLog(
                    "ConvertBitmapToBase64 ERROR: " +
                    ex);

                return string.Empty;
            }
        }
    }
}