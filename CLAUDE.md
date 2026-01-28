# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

### Prerequisites
- .NET SDK 10.0.101 (specified in `global.json`)
- Required workloads: `dotnet workload install android ios maccatalyst wasm-tools`
- Optional (Tizen): Install via PowerShell script from Samsung/Tizen.NET

### Building
```bash
# Build and run sample app (quickest way to test changes)
cd samples/ControlCatalog.Desktop
dotnet restore
dotnet run

# Build entire solution with Nuke
nuke --target Compile --configuration Release

# Or run Nuke directly without global tool
dotnet run --project nukebuild/_build.csproj -- --configuration Debug
```

### Testing
```bash
# Run all tests with Nuke
nuke --target RunTests --configuration Release

# Run specific test project
dotnet test tests/Avalonia.Base.UnitTests

# Run integration tests (requires setup - see tests/Avalonia.IntegrationTests.Appium/readme.md)
# Windows: Install WinAppDriver, run it, then run tests
# macOS: Install Appium, bundle IntegrationTestApp, then run tests
```

### Packaging
```bash
# Create NuGet packages (includes compile + tests)
nuke --target Package --configuration Release
```

### Opening in Visual Studio
- **Avalonia.sln**: Full solution (requires all workloads)
- **Avalonia.Desktop.slnf**: Desktop-only filter (no extra workloads needed)
- Requires Visual Studio 2022 or newer
- First build may require manually building `Avalonia.Build.Tasks` project

## Architecture Overview

### Core Layers

**Avalonia.Base** - Foundation layer with no platform dependencies:
- Property system: `AvaloniaObject` → `AvaloniaProperty` with priority-based resolution via `ValueStore`
- Class hierarchy: `AvaloniaObject` → `Animatable` → `StyledElement` → `Visual` → `Layoutable` → `Interactive`
- Data binding infrastructure (CompiledBinding, ReflectionBinding)
- Styling system (selectors, styles, themes)
- Rendering primitives and composition

**Avalonia.Controls** - UI layer:
- Extends base: `Interactive` → `InputElement` → `Control`
- All UI controls (Button, TextBox, Window, etc.)
- Layout panels (Grid, StackPanel, Canvas)
- Template system (ControlTemplate, DataTemplate)

**Markup Layer**:
- **Avalonia.Markup**: Core markup abstractions
- **Avalonia.Markup.Xaml**: Runtime XAML loading, markup extensions
- **Avalonia.Build.Tasks**: Build-time XAML compilation using XamlX + Mono.Cecil

**Platform Implementations** (all reference Base, no reverse dependencies):
- **Windows**: Avalonia.Win32 (+ Win32.Automation, Win32.Interoperability)
- **macOS**: Avalonia.Native (requires Xcode for native libs)
- **Linux**: Avalonia.X11, Avalonia.FreeDesktop
- **Mobile**: Avalonia.Android, Avalonia.iOS
- **Browser**: Avalonia.Browser (WebAssembly)

**Avalonia.Skia**: Primary rendering backend implementing `IPlatformRenderInterface`

**Avalonia.Desktop**: Meta-package aggregating Win32 + X11 + Native + Skia

### Key Subsystems

**Data Binding**:
- Three types: `CompiledBinding` (strongly-typed, preferred), `ReflectionBinding` (runtime), `TemplateBinding`
- Priority system: Animation → LocalValue → TemplatedParent → StyleTrigger → Style → Inherited → Unset
- `ValueStore` manages effective values via stacked `ValueFrame`s
- Located in `src/Avalonia.Base/Data`

**Styling**:
- CSS-like selectors (type, class, pseudoclass, property, combinators)
- `Style` = selector + setters collection
- `ControlTheme` for control-scoped theming
- Applied via `ValueFrame` in property store
- Located in `src/Avalonia.Base/Styling`

**Layout**:
- Two-pass: Measure then Arrange
- `Layoutable` base class, panels override `MeasureOverride`/`ArrangeOverride`
- Invalidation propagates up visual tree

**Rendering**:
- Modern: Composition-based with separate render thread (`Compositor`, `ServerCompositor`)
- Legacy: `ImmediateRenderer` for simpler scenarios
- Platform abstraction via `IPlatformRenderInterface`
- Located in `src/Avalonia.Base/Rendering`

**XAML Compilation**:
- Build-time: `CompileAvaloniaXamlTask` → XamlX parses XAML → IL emitted via Mono.Cecil
- Runtime: `AvaloniaXamlLoader` for dynamic XAML
- Located in `src/Avalonia.Build.Tasks` and `src/Markup`

