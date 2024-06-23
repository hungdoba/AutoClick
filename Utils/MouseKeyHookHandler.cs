using AutoClick.Models;
using Gma.System.MouseKeyHook;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoClick.Utils
{
    public class MouseKeyHookHandler
    {
        private IKeyboardMouseEvents? _events;
        private bool isMouseDrag = false;
        private const uint GA_ROOT = 2;
        private ObservableCollection<Models.Action> Actions;

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(System.Drawing.Point p);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        public MouseKeyHookHandler(ObservableCollection<Models.Action> actions)
        {
            Actions = actions;
        }

        public void SubscribeGlobal()
        {
            Unsubscribe();
            Subscribe(Hook.GlobalEvents());
        }

        public void Subscribe(IKeyboardMouseEvents events)
        {
            _events = events;
            _events.KeyPress += HookManager_KeyPress!;
            _events.MouseClick += OnMouseClick!;
            _events.MouseDoubleClick += OnMouseDoubleClick!;
            _events.MouseDragStarted += OnMouseDragStarted!;
            _events.MouseDragFinished += OnMouseDragFinished!;
            _events.MouseWheelExt += HookManager_MouseWheel!;
            _events.MouseHWheelExt += HookManager_MouseHWheel!;
            _events.MouseDownExt += HookManager_Suppress!;
        }

        public void Unsubscribe()
        {
            if (_events == null) return;

            _events.KeyPress -= HookManager_KeyPress!;
            _events.MouseClick -= OnMouseClick!;
            _events.MouseDoubleClick -= OnMouseDoubleClick!;
            _events.MouseDragStarted -= OnMouseDragStarted!;
            _events.MouseDragFinished -= OnMouseDragFinished!;
            _events.MouseWheelExt -= HookManager_MouseWheel!;
            _events.MouseHWheelExt -= HookManager_MouseHWheel!;
            _events.MouseDownExt -= HookManager_Suppress!;

            _events.Dispose();
            _events = null;
        }

        private void HookManager_Suppress(object? sender, MouseEventExtArgs e)
        {
            if (e.Button == MouseButtons.Right && !IsMouseEventInApp(e))
            {
                AddAction("mouse_click", e.Button.ToString(), new Position(e.X, e.Y));
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
                AddAction("mouse_click", e.Button.ToString(), new Position(e.X, e.Y));
            }
        }

        private void OnMouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (!IsMouseEventInApp(e))
            {
                Actions.Remove(Actions[Actions.Count - 1]);
                AddAction("mouse_double", e.Button.ToString(), new Position(e.X, e.Y), null, true);
            }
        }

        private void OnMouseDragStarted(object? sender, MouseEventArgs e)
        {
            if (!IsMouseEventInApp(e))
            {
                isMouseDrag = true;
                AddAction("mouse_drag", e.Button.ToString(), new Position(e.X, e.Y));
            }
        }

        private void OnMouseDragFinished(object? sender, MouseEventArgs e)
        {
            if (!IsMouseEventInApp(e))
            {
                isMouseDrag = false;
                AddAction("mouse_drop", e.Button.ToString(), new Position(e.X, e.Y));
            }
        }

        private void HookManager_MouseWheel(object? sender, MouseEventExtArgs e)
        {
            if (!IsMouseEventInApp(e))
            {
                AddAction("mouse_scroll", e.Button.ToString(), new Position(e.X, e.Y), e.Delta.ToString());
            }
        }

        private void HookManager_MouseHWheel(object? sender, MouseEventExtArgs e)
        {
            if (!IsMouseEventInApp(e))
            {
                AddAction("mouse_scroll", e.Button.ToString(), new Position(e.X, e.Y), e.Delta.ToString());
            }
        }

        private bool IsMouseEventInApp(MouseEventArgs e)
        {
            var windowPoint = new Point(e.X, e.Y);
            IntPtr hwnd = WindowFromPoint(windowPoint);
            IntPtr rootHwnd = GetAncestor(hwnd, GA_ROOT);

            var windowInteropHelper = new System.Windows.Interop.WindowInteropHelper(System.Windows.Application.Current.MainWindow);
            IntPtr appHwnd = windowInteropHelper.Handle;

            return rootHwnd == appHwnd;
        }

        private bool IsMouseEventInApp(KeyPressEventArgs e)
        {
            return System.Windows.Application.Current.MainWindow.IsActive;
        }

        private void AddAction(string type, string button, Position? point = null, string? delta = null, bool isDoubleClick = false)
        {
            var mainWindow = (MainWindow)System.Windows.Application.Current.MainWindow;
            mainWindow.AddAction(type, button, point, delta, isDoubleClick);
        }
    }
}
