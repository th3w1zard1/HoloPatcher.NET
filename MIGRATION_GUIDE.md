# HoloPatcher Python to C#/.NET Migration Guide

This document outlines the migration from Python/Tkinter to C#/.NET/Avalonia.

## Testing Infrastructure ✅

### Test Project Setup
- ✅ xUnit test framework
- ✅ FluentAssertions for readable assertions
- ✅ Moq for mocking dependencies
- ✅ 38 unit tests covering core components
- ✅ Integrated into build scripts
- ✅ CI/CD ready

### Test Coverage
| Component | Tests | Status |
|-----------|-------|--------|
| PatcherMemory | 5 | ✅ Complete |
| PatchLogger | 10 | ✅ Complete |
| PatcherConfig | 9 | ✅ Complete |
| LogLevel | 4 | ✅ Complete |
| PatcherNamespace | 10 | ✅ Complete |

See [TESTING.md](TESTING.md) and [TEST_IMPLEMENTATION_SUMMARY.md](TEST_IMPLEMENTATION_SUMMARY.md) for details.

## Completed Components ✅

### 1. Project Structure
- ✅ Solution file (`HoloPatcher.sln`)
- ✅ Main UI project (`HoloPatcher`)
- ✅ Core library project (`TSLPatcher.Core`)
- ✅ Package references (Avalonia, CommunityToolkit.Mvvm)

### 2. Core Models (TSLPatcher.Core)
- ✅ `Logger/LogType.cs` - Log entry types
- ✅ `Logger/PatchLog.cs` - Individual log entries
- ✅ `Logger/PatchLogger.cs` - Main logging system with events
- ✅ `Config/LogLevel.cs` - Configuration log levels
- ✅ `Config/PatcherConfig.cs` - Main configuration model
- ✅ `Memory/PatcherMemory.cs` - Token memory storage
- ✅ `Namespaces/PatcherNamespace.cs` - Namespace management

### 3. UI Components (HoloPatcher)
- ✅ `Program.cs` - Application entry point
- ✅ `App.axaml/.cs` - Avalonia application
- ✅ `Views/MainWindow.axaml/.cs` - Main window view
- ✅ `ViewModels/MainWindowViewModel.cs` - Main view model with commands

## Pending Components 🚧

### High Priority

#### 1. ConfigReader (`TSLPatcher.Core/Config/ConfigReader.cs`)
**Python Source**: `pykotor/tslpatcher/reader.py` (lines 131-1546)

Key functionality to port:
- INI file parsing (ConfigParser → IConfiguration or custom)
- Section reading (`load_settings`, `load_install_list`, etc.)
- Field value parsing
- GFF list parsing
- 2DA list parsing
- TSLPatcher type resolution

**C# Implementation Strategy**:
```csharp
public class ConfigReader
{
    private readonly IniParser _parser;
    private readonly PatchLogger _logger;
    
    public PatcherConfig Load(string iniPath) { }
    private void LoadSettings() { }
    private void LoadInstallList() { }
    private void LoadTlkList() { }
    // ... etc
}
```

#### 2. ModInstaller (`TSLPatcher.Core/Patcher/ModInstaller.cs`)
**Python Source**: `pykotor/tslpatcher/patcher.py` (lines 42-525)

Key functionality:
- Resource lookup
- Capsule handling (ERF/MOD/RIM files)
- Backup creation
- Patch application
- Progress tracking

**C# Implementation Strategy**:
```csharp
public class ModInstaller
{
    public async Task InstallAsync(
        CancellationToken cancellationToken,
        IProgress<int> progress)
    {
        // Installation logic
    }
}
```

### Medium Priority

#### 3. GFF Modifications (`TSLPatcher.Core/Mods/GFF/`)
**Python Source**: `pykotor/tslpatcher/mods/gff.py`

Classes to port:
- `ModificationsGFF`
- `ModifyFieldGFF`
- `AddFieldGFF`
- `AddStructToListGFF`
- `Memory2DAModifierGFF`
- `LocalizedStringDelta`
- Field value classes

