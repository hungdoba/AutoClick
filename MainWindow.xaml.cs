using AutoClick.Models;
using AutoClick.Utils;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace AutoClick
{
    public partial class MainWindow : Window
    {
        private bool _isWatching = false;
        private long _previousActionTimestamp = 0;
        public ObservableCollection<Models.Action> Actions;

        private readonly IniFile _iniFile;
        private const string Section = "MainWindowPosition";
        private const string IniFileName = "setting.ini";
        private MouseKeyHookHandler hookHandler;

        private CancellationTokenSource? cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
            Actions = new ObservableCollection<Models.Action>();
            DgActions.ItemsSource = Actions;
            DeleteTempImage();
            string iniFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, IniFileName);
            _iniFile = new IniFile(iniFilePath);

            hookHandler = new MouseKeyHookHandler(Actions);
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            hookHandler.Unsubscribe();
            SaveWindowPosition();
        }

        private void SaveWindowPosition()
        {
            _iniFile.WriteValue(Section, "Left", Left.ToString());
            _iniFile.WriteValue(Section, "Top", Top.ToString());
            _iniFile.WriteValue(Section, "Width", Width.ToString());
            _iniFile.WriteValue(Section, "Height", Height.ToString());
        }

        public static void DeleteTempImage()
        {
            var imgFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_img");
            try
            {
                if (Directory.Exists(imgFolder))
                {
                    string[] files = Directory.GetFiles(imgFolder);
                    foreach (string file in files)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Delete temporary image error: {e.Message}");
            }
        }

        private void BtnOpenScript_Click(object sender, RoutedEventArgs e)
        {
            var recordFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "record");
            OpenFileDialog openFileDialog = new()
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                InitialDirectory = recordFolder,
                Title = "Select a JSON File"
            };

            // Show OpenFileDialog
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(openFileDialog.FileName);

                    var actions = JsonConvert.DeserializeObject<ObservableCollection<Models.Action>>(json);

                    Actions.Clear();

                    if (actions != null)
                    {
                        foreach (var action in actions)
                        {
                            Actions.Add(action);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading JSON file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSaveScript_Click(object sender, RoutedEventArgs e)
        {
            StopRecording();
            var recordFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "record");
            var imgFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img");

            if (!Directory.Exists(recordFolder))
            {
                Directory.CreateDirectory(recordFolder);
            }
            if (!Directory.Exists(imgFolder))
            {
                Directory.CreateDirectory(imgFolder);
            }

            foreach (Models.Action action in Actions)
            {
                if (action.Type == "mouse_template" && action.Data != null)
                {
                    string img = action.Data.ToString().Replace("_img", "img");
                    File.Copy(action.Data, img, true);
                    action.Data = img;
                }
                action.ExecuteSucceeded = null;
            }

            string json = JsonConvert.SerializeObject(Actions, Formatting.Indented);
            // Open the save file dialog
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                InitialDirectory = recordFolder,
                FileName = "temp.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveFileDialog.FileName, json);
            }
        }

        private void BtnResetScript_Click(object sender, RoutedEventArgs e)
        {
            Actions.Clear();
            _previousActionTimestamp = 0;
        }

        private void DgActions_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete)
            {
                foreach (Models.Action action in DgActions.SelectedItems)
                {
                    Actions.Remove(action);
                }
            }
        }

        public void AddAction(string type, string? button, Position? position = null, string? data = null, bool ignoreWait = false)
        {
            long timeNow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long waitTime = 1000;
            if (_previousActionTimestamp != 0 && !ignoreWait)
            {
                waitTime = timeNow - _previousActionTimestamp;
            }
            var waitAction = new Models.Action("wait", null, null, waitTime.ToString());
            Actions.Add(waitAction);
            _previousActionTimestamp = timeNow;
            var mouseAction = new Models.Action(type, button, position, data);
            Actions.Add(mouseAction);
            DgActions.ScrollIntoView(mouseAction);
        }

        public void ResetActionsExecuteSucceeded()
        {
            foreach (Models.Action action in Actions)
            {
                action.ExecuteSucceeded = null;
            }
            DgActions.Items.Refresh();
            DgActions.UpdateLayout();
        }

        private void ToolBar_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }

            Left += e.HorizontalChange;
            Top += e.VerticalChange;
        }

        private void BtnToggleRecord_Click(object sender, RoutedEventArgs e)
        {
            if (BtnToggleRecord.IsChecked == true)
            {
                StartRecording();
            }
            else
            {
                StopRecording();
            }
        }

        private void StartRecording()
        {
            hookHandler.SubscribeGlobal();
            _isWatching = true;
            BtnToggleRecord.IsChecked = true;
        }

        private void StopRecording()
        {
            hookHandler.Unsubscribe();
            _isWatching = false;
            BtnToggleRecord.IsChecked = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadWindowPosition();
        }

        private void LoadWindowPosition()
        {
            var left = _iniFile.ReadValue(Section, "Left", "0");
            var top = _iniFile.ReadValue(Section, "Top", "0");
            var width = _iniFile.ReadValue(Section, "Width", "340");
            var height = _iniFile.ReadValue(Section, "Height", "56");

            Left = double.Parse(left);
            Top = double.Parse(top);
            Width = double.Parse(width);
            Height = double.Parse(height);
        }


        private void ToolBarTray_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void BtnToggleScreenShot_Click(object sender, RoutedEventArgs e)
        {
            if (_isWatching)
            {
                StopRecording();
                CaptureWindow captureWindow = new();
                captureWindow.ShowDialog();
                string? savedFilePath = captureWindow.SavedFilePath;
                Position? point = captureWindow.StartPoint;

                if (!string.IsNullOrEmpty(savedFilePath))
                {
                    Execute.Mouse.Click.Template(savedFilePath);
                    AddAction("mouse_template", null, point, savedFilePath);
                }
                StartRecording();
            }
            BtnToggleScreenShot.IsChecked = false;
        }

        //private void BtnToggleScreenCheck_Click(object sender, RoutedEventArgs e)
        //{
        //    if (_isWatching)
        //    {
        //        hookHandler.Unsubscribe();
        //        CaptureWindow captureWindow = new();
        //        captureWindow.ShowDialog();
        //        string? savedFilePath = captureWindow.SavedFilePath;
        //        Position? point = captureWindow.StartPoint;

        //        if (!string.IsNullOrEmpty(savedFilePath) && point != null)
        //        {
        //            AddAction("screen_check", null, point, savedFilePath);
        //        }
        //    }
        //    BtnToggleScreenCheck.IsChecked = false;
        //}

        private async void BtnTogglePlay_Click(object sender, RoutedEventArgs e)
        {
            if (BtnTogglePlay.IsChecked == true)
            {
                if (Actions.Count == 0)
                {

                    BtnTogglePlay.IsChecked = false;
                    return;
                }

                if (!int.TryParse(CbExecuteTimes.Text, out int executeTimes))
                {
                    MessageBox.Show("Recheck the execute times");
                    BtnTogglePlay.IsChecked = false;
                    return;
                }

                if (executeTimes == 0)
                {
                    executeTimes = 1;
                }

                StopRecording();
                ResetActionsExecuteSucceeded();

                // Initialize cancellation token source
                cancellationTokenSource = new CancellationTokenSource();

                int waitTimeDrop = 0;
                bool isDrag = false;

                for (int i = executeTimes - 1; i >= 0; i--)
                {
                    foreach (Models.Action action in Actions)
                    {
                        if (cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            return;
                        }

                        bool isExecuteSucceeded = true;
                        action.ExecuteSucceeded = isExecuteSucceeded;
                        DgActions.Items.Refresh();
                        DgActions.UpdateLayout();
                        DgActions.ScrollIntoView(action);

                        switch (action.Type)
                        {
                            case "mouse_click":
                                if (action.Position == null)
                                {
                                    isExecuteSucceeded = false;
                                    break;
                                }
                                switch (action.Button)
                                {
                                    case "Left":
                                        await Task.Run(() => Execute.Mouse.Click.Left(action.Position));
                                        break;
                                    case "Middle":
                                        await Task.Run(() => Execute.Mouse.Click.Middle(action.Position));
                                        break;
                                    case "Right":
                                        await Task.Run(() => Execute.Mouse.Click.Right(action.Position));
                                        break;
                                }
                                break;
                            case "mouse_double":
                                if (action.Position == null)
                                {
                                    isExecuteSucceeded = false;
                                    break;
                                }
                                await Task.Run(() => Execute.Mouse.Click.Double(action.Position));
                                break;
                            case "mouse_drag":
                                if (action.Position == null)
                                {
                                    isExecuteSucceeded = false;
                                    break;
                                }
                                await Task.Run(() =>
                                {
                                    Execute.Mouse.Drag.Start(action.Position.X, action.Position.Y);
                                });
                                isDrag = true;
                                break;
                            case "mouse_drop":
                                if (action.Position == null)
                                {
                                    isExecuteSucceeded = false;
                                    break;
                                }
                                if (isDrag)
                                {
                                    await Task.Run(() => Execute.Mouse.Drag.End(action.Position.X, action.Position.Y, waitTimeDrop));
                                    isDrag = false;
                                }
                                break;
                            case "mouse_scroll":
                                if (action.Data == null)
                                {
                                    isExecuteSucceeded = false;
                                    break;
                                }
                                await Task.Run(() => Execute.Mouse.Scroll(int.Parse(action.Data)));
                                break;
                            case "mouse_template":
                                if (action.Data == null)
                                {
                                    isExecuteSucceeded = false;
                                    break;
                                }
                                await Task.Run(() => Execute.Mouse.Click.Template(action.Data));
                                break;
                            case "key_press":
                                if (action.Button == null)
                                {
                                    isExecuteSucceeded = false;
                                    break;
                                }
                                await Task.Run(() => Execute.Keyboard.Type(action.Button));
                                break;
                            case "wait":
                                if (action.Data == null)
                                {
                                    isExecuteSucceeded = false;
                                    break;
                                }
                                if (isDrag)
                                {
                                    waitTimeDrop = int.Parse(action.Data);
                                }
                                else
                                {
                                    await Task.Run(() => System.Threading.Thread.Sleep(int.Parse(action.Data)));
                                }
                                break;
                            default:
                                return;
                        }

                        if (!isExecuteSucceeded)
                        {
                            action.ExecuteSucceeded = isExecuteSucceeded;
                            DgActions.Items.Refresh();
                            DgActions.UpdateLayout();
                        }
                    }

                    CbExecuteTimes.Text = i.ToString();
                    ResetActionsExecuteSucceeded();
                }
            }
            else
            {
                cancellationTokenSource?.Cancel();
            }
            BtnTogglePlay.IsChecked = false;
        }

        private void BtnToggleExpand_Click(object sender, RoutedEventArgs e)
        {
            if (BtnToggleExpand.IsChecked == true)
            {
                Width = 510;
                Height = 600;
            }
            else
            {
                Width = 510;
                Height = 56;
            }
        }

        private void CloseApp_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Close();
        }

        private void MinimizeApp_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeApp_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void PackIcon_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Hand;
        }

        private void PackIcon_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Input.Mouse.OverrideCursor = null;
        }

    }
}
