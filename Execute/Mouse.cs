using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoClick.Execute
{
    internal class Mouse
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;


        internal static class Click
        {
            static public void Left(int x, int y)
            {
                // Set the cursor position
                SetCursorPos(x, y);

                // Simulate left mouse down and up to perform click
                mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, 0);
                mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
            }

            static public void Right(int x, int y)
            {
                // Set the cursor position
                SetCursorPos(x, y);

                // Simulate right mouse down and up to perform click
                mouse_event(MOUSEEVENTF_RIGHTDOWN, (uint)x, (uint)y, 0, 0);
                mouse_event(MOUSEEVENTF_RIGHTUP, (uint)x, (uint)y, 0, 0);
            }

            static public void Middle(int x, int y)
            {
                // Set the cursor position
                SetCursorPos(x, y);

                // Simulate middle mouse down and up to perform click
                mouse_event(MOUSEEVENTF_MIDDLEDOWN, (uint)x, (uint)y, 0, 0);
                mouse_event(MOUSEEVENTF_MIDDLEUP, (uint)x, (uint)y, 0, 0);
            }

            static public void Double(int x, int y)
            {
                // Set the cursor position
                SetCursorPos(x, y);

                // Simulate left mouse double click
                mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, 0);
                mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
                mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, 0);
                mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
            }


            static public void Template(string templateImagePath)
            {
                // Get position
                System.Drawing.Point? position = GetClickPosition(templateImagePath);
                if (position == null)
                {
                    return;
                }

                Left(position.Value.X, position.Value.Y);
            }

            public static System.Drawing.Point? GetClickPosition(string templateImagePath)
            {
                if (!File.Exists(templateImagePath))
                {
                    MessageBox.Show("Template image file not found");
                    return null;
                }

                Mat template = Cv2.ImRead(templateImagePath, ImreadModes.Color);

                if (template.Empty())
                {
                    MessageBox.Show("Failed to load template image");
                    return null;
                }

                Rectangle screenBounds = Rectangle.Empty;
                foreach (var screen in Screen.AllScreens)
                {
                    screenBounds = Rectangle.Union(screenBounds, screen.Bounds);
                }

                using Bitmap screenImage = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(screenImage))
                {
                    g.CopyFromScreen(screenBounds.Location, System.Drawing.Point.Empty, screenBounds.Size);
                }

                Mat screenMat = BitmapConverter.ToMat(screenImage);

                if (template.Channels() == 3)
                {
                    Cv2.CvtColor(screenMat, screenMat, ColorConversionCodes.BGRA2BGR);
                }
                else if (template.Channels() == 4)
                {
                    Cv2.CvtColor(screenMat, screenMat, ColorConversionCodes.BGRA2RGBA);
                }
                else
                {
                    MessageBox.Show($"Unsupported template image channel count: {template.Channels()}");
                    return null;
                }

                Mat result = new Mat();
                Cv2.MatchTemplate(screenMat, template, result, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                if (maxVal < 0.9)
                {
                    MessageBox.Show($"No match found above threshold");
                    return null;
                }

                return new System.Drawing.Point(maxLoc.X, maxLoc.Y);
            }
        }

        internal static class Drag
        {
            static public void Start(int x, int y)
            {
                SetCursorPos(x, y);
                mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, 0);
            }
            static public void End(int x, int y, int waitTime = 500)
            {
                SetCursorPos(x, y);
                Thread.Sleep(waitTime);
                mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
            }
        }


        public static void Scroll(int delta)
        {
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)(delta), 0);
        }
    }
}