#### 4. 2DA Modifications (`TSLPatcher.Core/Mods/TwoDA/`)
**Python Source**: `pykotor/tslpatcher/mods/twoda.py`

Classes to port:
- `Modifications2DA`
- `AddRow2DA`
- `ChangeRow2DA`
- `CopyRow2DA`
- `AddColumn2DA`
- Row value classes

#### 5. TLK Modifications (`TSLPatcher.Core/Mods/TLK/`)
**Python Source**: `pykotor/tslpatcher/mods/tlk.py`

#### 6. Script Compilation (`TSLPatcher.Core/Mods/NSS/`)
**Python Source**: `pykotor/tslpatcher/mods/nss.py` and `ncs.py`

### Lower Priority

#### 7. Uninstaller (`TSLPatcher.Core/Patcher/ModUninstaller.cs`)
**Python Source**: `pykotor/tslpatcher/uninstall.py`

#### 8. UI Enhancements
- RTF/RTE file viewer
- Update checker
- Permission fixer
- iOS case sensitivity tool

## Key Differences: Python → C#

### 1. Threading Model
**Python**:
```python
thread = Thread(target=self.begin_install_thread)
thread.start()
```

**C#**:
```csharp
await Task.Run(() => InstallAsync(cancellationToken));
```

### 2. Event System
**Python**:
```python
self.logger.verbose_observable.subscribe(self.write_log)
```

**C#**:
```csharp
_logger.VerboseLogged += OnVerboseLog;
```

### 3. File I/O
**Python**:
```python
with open(path, 'rb') as f:
    data = f.read()
```

**C#**:
```csharp
var data = await File.ReadAllBytesAsync(path);
```

### 4. Configuration
**Python** (ConfigParser):
```python
ini = ConfigParser()
ini.read_string(ini_text)
```

**C#** (Multiple options):
```csharp
// Option 1: Microsoft.Extensions.Configuration
var config = new ConfigurationBuilder()
    .AddIniFile(iniPath)
    .Build();

// Option 2: Custom INI parser
var parser = new IniParser();
var data = parser.Parse(iniText);
```

### 5. UI Data Binding
**Python** (Manual):
```python
self.namespaces_combobox.set(value)
self.namespaces_combobox["values"] = values
```

**C#** (MVVM):
```csharp
// ViewModel
public ObservableCollection<string> Namespaces { get; }
public string SelectedNamespace { get; set; }

// XAML
<ComboBox ItemsSource="{Binding Namespaces}"
          SelectedItem="{Binding SelectedNamespace}"/>
```

## Implementation Priority

1. **Phase 1** (Foundation) ✅ COMPLETE
   - Project structure
   - Core models
   - Basic UI

2. **Phase 2** (Core Engine)
   - ConfigReader
   - INI parsing
   - PatcherNamespace loading
   - Resource file handling

3. **Phase 3** (Patching Operations)
   - GFF modifications
   - 2DA modifications
   - TLK modifications
   - Script compilation

4. **Phase 4** (Installation)
   - ModInstaller
   - Backup system
   - Capsule handling
   - Progress tracking

5. **Phase 5** (Tools & Polish)
   - Uninstaller
   - Validation
   - Permission tools
   - Update system

## Testing Strategy

1. Unit tests for core logic (TSLPatcher.Core)
2. Integration tests for file operations
3. UI tests for Avalonia views
4. End-to-end tests with sample mods

## Resources

- [Avalonia Documentation](https://docs.avaloniaui.net/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [.NET Configuration](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
- Original Python source: `Libraries/PyKotor/src/pykotor/tslpatcher/`

## Notes

- Maintain functional parity with Python version
- Improve performance where possible (async I/O, parallel operations)
- Follow C# conventions and best practices
- Use modern .NET features (nullable reference types, pattern matching, etc.)

