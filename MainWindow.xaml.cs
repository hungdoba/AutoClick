using Gma.System.MouseKeyHook;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;

namespace AutoClick
{
    public partial class MainWindow : Window
    {
        private readonly string _logFilePath = @"C:\Users\70830717\Desktop\autoclick.json";
        private Boolean isMouseDrag = false;
        private IKeyboardMouseEvents? _events;
        private long _previousActionTimestamp = 0;
        private System.Windows.Point _startPoint;

        public ObservableCollection<Models.Action> Actions;

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(System.Drawing.Point p);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);


        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hwnd, ref RECT rect);

        private const uint GA_ROOT = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public MainWindow()
        {
            InitializeComponent();
            Actions = new ObservableCollection<Models.Action>();
            DgActions.ItemsSource = Actions;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Unsubscribe();
        }

        private void SubscribeGlobal()
        {
            Unsubscribe();
            Subscribe(Hook.GlobalEvents());
        }

        private void Subscribe(IKeyboardMouseEvents events)
        {
            _events = events;
            _events.KeyPress += HookManager_KeyPress!;
            _events.MouseClick += OnMouseClick!;
            _events.MouseDoubleClick += OnMouseDoubleClick!;
            _events.MouseDragStarted += OnMouseDragStarted!;
            _events.MouseDragFinished += OnMouseDragFinished!;
            _events.MouseWheelExt += HookManager_MouseWheelExt!;
            _events.MouseHWheelExt += HookManager_MouseHWheelExt!;
            _events.MouseDownExt += HookManager_Suppress!;
        }

        private void Unsubscribe()
        {
            if (_events == null) return;

            _events.KeyPress -= HookManager_KeyPress!;
            _events.MouseClick -= OnMouseClick!;
            _events.MouseDoubleClick -= OnMouseDoubleClick!;
            _events.MouseDragStarted -= OnMouseDragStarted!;
            _events.MouseDragFinished -= OnMouseDragFinished!;
            _events.MouseWheelExt -= HookManager_MouseWheelExt!;
            _events.MouseHWheelExt -= HookManager_MouseHWheelExt!;
            _events.MouseDownExt -= HookManager_Suppress!;

            _events.Dispose();
            _events = null;
        }



        // Add Action
        public void AddAction(string type, string button, System.Drawing.Point? position = null, string? data = null, bool ignoreWait = false)
        {
            // Add action include add wait
            long timeNow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (_previousActionTimestamp != 0 && !ignoreWait)
            {
                long waitTime = timeNow - _previousActionTimestamp;
                var waitAction = new Models.Action("wait", null, null, waitTime.ToString());
                Actions.Add(waitAction);
            }
            _previousActionTimestamp = timeNow;
            var mouseAction = new Models.Action(type, button, position, data);
            Actions.Add(mouseAction);
            DgActions.ScrollIntoView(mouseAction);
        }

        // Event handle
        private void HookManager_Suppress(object? sender, MouseEventExtArgs e)
        {
            if (e.Button == MouseButtons.Right && !IsMouseEventInApp(e))
            {
                AddAction("mouse_click", e.Button.ToString(), new System.Drawing.Point(e.X, e.Y));
                e.Handled = true;
            }
        }

        private void HookManager_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!IsMouseEventInApp(e))
            {
                AddAction("key_press", e.KeyChar.ToString());
            }
        }

        private void OnMouseClick(object? sender, MouseEventArgs e)
        {
            if (!IsMouseEventInApp(e) && !isMouseDrag)
            {
                bool isCtrlPressed = (System.Windows.Forms.Control.ModifierKeys & Keys.Control) == Keys.Control;
                if (isCtrlPressed && e.Button == MouseButtons.Left)
                {
                    string templatePath = CaptureScreenArea(e.X, e.Y, 100, 100);
                    AddAction("mouse_template", e.Button.ToString(), new System.Drawing.Point(e.X, e.Y), templatePath);
                }
                else
                {
                    AddAction("mouse_click", e.Button.ToString(), new System.Drawing.Point(e.X, e.Y));
                }
            }
        }
        private void OnMouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (!IsMouseEventInApp(e))
            {
                // Remove event first click
                Actions.Remove(Actions.Last());
                AddAction("mouse_double", e.Button.ToString(), new System.Drawing.Point(e.X, e.Y), null, true);
            }
        }

        private void OnMouseDragStarted(object? sender, MouseEventArgs e)
        {
            if (!IsMouseEventInApp(e))
            {
                isMouseDrag = true;
                AddAction("mouse_drag", e.Button.ToString(), new System.Drawing.Point(e.X, e.Y));
            }
        }

        private void OnMouseDragFinished(object? sender, MouseEventArgs e)
        {
            if (!IsMouseEventInApp(e))
            {
                isMouseDrag = false;
                AddAction("mouse_drop", e.Button.ToString(), new System.Drawing.Point(e.X, e.Y));
            }
        }

        private void HookManager_MouseWheelExt(object? sender, MouseEventExtArgs e)
        {
            if (!IsMouseEventInApp(e))
            {
                AddAction("mouse_scroll", e.Button.ToString(), new System.Drawing.Point(e.X, e.Y), e.Delta.ToString());
                e.Handled = true;
            }
        }

        private void HookManager_MouseHWheelExt(object? sender, MouseEventExtArgs e)
        {
            if (!IsMouseEventInApp(e))
            {
                AddAction("mouse_scroll", e.Button.ToString(), new System.Drawing.Point(e.X, e.Y), e.Delta.ToString());
                e.Handled = true;
            }
        }



        // Ultils
        private string CaptureScreenArea(int x, int y, int width, int height)
        {
            using (Bitmap bmp = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(x - width / 2, y - height / 2, 0, 0, new System.Drawing.Size(width, height));
                }
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"template_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                bmp.Save(filePath, ImageFormat.Png);
                string outputPath = filePath.Replace("template", "screen");
                return filePath;
            }
        }

        private bool IsMouseEventInApp(MouseEventArgs e)
        {
            var windowPoint = new System.Drawing.Point(e.X, e.Y);
            IntPtr hwnd = WindowFromPoint(windowPoint);
            IntPtr rootHwnd = GetAncestor(hwnd, GA_ROOT);

            // Get the application window's handle
            var windowInteropHelper = new System.Windows.Interop.WindowInteropHelper(this);
            IntPtr appHwnd = windowInteropHelper.Handle;

            return rootHwnd == appHwnd;
        }

        private bool IsMouseEventInApp(KeyPressEventArgs e)
        {
            // Assuming all key presses within the app should not be logged
            // This can be more complex if needed to distinguish between different key presses
            return IsActive; // Only log key presses if the application is not active
        }

        private void BtnSaveLog_Click(object sender, RoutedEventArgs e)
        {
            // Serialize actions to JSON
            string json = JsonConvert.SerializeObject(Actions, Formatting.Indented);

            // Write JSON to file
            File.WriteAllText(_logFilePath, json);
        }

        private void LbActions_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete)
            {
                foreach (Models.Action action in DgActions.SelectedItems.Cast<Models.Action>().ToList())
                {
                    Actions.Remove(action);
                }
            }
        }

        private void BtnExecute_Click(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(1000);
            Execute.Mouse.Click.Template(@"C:\Users\70830717\Desktop\template_20240621_160357.png");
            //Thread.Sleep(1000);
            //Execute.Keyboard.Type("Hello, world!");

            //Execute.Mouse.Scroll(-120);
            //Thread.Sleep(1000);
            //Execute.Mouse.Scroll(120);
            // Start dragging from (1863, 43)
            //Execute.Mouse.Drag.Start(1869, 34);

            // End dragging at (1563, 143)
            //Execute.Mouse.Drag.End(1651, 376);

            //if (Actions.Count() == 0)
            //    return;

            //Unsubscribe();
            //foreach (Models.Action action in Actions)
            //{
            //    switch (action.Type)
            //    {
            //        case "mouse_click":
            //            switch (action.Button)
            //            {
            //                case "Left":
            //                    System.Windows.MessageBox.Show("Click Left");
            //                    break;
            //                case "Middle":
            //                    System.Windows.MessageBox.Show("Click Middle");
            //                    break;
            //                case "Right":
            //                    System.Windows.MessageBox.Show("Click Right");
            //                    break;
            //            }
            //            break;
            //        case "mouse_double":
            //            System.Windows.MessageBox.Show("Double Click");
            //            break;
            //        case "mouse_drag":
            //            System.Windows.MessageBox.Show("Drag");
            //            break;
            //        case "mouse_drop":
            //            System.Windows.MessageBox.Show("Drop");
            //            break;
            //        case "mouse_scroll":
            //            System.Windows.MessageBox.Show("Scroll");
            //            break;
            //        case "mouse_template":
            //            System.Windows.MessageBox.Show("Click Template");
            //            break;
            //        case "key_press":
            //            System.Windows.MessageBox.Show("Key press");
            //            break;
            //        case "wait":
            //            System.Windows.MessageBox.Show("Wait");
            //            break;
            //        default:
            //            return;
            //    }
            //}
        }

        private void BtnReadFile_Click(object sender, RoutedEventArgs e)
        {
            // Read the entire file content as a string
            string json = File.ReadAllText(_logFilePath);

            // Deserialize JSON string to a list of Action objects
            var actionsList = JsonConvert.DeserializeObject<List<Models.Action>>(json);

            Actions.Clear();
            // Convert list to ObservableCollection
            foreach (var action in actionsList)
            {
                Actions.Add(action);
            }
        }

        private void Subscribe_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Unsubscribe_Click(object sender, RoutedEventArgs e)
        {
            Unsubscribe();
        }

        private void BtnSubscribe_Click(object sender, RoutedEventArgs e)
        {
            SubscribeGlobal();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            Actions.Clear();
            _previousActionTimestamp = 0;
        }

        private void BtnCapture_Click(object sender, RoutedEventArgs e)
        {
            CaptureWindow captureWindow = new CaptureWindow();
            captureWindow.ShowDialog();
        }
    }
}