### Platform Abstraction Pattern

Interface-based (no #ifdefs in core code):
- `IWindowingPlatform`, `IPlatformRenderInterface`, `IPlatformThreadingInterface`, etc.
- Services registered via `AvaloniaLocator` at platform initialization
- Example: `Win32Platform.Initialize()` registers all Win32-specific implementations

### Assembly Dependency Flow
```
Application
  ↓
Avalonia.Desktop (package)
  ↓
├── Platform-specific (Win32/X11/Native)
├── Avalonia.Skia
└── Avalonia.Markup.Xaml
      ↓
    Avalonia.Markup
      ↓
    Avalonia.Controls
      ↓
    Avalonia.Base (foundation)
```

## Test Organization

**Unit Tests** (tests/):
- `Avalonia.Base.UnitTests`: Property system, bindings, styling
- `Avalonia.Controls.UnitTests`: Control behavior
- `Avalonia.Markup.Xaml.UnitTests`: XAML loading and compilation
- Named: `Method_Name_Should_Do_Something()`

**Render Tests**:
- `Avalonia.RenderTests`: Platform-agnostic render test definitions
- `Avalonia.Skia.RenderTests`: Skia-based execution
- Named: `Rectangle_2px_Stroke_Filled()`

**Integration Tests**:
- `Avalonia.IntegrationTests.Appium`: Cross-platform UI automation tests
- Requires WinAppDriver (Windows) or Appium (macOS)
- See tests/Avalonia.IntegrationTests.Appium/readme.md for setup

**Build Tests**:
- `BuildTests/`: Verify XAML compilation across platforms
- Not in main solution (requires local NuGet packages first)

**Headless Tests**:
- `Avalonia.Headless.XUnit.*`: XUnit integration for UI tests without platform
- `Avalonia.Headless.NUnit.*`: NUnit integration

## Contribution Guidelines

**Bug Fixes**:
- Ideally include test demonstrating the issue
- Commit pattern: failing test commit → fix commit
- Unit tests (tests/) for non-platform issues
- Integration tests for platform-specific issues
- Render tests for visual issues

**Features**:
- Always include tests where possible
- New controls should have `AutomationPeer` for accessibility
- Follow existing patterns in codebase

**Style**:
- [.NET Core coding style](https://github.com/dotnet/runtime/blob/master/docs/coding-guidelines/coding-style.md)
- ~120 char line length (soft limit)
- 100 char line limit for XML docs (hard limit)
- Public methods need XML docs
- **NO #REGIONS**
- Test naming: `Method_Name_Should_Do_Expected_Thing()`

**PR Guidelines**:
- Fill in PR template sections (discretionary, delete irrelevant)
- Link issues with `Fixes #1234`
- No unrelated formatting/whitespace changes
- Breaking changes during major release cycles require `TODOXX:` comments (XX = next major version)

**Building Avalonia.Build.Tasks**:
- If you get MSB4062 errors, manually build `Avalonia.Build.Tasks` project once
- Or build entire solution once with Nuke

## Platform-Specific Notes

**macOS**:
- Can't build full solution (only .NET Standard/.NET Core subset)
- Requires Xcode for native libraries
- Run `./build.sh CompileNative` to build native libs
- Use Rider or VS Code (Visual Studio for Mac not supported)

**Linux**:
- Same as macOS - subset only
- Use Rider or VS Code

**Browser/WASM**:
- Requires NodeJS (latest LTS from nodejs.org)

**Windows**:
- Can build everything including .NET Framework samples
- Requires .NET Framework 4.7+ SDK

## Important File Locations

- **Property System**: `src/Avalonia.Base/AvaloniaObject.cs`, `src/Avalonia.Base/AvaloniaProperty.cs`
- **Value Store**: `src/Avalonia.Base/PropertyStore/`
- **Binding**: `src/Avalonia.Base/Data/`
- **Styling**: `src/Avalonia.Base/Styling/`
- **Controls**: `src/Avalonia.Controls/`
- **XAML Build**: `src/Avalonia.Build.Tasks/`
- **Rendering**: `src/Avalonia.Base/Rendering/`, `src/Avalonia.Skia/`
- **Platform**: `src/Windows/`, `src/Avalonia.X11/`, `src/Avalonia.Native/`

## This Repository

This is a public repository. Use `gh` command to read issues and PRs.

Main branch for PRs: `master`
