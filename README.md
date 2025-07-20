# AutoClick

AutoClick is a Windows desktop application for automating mouse and keyboard actions. It allows users to record, save, and replay sequences of clicks and keystrokes, making repetitive tasks easier and faster.

## Features

- Record mouse clicks and keyboard actions
- Save and load action sequences
- Replay actions with customizable intervals
- Capture screen regions
- Configurable settings via INI file

## Project Structure

- `App.xaml`, `App.xaml.cs`: Application entry and resources
- `MainWindow.xaml`, `MainWindow.xaml.cs`: Main UI and logic
- `CaptureWindow.xaml`, `CaptureWindow.xaml.cs`: Screen capture functionality
- `Models/`: Data models (`Action`, `Position`)
- `Execute/`: Mouse and keyboard automation logic
- `Utils/`: Utility classes (INI file handler, hook handler)

## Getting Started

1. Open `AutoClick.sln` in Visual Studio
2. Restore NuGet packages
3. Build and run the solution

## Dependencies

- [MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
- [OpenCvSharp](https://github.com/shimat/opencvsharp)
- [Gma.System.MouseKeyHook](https://github.com/gmamaladze/globalmousekeyhook)

## License

See [LICENSE](LICENSE) for details.

## Author

- [hungdoba](https://github.com/hungdoba)
