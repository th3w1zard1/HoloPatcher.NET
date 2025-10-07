# HoloPatcher.NET - Detailed TODO List

## ✅ Completed (October 7, 2025)

- [x] ConfigReader - Basic structure and Settings parsing
- [x] NamespaceReader - Full implementation
- [x] Installation class - Game detection
- [x] ModInstaller - Core orchestration
- [x] GUI MainWindowViewModel - Integration with backend
- [x] Backup system - Directory management
- [x] Game enum - K1/K2 distinction
- [x] Progress tracking - UI integration
- [x] Error handling - Comprehensive logging

---

## 🔴 Critical Path (Must Complete for Basic Functionality)

### 1. Complete ConfigReader Section Parsers

**Priority**: Critical  
**Estimated Time**: 3-4 days

#### InstallList Parser

- [ ] Parse file installation instructions
- [ ] Handle !SourceFolder directive
- [ ] Create InstallFile objects
- [ ] Support destination paths (Override, Modules)

#### 2DAList Parser

- [ ] Parse 2DA modification sections
- [ ] Handle ChangeRow, AddRow, CopyRow operations
- [ ] Parse RowValue types (Constant, 2DAMemory, TLKMemory, High, etc.)
- [ ] Support Target types (ROW_INDEX, ROW_LABEL, LABEL_COLUMN)
- [ ] Parse AddColumn operations

#### GFFList Parser

- [ ] Parse GFF modification sections
- [ ] Handle ModifyField operations
- [ ] Parse FieldValue types (Constant, ListIndex, 2DAMemory, TLKMemory)
- [ ] Support LocalizedString modifications
- [ ] Parse struct paths and field types

#### TLKList Parser

- [ ] Parse TLK modification sections
- [ ] Handle Append and Replace operations
- [ ] Support StrRef token storage

#### SSFList Parser

- [ ] Parse SSF modification sections
- [ ] Handle sound index modifications

#### CompileList & HACKList Parsers

- [ ] Parse NSS script files for compilation
- [ ] Parse NCS binary patching instructions
- [ ] Handle token replacement (#2DAMEMORY#, #StrRef#)

---

### 2. Implement Resource Installation System

**Priority**: Critical  
**Estimated Time**: 5-7 days

#### File Operations

- [ ] Copy files to Override folder
- [ ] Copy files to Modules folder
- [ ] Create backup before overwriting
- [ ] Handle file conflicts
- [ ] Implement OverrideType (ignore, warn, rename)

#### Capsule Handling

- [ ] Load ERF/RIM/MOD files
- [ ] Extract resources from capsules
- [ ] Modify resources in capsules
- [ ] Write modified capsules back to disk
- [ ] Support building .mod from .rim files

#### Resource Lookup

- [ ] Check if resource exists in game
- [ ] Determine whether to patch or replace
- [ ] Load source files from mod directory
- [ ] Load destination files from game directory

---

### 3. Implement Actual Patching Logic

**Priority**: Critical  
**Estimated Time**: 4-5 days

#### Apply Modifications

- [ ] Apply GFF field modifications
- [ ] Apply 2DA row/column modifications
- [ ] Apply TLK string additions
- [ ] Apply SSF sound modifications
- [ ] Apply NSS token replacements
- [ ] Apply NCS binary patches

#### Memory Management

- [ ] Store 2DAMEMORY tokens during patching
- [ ] Store StrRef tokens during patching
- [ ] Resolve memory references in later patches

---

## 🟡 Important (Needed for Full Functionality)

### 4. Advanced GFF Operations

**Priority**: High  
**Estimated Time**: 3-5 days

- [ ] Implement AddFieldGFF class
  - [ ] Field type to value conversion
  - [ ] Struct path resolution
  - [ ] Value modifier chains

- [ ] Implement AddStructToListGFF class
  - [ ] List navigation
  - [ ] Struct creation
  - [ ] Template copying
  - [ ] Index token storage

- [ ] Implement Memory2DAModifierGFF class
  - [ ] !FieldPath support
  - [ ] Path-based memory references
  - [ ] Field pointer dereferencing

---

### 5. ModUninstaller

**Priority**: Medium  
**Estimated Time**: 2-3 days

- [ ] Backup directory location
- [ ] File restoration from backup
- [ ] Backup validation
- [ ] Partial rollback support
- [ ] Backup cleanup options

---

### 6. Command-Line Arguments

**Priority**: Medium  
**Estimated Time**: 1-2 days

- [ ] Parse CLI arguments in Program.cs
- [ ] Support --game-dir flag
- [ ] Support --tslpatchdata flag
- [ ] Support --namespace-option-index flag
- [ ] Support --install flag (headless mode)
- [ ] Support --uninstall flag
- [ ] Support --validate flag
- [ ] Support --console flag

---

## 🟢 Nice to Have (Future Enhancements)

### 7. Enhanced Installation Features

**Priority**: Low  
**Estimated Time**: 2-3 days

- [ ] Validate INI file syntax
- [ ] Show namespace description dialogs
- [ ] Fix file/folder permissions
- [ ] Fix iOS case sensitivity issues
- [ ] Check for updates
- [ ] Download updates automatically

---

### 8. Testing & Quality Assurance

**Priority**: Medium  
**Estimated Time**: Ongoing

- [ ] Unit tests for ConfigReader
- [ ] Unit tests for NamespaceReader
- [ ] Unit tests for ModInstaller
- [ ] Integration tests with real mods
- [ ] Performance testing
- [ ] Memory leak testing
- [ ] Error recovery testing

---

### 9. Documentation

**Priority**: Low  
**Estimated Time**: 2-3 days

- [ ] API documentation
- [ ] User manual
- [ ] Developer guide
- [ ] Contribution guidelines
- [ ] Example mods for testing

---

### 10. Platform Support

**Priority**: Low  
**Estimated Time**: 3-4 days

- [ ] macOS testing and fixes
- [ ] Linux testing and fixes
- [ ] Path separator handling
- [ ] Platform-specific game detection

---

## 📋 Development Phases

### Phase 1: Core Functionality (Weeks 1-2)

1. Complete ConfigReader parsers
2. Implement resource installation
3. Implement patching logic
**Goal**: Basic mod installation works

### Phase 2: Advanced Features (Weeks 3-4)

1. Advanced GFF operations
2. ModUninstaller
3. Command-line arguments
**Goal**: Feature parity with Python HoloPatcher

### Phase 3: Polish & Testing (Week 5)

1. Comprehensive testing
2. Bug fixes
3. Documentation
4. Performance optimization
**Goal**: Production-ready release

---

## 🎯 Success Criteria

### Minimum Viable Product (MVP)

- ✅ GUI loads and displays
- ✅ Can browse for mods
- ✅ Can select game directory
- ⏳ Can parse changes.ini
- ⏳ Can install simple mods
- ⏳ Creates backups
- ✅ Shows progress
- ✅ Logs operations

### Full Feature Parity

- All TSLPatcher features
- Python HoloPatcher features
- Cross-platform support
- Robust error handling
- Comprehensive logging
- Automated testing

---

**Last Updated**: October 7, 2025  
**Maintainer**: Development Team
