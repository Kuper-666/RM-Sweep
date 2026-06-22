# RM-Sweep - System Cleaner

Cross-platform system and registry cleaner built with C# (.NET 8) and Avalonia UI.

## Features

- **Temporary Files Cleanup** - Clean system and user temp directories, Prefetch
- **Browser Cache Cleanup** - Chrome, Edge, Firefox, Safari, Opera caches
- **Recycle Bin / Trash** - Empty recycle bin (Windows) or trash (macOS)
- **System Settings** - Clean registry (Windows) or preferences/plist files (macOS)
- **Autostart** - Clean autostart entries and startup folder
- **System Logs** - Clean system log files
- **Restore Point** - Create system restore point (Windows) or Time Machine snapshot (macOS)

## Localization

Supports 3 languages with real-time switching (no restart required):
- English (en-US)
- Russian (ru-RU)
- German (de-DE)

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10+ or macOS 10.15+

## Building

### Windows

```bash
dotnet publish src/RMSweep/RMSweep.csproj -c Release -r win-x64 --self-contained true -o output/win-x64
```

Or use the build script:
```bash
build\build-windows.bat
```

### macOS

```bash
dotnet publish src/RMSweep/RMSweep.csproj -c Release -r osx-x64 --self-contained true -o output/osx-x64
```

Or use the build script:
```bash
chmod +x build/build-mac.sh
./build/build-mac.sh
```

This creates a `.app` bundle at `output/RM-Sweep.app`.

### macOS .app Bundle

The build script automatically creates a proper macOS app bundle with:
- `RM-Sweep.app/Contents/MacOS/` - Executable and dependencies
- `RM-Sweep.app/Contents/Resources/` - Icon
- `RM-Sweep.app/Contents/Info.plist` - App metadata

### Windows Installer

Use [Inno Setup](https://jrsoftware.org/isdl.php) with `build/installer.iss` to create a Windows installer.

## Architecture

```
src/RMSweep/
├── Program.cs              # Entry point
├── App.axaml / .cs         # Avalonia application
├── Interfaces/
│   └── ISystemCleaner.cs   # Strategy interface
├── Cleaners/
│   ├── CleanerFactory.cs   # Platform factory
│   ├── WindowsCleaner.cs   # Windows implementation
│   └── MacCleaner.cs       # macOS implementation
├── ViewModels/
│   └── MainWindowViewModel.cs
├── Views/
│   └── MainWindow.axaml / .cs
├── Services/
│   ├── LocalizationService.cs  # Runtime localization
│   ├── SettingsService.cs      # User preferences
│   └── LogService.cs           # Operation logging
├── Models/
│   └── CleanModels.cs      # Data models
└── Resources/Localization/
    ├── Strings.resx        # English
    ├── Strings.ru-RU.resx  # Russian
    └── Strings.de-DE.resx  # German
```

## Design Patterns

- **Strategy Pattern** - `ISystemCleaner` interface with platform-specific implementations
- **Factory Pattern** - `CleanerFactory` creates the correct cleaner based on OS
- **MVVM** - Model-View-ViewModel with CommunityToolkit.Mvvm
- **Async/Await** - All cleaning operations are async with cancellation support

## Admin Rights

- **Windows**: Runs with administrator privileges (required manifest)
- **macOS**: Runs as current user. System-level operations may require elevated permissions.

## License

MIT
