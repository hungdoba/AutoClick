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

        private CancellationTokenSource cancellationTokenSource;

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
            }

            string json = JsonConvert.SerializeObject(Actions, Formatting.Indented);
            // Open the save file dialog
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                InitialDirectory = recordFolder,
                FileName = "1.json"
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

        public void AddAction(string type, string? button, System.Drawing.Point? position = null, string? data = null, bool ignoreWait = false)
        {
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
            var width = _iniFile.ReadValue(Section, "Width", "388");
            var height = _iniFile.ReadValue(Section, "Height", "56");

            Left = double.Parse(left);
            Top = double.Parse(top);
            Width = double.Parse(width);
            Height = double.Parse(height);
        }

        private void BtnWindowMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnWindowMaximize_Click(object sender, RoutedEventArgs e)
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

        private void BtnWindowClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToolBarTray_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void BtnToggleScreenShot_Click(object sender, RoutedEventArgs e)
        {
            if (_isWatching)
            {
                hookHandler.Unsubscribe();
                CaptureWindow captureWindow = new();
                captureWindow.ShowDialog();
                string? savedFilePath = captureWindow.SavedFilePath;

                if (!string.IsNullOrEmpty(savedFilePath))
                {
                    Execute.Mouse.Click.Template(savedFilePath);
                    AddAction("mouse_template", null, null, savedFilePath);
                }
            }
            BtnToggleScreenShot.IsChecked = false;
        }

        private async void BtnTogglePlay_Click(object sender, RoutedEventArgs e)
        {
            if (BtnTogglePlay.IsChecked == true)
            {
                if (Actions.Count == 0)
                    return;

                if (!int.TryParse(CbExecuteTimes.Text, out int executeTimes) || executeTimes < 1)
                {
                    MessageBox.Show("Recheck the execute times");
                    return;
                }

                StopRecording();
                ResetActionsExecuteSucceeded();

                // Initialize cancellation token source
                cancellationTokenSource = new CancellationTokenSource();

                hookHandler.Unsubscribe();
                int waitTimeDrop = 0;
                bool isDrag = false;

                for (int i = 0; i < executeTimes; i++)
                {
                    foreach (Models.Action action in Actions)
                    {
                        // Check if cancellation has been requested
                        if (cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            // Clean up and return if cancellation requested
                            hookHandler.SubscribeGlobal(); // Subscribe back to global events
                            return;
                        }

                        bool isExecuteSucceeded = true;

                        //switch (action.Type)
                        //{
                        //    case "mouse_click":
                        //        if (action.Position == null)
                        //        {
                        //            isExecuteSucceeded = false;
                        //            break;
                        //        }
                        //        switch (action.Button)
                        //        {
                        //            case "Left":
                        //                await Task.Run(() => Execute.Mouse.Click.Left(action.Position.Value.X, action.Position.Value.Y));
                        //                break;
                        //            case "Middle":
                        //                await Task.Run(() => Execute.Mouse.Click.Middle(action.Position.Value.X, action.Position.Value.Y));
                        //                break;
                        //            case "Right":
                        //                await Task.Run(() => Execute.Mouse.Click.Right(action.Position.Value.X, action.Position.Value.Y));
                        //                break;
                        //        }
                        //        break;
                        //    case "mouse_double":
                        //        if (action.Position == null)
                        //        {
                        //            isExecuteSucceeded = false;
                        //            break;
                        //        }
                        //        await Task.Run(() => Execute.Mouse.Click.Double(action.Position.Value.X, action.Position.Value.Y));
                        //        break;
                        //    case "mouse_drag":
                        //        if (action.Position == null)
                        //        {
                        //            isExecuteSucceeded = false;
                        //            break;
                        //        }
                        //        await Task.Run(() =>
                        //        {
                        //            Execute.Mouse.Drag.Start(action.Position.Value.X, action.Position.Value.Y);
                        //        });
                        //        isDrag = true;
                        //        break;
                        //    case "mouse_drop":
                        //        if (action.Position == null)
                        //        {
                        //            isExecuteSucceeded = false;
                        //            break;
                        //        }
                        //        if (isDrag)
                        //        {
                        //            await Task.Run(() => Execute.Mouse.Drag.End(action.Position.Value.X, action.Position.Value.Y, waitTimeDrop));
                        //            isDrag = false;
                        //        }
                        //        break;
                        //    case "mouse_scroll":
                        //        if (action.Data == null)
                        //        {
                        //            isExecuteSucceeded = false;
                        //            break;
                        //        }
                        //        await Task.Run(() => Execute.Mouse.Scroll(int.Parse(action.Data)));
                        //        break;
                        //    case "mouse_template":
                        //        if (action.Data == null)
                        //        {
                        //            isExecuteSucceeded = false;
                        //            break;
                        //        }
                        //        await Task.Run(() => Execute.Mouse.Click.Template(action.Data));
                        //        break;
                        //    case "key_press":
                        //        if (action.Button == null)
                        //        {
                        //            isExecuteSucceeded = false;
                        //            break;
                        //        }
                        //        await Task.Run(() => Execute.Keyboard.Type(action.Button));
                        //        break;
                        //    case "wait":
                        //        if (action.Data == null)
                        //        {
                        //            isExecuteSucceeded = false;
                        //            break;
                        //        }
                        //        if (isDrag)
                        //        {
                        //            waitTimeDrop = int.Parse(action.Data);
                        //        }
                        //        else
                        //        {
                        //            await Task.Run(() => System.Threading.Thread.Sleep(int.Parse(action.Data)));
                        //        }
                        //        break;
                        //    default:
                        //        return;
                        //}

                        action.ExecuteSucceeded = isExecuteSucceeded;
                        DgActions.Items.Refresh();
                        DgActions.UpdateLayout();
                        DgActions.ScrollIntoView(action);

                        await Task.Run(() => Thread.Sleep(500));

                        Random random = new Random();
                        bool randomBool = random.Next(2) == 1;
                        if (randomBool)
                        {
                            action.ExecuteSucceeded = false;
                            DgActions.Items.Refresh();
                            DgActions.UpdateLayout();
                        }
                    }

                    CbExecuteTimes.Text = i.ToString();
                    ResetActionsExecuteSucceeded();
                }
                hookHandler.SubscribeGlobal();
                BtnTogglePlay.IsChecked = false;
            }
            else
            {
                cancellationTokenSource?.Cancel();
            }
        }

        private void BtnToggleExpand_Click(object sender, RoutedEventArgs e)
        {
            if (BtnToggleExpand.IsChecked == true)
            {
                Width = 610;
                Height = 610;
            }
            else
            {
                Width = 610;
                Height = 56;
            }
        }
    }
}
