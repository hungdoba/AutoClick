using AutoClick.Models;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AutoClick
{
    public partial class CaptureWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool BlockInput(bool fBlockIt);

        private Rectangle selectionRectangle;

        public Position? StartPoint;
        public string? SavedFilePath { get; private set; }

        public CaptureWindow()
        {
            InitializeComponent();
            SetFullScreenAcrossAllMonitors();
            selectionRectangle = new Rectangle
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0))
            };
            SnippingCanvas.Children.Add(selectionRectangle);
        }

        private void SetFullScreenAcrossAllMonitors()
        {
            // Calculate the total size of all screens
            int totalWidth = 0;
            int totalHeight = 0;
            int minX = int.MaxValue;
            int minY = int.MaxValue;

            foreach (var screen in Screen.AllScreens)
            {
                minX = Math.Min(minX, screen.Bounds.X);
                minY = Math.Min(minY, screen.Bounds.Y);
                totalWidth += screen.Bounds.Width;
                totalHeight = Math.Max(totalHeight, screen.Bounds.Height);
            }

            // Set the window size and position to cover all screens
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Left = minX;
            this.Top = minY;
            this.Width = totalWidth;
            this.Height = totalHeight;
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point point = e.GetPosition(this);
            StartPoint = new Position((int)point.X, (int)point.Y);
            Canvas.SetLeft(selectionRectangle, StartPoint.X);
            Canvas.SetTop(selectionRectangle, StartPoint.Y);
            selectionRectangle.Width = 0;
            selectionRectangle.Height = 0;
        }

        private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(this);
                var x = Math.Min(pos.X, StartPoint.X);
                var y = Math.Min(pos.Y, StartPoint.Y);
                var width = Math.Abs(pos.X - StartPoint.X);
                var height = Math.Abs(pos.Y - StartPoint.Y);
                Canvas.SetLeft(selectionRectangle, x);
                Canvas.SetTop(selectionRectangle, y);
                selectionRectangle.Width = width;
                selectionRectangle.Height = height;
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var endPoint = e.GetPosition(this);
            if (StartPoint != null)
            {
                CaptureSnip(StartPoint, endPoint);
            }
            this.Close();
        }

        private void CaptureSnip(Position start, Point end)
        {
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point(2000, 0);
            this.Hide();
            Thread.Sleep(100);

            var x = (int)Math.Min(start.X, end.X);
            var y = (int)Math.Min(start.Y, end.Y);
            var width = (int)Math.Abs(start.X - end.X);
            var height = (int)Math.Abs(start.Y - end.Y);

            if (width > 0 && height > 0)
            {
                using var bmp = new System.Drawing.Bitmap(width, height);
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));
                }

                var imgFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_img");
                if (!System.IO.Directory.Exists(imgFolder))
                {
                    System.IO.Directory.CreateDirectory(imgFolder);
                }

                var fileName = System.IO.Path.Combine(imgFolder, $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.png");
                bmp.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);

                SavedFilePath = fileName;
            }

            this.Close();
        }


        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}
