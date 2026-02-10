# Interactive TUI Command - Implementation Plan

## Executive Summary

Add an interactive Terminal.Gui-based TUI command to `atc-opc-ua` CLI that provides a terminal-based OPC UA client for browsing server address spaces, monitoring variable values in real-time, reading node details, and writing values -- inspired by the [opcilloscope](https://github.com/SquareWaveSystems/opcilloscope) project but built on our own `Atc.Opc.Ua` library and following our established `atc-dsc` CLI interactive pattern.

---

## 1. Source Analysis

### 1.1 Opcilloscope (3rd Party Reference)

**Location:** `C:\Dev\Code\3rdParty\opcilloscope`

**Stack:** Terminal.Gui v2.0.0 + OPCFoundation.NetStandard.Opc.Ua.Client 1.5.378.65

**Architecture:**
- Standalone TUI app (no Spectre.Console CLI framework)
- `Program.cs` -> `Application.Init()` -> `MainWindow` (extends `Toplevel`)
- OPC UA layer: `ConnectionManager` -> `OpcUaClientWrapper` + `NodeBrowser` + `SubscriptionManager`
- Configuration layer: `ConfigurationService` + JSON serialization

**Key Features:**
| Feature | Description | Priority for us |
|---------|-------------|----------------|
| Address Space Browser | TreeView with lazy-loading child nodes | **Must-have** |
| Monitored Variables | TableView with real-time subscription updates (batched at 50ms) | **Must-have** |
| Node Details | Panel showing node attributes (NodeId, DataType, Value, Access, etc.) | **Must-have** |
| Log View | Scrollable log panel for connection/error messages | **Should-have** |
| Connect Dialog | Dialog for entering server URL + publishing interval | **Must-have** |
| Write Value Dialog | Dialog for writing values to writable nodes | **Should-have** |
| Scope/Oscilloscope | BrailleCanvas-based real-time signal visualization (up to 5 signals) | **Should-have** |
| Trend Plot | Time-based plotting dialog | **Should-have** |
| CSV Recording | Record monitored values to CSV files | **Should-have** |
| Themes (Dark/Light) | ThemeManager with configurable color schemes | **Should-have** |
| Config Save/Load | Persist connection + subscriptions to JSON files | **Should-have** |
| Keybinding System | Lazygit-inspired context-aware keybindings | **Should-have** |
| Focus Management | Panel focus tracking with border highlighting | **Should-have** |

**Key Patterns Worth Adopting:**
- `UiThread.Run()` helper for marshalling to UI thread (Terminal.Gui's `Application.Invoke`)
- Batched table updates via `ConcurrentDictionary` + timer (50ms interval) to avoid excessive redraws
- Lazy-loading tree with `DelegateTreeBuilder<T>` and async child loading
- Event-driven architecture: `ConnectionManager.ValueChanged` -> `MonitoredVariablesView.UpdateVariable`
- `FireAndForget()` extension for safe async-void patterns

### 1.2 atc-dsc-configurations CLI (Our Pattern)

**Location:** `C:\Dev\Code\atc-net\atc-dsc-configurations\src\Atc.Dsc.Configurations.Cli`

**Stack:** Terminal.Gui 2.0.0-develop.5027 + Atc.Console.Spectre 3.0.16

**Architecture Pattern (we follow this):**
```
Program.cs
  -> CommandAppFactory.CreateWithRootCommand<InteractiveCommand>(serviceCollection)
  -> app.ConfigureCommands()  // registers sub-commands (list, show, test, apply)
  -> app.RunAsync(args)       // no args = interactive, with args = CLI sub-command

InteractiveCommand : AsyncCommand
  -> IInteractiveRunner.RunAsync()

IInteractiveRunner (interface)
  -> TerminalGuiRunner (implementation)
     -> Application.Create().Init()
     -> new MainWindow(app, ...dependencies...)
     -> app.Run(mainWindow)

MainWindow : Window
  -> Two-panel layout (FrameView left + FrameView right)
  -> Keyboard shortcuts via app.Keyboard.KeyDown
  -> Status bar label
```

**Key Design Decisions:**
1. `InteractiveCommand` is the **root/default command** (no args = interactive mode)
2. `IInteractiveRunner` interface decouples Terminal.Gui from the rest of the codebase
3. DI container injects all dependencies into the runner
4. `Application.Create().Init()` is the newer Terminal.Gui v2 initialization pattern
5. `CancellationToken` flows from command to runner for graceful shutdown
6. Vim-style navigation (j/k/h/l) alongside standard keys

### 1.3 atc-opc-ua CLI (Current State)

**Location:** `C:\Dev\Code\atc-net\atc-opc-ua\src\Atc.Opc.Ua.CLI`

**Stack:** Atc.Console.Spectre 2.0.562 + Atc 2.0.562 (target: net9.0)

**Current Commands:**
- `testconnection` - Test server connectivity
- `node read object` - Read node objects
- `node read variable single/multi` - Read variables
- `node read datatype single/multi` - Read DataType definitions
- `node write variable single` - Write a variable
- `node scan` - Scan address space
- `method execute` - Execute methods

**Current Program.cs pattern:**
```csharp
var app = CommandAppFactory.Create(serviceCollection);  // non-generic
app.ConfigureCommands();
return app.RunAsync(args);
```

**Core Library Capabilities (`IOpcUaClient`):**
- `ConnectAsync(Uri, CancellationToken)` / `ConnectAsync(Uri, username, password, CancellationToken)`
- `DisconnectAsync(CancellationToken)`
- `IsConnected()`
- `ReadNodeVariableAsync` / `ReadNodeVariablesAsync`
- `ReadNodeObjectAsync`
- `WriteNodeAsync` / `WriteNodesAsync`
- `ExecuteMethodAsync`
- `ReadEnumDataTypeAsync` / `ReadEnumDataTypesAsync`
- Public `Session` property (`ISession?`) -- **critical: gives us access to OPC Foundation session for subscriptions**

**Gap Analysis:**
| Capability | Current | Needed | Where to Add |
|-----------|---------|--------|-------------|
| Connection management | Yes | Yes (reuse) | -- |
| Address space browsing | Via `ReadNodeObjectAsync` + Scanner | Need lazy node-by-node browsing | **Core library** (`IOpcUaNodeBrowser`) |
| Value reading | Yes (one-shot) | Need real-time subscriptions | **Core library** (`IOpcUaClient` extension) |
| Value writing | Yes | Yes (reuse) | -- |
| Subscription/Monitoring | **NO** | **Must add** | **Core library** (`IOpcUaClient` + contracts) |
| Node attribute reading | Partial (via ReadNodeVariable) | Need full attribute reading | **Core library** (`IOpcUaNodeBrowser`) |

---

## 2. Architecture Decision

### 2.1 TUI Library: Terminal.Gui v2.0.0

**Consensus:** Use Terminal.Gui v2.0.0 (stable release, same as opcilloscope).

**Arguments For:**
- Both reference projects use Terminal.Gui v2
- Rich widget set: TreeView, TableView, Dialog, MenuBar, StatusBar, FrameView
- Cross-platform terminal rendering
- Active maintenance, MIT licensed

**Arguments Against (considered and rejected):**
- Spectre.Console Live/Layout: Too limited for multi-panel interactive apps with real-time updates
- Could use the develop branch (like atc-dsc): Decided against -- prefer stable v2.0.0 for reliability

### 2.2 Interactive Command Pattern: Follow atc-dsc

**Consensus:** Follow the atc-dsc pattern exactly.

```
InteractiveCommand (root/default command)
  -> IOpcUaInteractiveRunner
     -> TerminalGuiRunner
        -> MainWindow
```

**Key difference from opcilloscope:** Opcilloscope is a standalone TUI app. We integrate into an existing Spectre.Console CLI with the interactive mode as the default command, preserving all existing CLI sub-commands.

### 2.3 OPC UA Subscription Layer: Extend the Core Library

**Consensus:** Add subscription/monitoring support directly to the `Atc.Opc.Ua` core library, then build a thin TUI adapter on top.

**Why extend the core library (not TUI-only)?**
- Subscription is a **fundamental OPC UA capability** -- it belongs alongside connect/read/write/scan
- `ISession? Session { get; }` will be added to `IOpcUaClient` interface for clean access (no casting to concrete class)
- Makes subscriptions **reusable** for other consumers (sample app, future CLI commands, other projects)
- Follows the same `(bool Succeeded, T?, string?)` tuple pattern for consistency
- Can be **unit tested independently** of Terminal.Gui
- Aligns with how opcilloscope separates OPC UA logic from UI logic

**Architecture:**
```
Atc.Opc.Ua (core library - extended)
  IOpcUaClient (add subscription methods)
    ├── CreateSubscriptionAsync() -> returns subscription handle
    ├── SubscribeToNodeAsync(nodeId) -> adds monitored item, returns handle
    ├── UnsubscribeFromNodeAsync(handle) -> removes monitored item
    ├── UnsubscribeAllAsync() -> clears all subscriptions
    └── Event: NodeValueChanged (fires on subscription notifications)

  New contracts (in Atc.Opc.Ua.Contracts):
    ├── MonitoredNodeValue (NodeId, DisplayName, Value, Timestamp, StatusCode, DataType)
    ├── SubscriptionOptions (PublishingIntervalMs, SamplingIntervalMs, QueueSize)
    └── NodeBrowseResult (NodeId, DisplayName, NodeClass, DataType, HasChildren)

  New service (in Atc.Opc.Ua):
    IOpcUaNodeBrowser / OpcUaNodeBrowser
    ├── BrowseChildrenAsync(parentNodeId) -> lazy address space browsing
    ├── ReadNodeAttributesAsync(nodeId) -> full attribute read
    └── GetRootNodeId()

Atc.Opc.Ua.CLI (TUI layer - thin adapter)
  Tui/Services/OpcUaTuiService
    ├── Wraps IOpcUaClient + IOpcUaNodeBrowser
    ├── Manages TUI-specific state (BrowsedNode tree model, MonitoredVariable display model)
    ├── Marshals events to UI thread
    └── Translates between core contracts and TUI view models
```

### 2.4 Package Upgrades Required

| Package | Current | Target | Reason |
|---------|---------|--------|--------|
| `Atc.Console.Spectre` | 2.0.562 | 3.0.16+ | Need `CommandAppFactory.CreateWithRootCommand<T>` |
| `Atc` | 2.0.562 | latest matching | Keep in sync with Atc.Console.Spectre |
| `Terminal.Gui` | (new) | 2.0.0 | TUI framework |
| `OPCFoundation.NetStandard.Opc.Ua` | [1.5.377.21] | [1.5.377.21] (keep pinned) | **Do not upgrade** - pinned version |
| Target Framework | net9.0 | net9.0 (keep) | No change needed |

---

## 3. UI Layout Design

```
+--[ atc-opc-ua - Interactive OPC UA Client ]---------------------------+
| File | Connection | View | Help                                       |
+--[ Address Space ]----------+--[ Monitored Variables ]----------------+
|  Root                       |  Name    | NodeId   | Value   | Status  |
|  +- Objects                 |  Temp    | ns=2;... | 23.5    | Good    |
|  |  +- Server               |  Speed   | ns=2;... | 1500    | Good    |
|  |  +- Demo                 |  Voltage | ns=2;... | 220.1   | Good    |
|  |     +- Dynamic           |                                         |
|  |        +- Scalar          |                                         |
|  |           +- Float [var]  |                                         |
|  +- Types                   |                                         |
|  +- Views                   |                                         |
+-----------------------------+-----------------------------------------+
+--[ Node Details ]-----------------------------------------------------+
|  NodeId: ns=2;s=Demo.Dynamic.Scalar.Float  DataType: Float           |
|  Access: ReadWrite  Value: 23.456  Status: Good                       |
+-----------------------------------------------------------------------+
+--[ Log ]--------------------------------------------------------------+
|  [INFO] Connected to opc.tcp://localhost:48010                        |
|  [INFO] Subscribed to Float                                           |
+-----------------------------------------------------------------------+
| [c] Connect  [d] Disconnect  [Tab] Switch  [?] Help  [q] Quit        |
+-----------------------------------------------------------------------+
```

**Panel Layout:**
- **Address Space** (left, 35%): TreeView with lazy-loading OPC UA node browser
- **Monitored Variables** (right, 65%): TableView with real-time subscription values
- **Node Details** (bottom, 5 rows): Shows attributes of currently selected node
- **Log** (bottom, fills remaining): Connection/operation log messages
- **Status Bar** (bottom row): Context-aware keyboard shortcuts

---

## 4. Detailed TODO List

### Phase 0: Package Updates & Infrastructure

- [x] **0.1** Upgrade `Atc.Console.Spectre` from 2.0.562 to 3.0.18 in `Atc.Opc.Ua.CLI.csproj`
- [x] **0.2** Upgrade `Atc` package to 3.0.18 in both CLI and core library csproj files
- [x] **0.3** Add `Terminal.Gui` 2.0.0-develop.5027 package reference to `Atc.Opc.Ua.CLI.csproj`
- [x] **0.4** Update `Microsoft.Extensions.*` packages to 10.0.1 in all csproj files
- [x] **0.4b** Upgrade TargetFramework from net9.0 to net10.0 across all projects (required by Terminal.Gui 2.x and Atc 3.x)
- [x] **0.4c** Update CI pipelines (build.yml, release-please.yml) to use dotnet 10.0.x
- [x] **0.5** Verify the project builds and all 56 existing tests pass after upgrades
- [x] **0.6** Update `Program.cs` to use `CommandAppFactory.CreateWithRootCommand<InteractiveCommand>` pattern (replacing `CommandAppFactory.Create`)
- [x] **0.6b** Add `CancellationToken` parameter to all command `ExecuteAsync` overrides (Spectre.Console.Cli 3.x breaking change)
- [x] **0.7** Ensure existing CLI commands still work (non-breaking: if args provided, sub-commands run; no args = interactive)

### Phase 1: Core Library - Subscription & Browse Support

Add subscription/monitoring and lazy browsing capabilities to `Atc.Opc.Ua` and `Atc.Opc.Ua.Contracts`.

#### 1A: New Contracts (`Atc.Opc.Ua.Contracts`)

- [x] **1A.1** Create `MonitoredNodeValue` model
  ```csharp
  public class MonitoredNodeValue
  {
      public string NodeId { get; set; }
      public string DisplayName { get; set; }
      public string? Value { get; set; }
      public DateTime? Timestamp { get; set; }
      public uint StatusCode { get; set; }
      public string? DataTypeName { get; set; }
      public byte? AccessLevel { get; set; }
      public bool IsGood => StatusCode == 0;
  }
  ```
- [x] **1A.2** Create `SubscriptionOptions` model
  ```csharp
  public class SubscriptionOptions
  {
      public int PublishingIntervalMs { get; set; } = 250;
      public int SamplingIntervalMs { get; set; } = 250;
      public uint QueueSize { get; set; } = 10;
      public bool DiscardOldest { get; set; } = true;
  }
  ```
- [x] **1A.3** Create `NodeBrowseResult` model
  ```csharp
  public class NodeBrowseResult
  {
      public string NodeId { get; set; }
      public string DisplayName { get; set; }
      public string BrowseName { get; set; }
      public NodeClassType NodeClass { get; set; }
      public string? DataTypeName { get; set; }
      public bool HasChildren { get; set; }
  }
  ```
- [x] **1A.4** Create `NodeAttributeSet` model (full attribute read result)
- [x] **1A.5** Add unit tests for new contracts (18 tests, all passing)

#### 1B: Extend IOpcUaClient with Subscription Support (`Atc.Opc.Ua`)

- [x] **1B.1** Add `ISession? Session { get; }` property to `IOpcUaClient` interface (promotes existing concrete-class property to the interface for clean access)
- [x] **1B.2** Add subscription methods to `IOpcUaClient` interface
  ```csharp
  // Session access (promoted to interface)
  ISession? Session { get; }

  // Subscription lifecycle
  Task<(bool Succeeded, string? ErrorMessage)> CreateSubscriptionAsync(
      SubscriptionOptions? options = null, CancellationToken cancellationToken = default);

  Task<(bool Succeeded, string? ErrorMessage)> RemoveSubscriptionAsync(
      CancellationToken cancellationToken = default);

  // Node monitoring
  Task<(bool Succeeded, uint MonitoredItemHandle, string? ErrorMessage)> SubscribeToNodeAsync(
      string nodeId, string? displayName = null, CancellationToken cancellationToken = default);

  Task<(bool Succeeded, string? ErrorMessage)> UnsubscribeFromNodeAsync(
      uint monitoredItemHandle, CancellationToken cancellationToken = default);

  Task<(bool Succeeded, string? ErrorMessage)> UnsubscribeAllAsync(
      CancellationToken cancellationToken = default);

  // Subscription event
  event EventHandler<MonitoredNodeValue>? NodeValueChanged;
  ```
- [x] **1B.3** Implement subscription support in `OpcUaClient` (new partial class `OpcUaClientSubscription.cs`)
  - Create OPC UA `Subscription` on the existing `Session`
  - Manage `MonitoredItem` instances with O(1) lookup (dictionary by handle)
  - Process `MonitoredItemNotification` callbacks and raise `NodeValueChanged`
  - Handle subscription cleanup on disconnect/dispose
  - Handle subscription restoration on reconnect (reattach or recreate)
- [x] **1B.4** Add unit tests for subscription functionality
- [ ] **1B.5** Test subscription against real OPC UA server (integration test, deferred to Phase 1D sample app)

#### 1C: New Node Browser Service (`Atc.Opc.Ua`)

- [x] **1C.1** Create `IOpcUaNodeBrowser` interface
  ```csharp
  public interface IOpcUaNodeBrowser
  {
      Task<(bool Succeeded, IList<NodeBrowseResult>? Children, string? ErrorMessage)> BrowseChildrenAsync(
          IOpcUaClient client, string parentNodeId, CancellationToken cancellationToken = default);

      Task<(bool Succeeded, NodeAttributeSet? Attributes, string? ErrorMessage)> ReadNodeAttributesAsync(
          IOpcUaClient client, string nodeId, CancellationToken cancellationToken = default);
  }
  ```
- [x] **1C.2** Implement `OpcUaNodeBrowser` service
  - Lazy node-by-node browsing via `Session.Browse()` with `HierarchicalReferences`
  - Optimistic `HasChildren` for Objects/Variables (same pattern as opcilloscope)
  - Parallel data type name resolution
  - Full attribute reading (class-specific attributes)
- [x] **1C.3** Register `IOpcUaNodeBrowser` / `OpcUaNodeBrowser` in DI
- [ ] **1C.4** Add unit tests for node browser (deferred - requires ISession mocking)

### Phase 1D: Subscription Sample App

New sample app to validate and demonstrate the subscription/monitoring core library features. Follows the pattern from the existing `Atc.Opc.Ua.Sample` (minimal console app, direct instantiation, console logging, hardcoded constants).

- [x] **1D.1** Create `sample/Atc.Opc.Ua.Subscription.Sample/Atc.Opc.Ua.Subscription.Sample.csproj`
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <OutputType>Exe</OutputType>
      <TargetFramework>net9.0</TargetFramework>
      <IsPackable>false</IsPackable>
    </PropertyGroup>
    <ItemGroup>
      <PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.10" />
      <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="9.0.10" />
    </ItemGroup>
    <ItemGroup>
      <ProjectReference Include="..\..\src\Atc.Opc.Ua\Atc.Opc.Ua.csproj" />
    </ItemGroup>
  </Project>
  ```
- [x] **1D.2** Create `GlobalUsings.cs` (same pattern as existing sample)
- [x] **1D.3** Create `Program.cs` demonstrating:
  1. **DI + logging setup** (same pattern: `ServiceCollection` + console logger at Trace level)
  2. **Connect** to OPC UA server via `OpcUaClient`
  3. **Create subscription** via `client.CreateSubscriptionAsync(options)`
  4. **Subscribe to multiple nodes** via `client.SubscribeToNodeAsync(nodeId, displayName)`
  5. **Handle `NodeValueChanged` event** - log each value change to console
  6. **Run for a duration** (e.g., 30 seconds with `PeriodicTimer` or `Task.Delay`) while values stream in
  7. **Unsubscribe** from individual nodes via `client.UnsubscribeFromNodeAsync(handle)`
  8. **Unsubscribe all** via `client.UnsubscribeAllAsync()`
  9. **Remove subscription** via `client.RemoveSubscriptionAsync()`
  10. **Disconnect** and dispose
  ```csharp
  // Key demonstration pattern:
  using var client = new OpcUaClient(clientLogger);
  await client.ConnectAsync(ServerUri, UserName!, Password!, cts.Token);

  client.NodeValueChanged += (sender, value) =>
  {
      clientLogger.LogInformation(
          "Value changed: {DisplayName} ({NodeId}) = {Value} [{Status}] at {Timestamp}",
          value.DisplayName,
          value.NodeId,
          value.Value,
          value.IsGood ? "Good" : $"0x{value.StatusCode:X8}",
          value.Timestamp);
  };

  await client.CreateSubscriptionAsync(new SubscriptionOptions
  {
      PublishingIntervalMs = 250,
  });

  foreach (var nodeId in NodeIds)
  {
      var (succeeded, handle, errorMessage) = await client.SubscribeToNodeAsync(nodeId);
      // ...
  }

  // Let subscriptions run for 30 seconds
  await Task.Delay(TimeSpan.FromSeconds(30), cts.Token);

  await client.UnsubscribeAllAsync();
  await client.RemoveSubscriptionAsync();
  ```
- [x] **1D.4** Also demonstrate `IOpcUaNodeBrowser` usage - DemoBrowseAsync browses Objects folder, DemoReadAttributesAsync reads first child's full attributes
- [x] **1D.5** Add the new project to the solution file (added to Atc.Opc.Ua.slnx)
- [ ] **1D.6** Verify the sample compiles and runs against a real OPC UA server

### Phase 2: TUI Infrastructure

- [x] **2.1** Create `Tui/` folder under `Atc.Opc.Ua.CLI`
- [x] **2.2** Create `IOpcUaInteractiveRunner` interface (mirrors atc-dsc's `IInteractiveRunner`)
- [x] **2.3** Create `TerminalGuiRunner` implementing `IOpcUaInteractiveRunner`
- [x] **2.4** Create `InteractiveCommand : AsyncCommand` (root/default command)
- [x] **2.5** Register `IOpcUaInteractiveRunner` / `TerminalGuiRunner` in DI (Program.cs)
- [x] **2.6** Update `CommandAppExtensions.ConfigureCommands` to work with `CommandApp<InteractiveCommand>`
- [x] **2.7** Add non-interactive fallback when `Console.IsInputRedirected || Console.IsOutputRedirected`
- [x] **2.8** Create thin `Tui/Services/OpcUaTuiService` adapter
  - Wraps `IOpcUaClient` + `IOpcUaNodeBrowser` for the TUI
  - Manages TUI-specific view models (BrowsedNode tree, MonitoredVariable display)
  - Marshals `NodeValueChanged` events to UI thread
  - No direct OPC UA SDK access -- delegates everything to the core library

### Phase 3: Main Window & Layout

- [x] **3.1** Create `Tui/MainWindow.cs` extending `Window`
  - Four-panel layout: AddressSpace (left 35%), MonitoredVariables (right 65%), NodeDetails (bottom 5 rows), Log (fill)
  - Status label with context-aware keyboard shortcuts
  - Key bindings: c/d/Tab/?/q/Delete via namespace-prefixed Terminal.Gui.Input.Key
  - ConfirmQuit and Help dialogs (following atc-dsc pattern)
  - RunGuardedAsync for safe fire-and-forget async
- [x] **3.2** Create `Tui/Views/AddressSpaceView.cs` extending `FrameView`
  - TreeView<BrowsedNode> with DelegateTreeBuilder for lazy loading
  - Events: NodeSelected, SubscribeRequested
  - Empty state label when disconnected
- [x] **3.3** Create `Tui/Views/MonitoredVariablesView.cs` extending `FrameView`
  - TableView backed by DataTable with DataTableSource
  - Columns: Name, NodeId, Value, Status, Timestamp
  - Batched updates (50ms timer via ConcurrentDictionary + Application.AddTimeout)
  - Delete/Backspace to unsubscribe via HandleDeleteKey
- [x] **3.4** Create `Tui/Views/NodeDetailsView.cs` extending `FrameView`
  - Async attribute reading with formatted display (NodeId, Class, DataType, Value, Access, Status)
- [x] **3.5** Create `Tui/Views/LogView.cs` extending `FrameView`
  - ListView-backed scrollable log with ObservableCollection
  - Auto-scroll to latest, max 500 entries
  - AddInfo/AddWarning/AddError helpers
- [x] **3.6** Create `Tui/Models/BrowsedNode.cs`
  - TUI-specific model with lazy-loading state (ChildrenLoaded, Children, Parent)
  - Factory method FromBrowseResult for mapping from core contracts

### Phase 4: Dialogs & Interactions

- [x] **4.1** Create `Tui/Dialogs/ConnectDialog.cs` - Server URL, Username, Password fields with remember-last-endpoint via static fields, validation, OK/Cancel buttons with TGUI001-compliant Accepting handlers
- [x] **4.2** Create `Tui/Dialogs/WriteValueDialog.cs` - Displays node info (name, nodeId, dataType, current value), new value text field, Write/Cancel buttons
- [x] **4.3** Help dialog implemented inline in MainWindow.ShowHelp() - Keyboard shortcuts reference with categorized sections (Navigation, Connection, Actions, General)
- [x] **4.4** Wire up keyboard shortcuts in MainWindow - `c` Connect, `d` Disconnect, `w` Write, `r` Refresh, `Tab` Switch focus, `Enter` Subscribe, `Delete`/`Backspace` Unsubscribe, `?` Help, `q` Quit with confirmation

### Phase 5: Integration & Polish

- [x] **5.1** Wire up all events between MainWindow, Views, and OpcUaTuiService - NodeValueChanged -> MonitoredVariablesView.UpdateVariable, UnsubscribeRequested -> UnsubscribeFromNodeAsync, NodeSelected -> NodeDetailsView, SubscribeRequested -> SubscribeToNodeAsync
- [x] **5.2** Implement connection status indicator in status bar - Dynamic UpdateStatusLabel() showing "Connected: url" or "Disconnected" state
- [x] **5.3** ~~Connecting animation~~ Replaced with "Connecting to..." log message (sufficient for v1)
- [x] **5.4** Handle disconnection gracefully - Clear all views, reset status bar, show empty states with helpful prompts
- [x] **5.5** Handle errors/exceptions with user-friendly messages - RunGuardedAsync catches all exceptions and logs to LogView, service operations show specific error messages
- [x] **5.6** ~~UiThread helper~~ Not needed - app.Invoke() and OpcUaTuiService UI-thread marshalling via EventHandler pattern serve this purpose
- [x] **5.7** Dispose tuiService properly in TerminalGuiRunner (using var)
- [ ] **5.8** Test interactive mode against a real OPC UA server (manual verification)
- [ ] **5.9** Test that all existing CLI commands still work correctly (manual verification)

### Phase 6: Advanced Features

- [x] **6.1** Configuration save/load - TuiConfigurationService persists recent connections to JSON in %APPDATA%/atc-opc-ua/tui-config.json
- [x] **6.2** Recent connections list - ConnectDialog shows recent connections ListView, selecting populates fields
- [ ] **6.3** Theme support (dark/light) with `ThemeManager` (deferred to future release)
- [x] **6.4** Scope/Oscilloscope view - BrailleCanvas-based real-time signal visualization with custom Unicode braille renderer (2x4 sub-pixel resolution), Bresenham line drawing, auto-scaling Y axis, ring buffer, legend with live values, up to 5 signals
- [x] **6.5** Trend plot dialog - TrendPlotDialog with braille canvas plotting, min/max/avg statistics, duration display
- [x] **6.6** CSV recording - CsvRecorder with thread-safe writes, proper CSV escaping, start/stop via `s` key, output to Documents folder with timestamp filename

---

## 5. New File Structure

```
sample/Atc.Opc.Ua.Subscription.Sample/       (NEW - Phase 1D)
  Atc.Opc.Ua.Subscription.Sample.csproj       (NEW)
  GlobalUsings.cs                              (NEW)
  Program.cs                                   (NEW)

src/Atc.Opc.Ua.CLI/
  Commands/
    InteractiveCommand.cs              (NEW - root/default command)
    ... (existing commands unchanged)
  Extensions/
    CommandAppExtensions.cs            (MODIFIED - adapt to CommandApp<InteractiveCommand>)
  Tui/
    IOpcUaInteractiveRunner.cs         (NEW)
    TerminalGuiRunner.cs               (NEW)
    MainWindow.cs                      (NEW)
    UiThread.cs                        (NEW - helper)
    Views/
      AddressSpaceView.cs              (NEW)
      MonitoredVariablesView.cs        (NEW)
      NodeDetailsView.cs               (NEW)
      LogView.cs                       (NEW)
      ScopeView.cs                     (NEW - Phase 6)
    Dialogs/
      ConnectDialog.cs                 (NEW)
      WriteValueDialog.cs              (NEW)
      HelpDialog.cs                    (NEW)
      TrendPlotDialog.cs               (NEW - Phase 6)
    Models/
      BrowsedNode.cs                   (NEW)
      MonitoredVariable.cs             (NEW)
      NodeAttributeSet.cs              (NEW)
      TuiConnectionState.cs            (NEW)
    Services/
      IOpcUaTuiService.cs              (NEW)
      OpcUaTuiService.cs               (NEW)
      CsvRecorder.cs                   (NEW - Phase 6)
      ThemeManager.cs                  (NEW - Phase 6)
  Program.cs                           (MODIFIED)
  Atc.Opc.Ua.CLI.csproj               (MODIFIED - new packages)
```

---

## 6. Key Code Patterns

### 6.1 Program.cs (Modified)

```csharp
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var consoleLoggerConfiguration = new ConsoleLoggerConfiguration();
        configuration.GetSection("ConsoleLogger").Bind(consoleLoggerConfiguration);
        ProgramCsHelper.SetMinimumLogLevelIfNeeded(args, consoleLoggerConfiguration);

        var serviceCollection = ServiceCollectionFactory.Create(consoleLoggerConfiguration);
        serviceCollection.AddTransient<IOpcUaClient, OpcUaClient>();
        serviceCollection.AddTransient<IOpcUaScanner, OpcUaScanner>();
        serviceCollection.AddSingleton<IOpcUaInteractiveRunner, TerminalGuiRunner>();
        serviceCollection.AddSingleton<IOpcUaTuiService, OpcUaTuiService>();

        var app = CommandAppFactory.CreateWithRootCommand<InteractiveCommand>(serviceCollection);
        app.ConfigureCommands();

        if (IsNonInteractiveTerminal(args))
        {
            args = [CommandConstants.ArgumentShortHelp];
        }

        return await app.RunAsync(args);
    }

    private static bool IsNonInteractiveTerminal(string[] args)
        => args.Length == 0 &&
           (Console.IsInputRedirected || Console.IsOutputRedirected);
}
```

### 6.2 InteractiveCommand Pattern

```csharp
public sealed class InteractiveCommand(
    IOpcUaInteractiveRunner runner)
    : AsyncCommand
{
    public override Task<int> ExecuteAsync(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        ConsoleHelper.WriteHeader();
        return runner.RunAsync(cancellationToken);
    }
}
```

### 6.3 TerminalGuiRunner Pattern

```csharp
public sealed class TerminalGuiRunner(IOpcUaTuiService tuiService) : IOpcUaInteractiveRunner
{
    public Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        using var app = Application.Create().Init();
        using var registration = cancellationToken.Register(() => app.Invoke(() => app.RequestStop()));

        var mainWindow = new MainWindow(app, tuiService);
        app.Run(mainWindow);

        return Task.FromResult(0);
    }
}
```

### 6.4 Batched Table Updates (from opcilloscope)

```csharp
// In MonitoredVariablesView
private readonly ConcurrentDictionary<uint, MonitoredVariable> _pendingUpdates = new();
private const int UpdateBatchIntervalMs = 50;

public void UpdateVariable(MonitoredVariable variable)
{
    _pendingUpdates[variable.ClientHandle] = variable;
    EnsureUpdateTimerRunning();
}

private bool ProcessPendingUpdates()
{
    // Snapshot pending, apply to DataTable rows, single _tableView.Update()
    // Return true to continue timer, false to stop
}
```

### 6.5 Subscription via Core Library (No Casting Needed)

```csharp
// In OpcUaTuiService - clean access via IOpcUaClient interface
public async Task SubscribeAsync(string nodeId, string displayName)
{
    var (succeeded, errorMessage) = await client.CreateSubscriptionAsync(
        new SubscriptionOptions { PublishingIntervalMs = 250 });

    if (!succeeded)
    {
        log.LogError("Failed to create subscription: {Error}", errorMessage);
        return;
    }

    var (subOk, handle, subError) = await client.SubscribeToNodeAsync(nodeId, displayName);
    if (!subOk)
    {
        log.LogError("Failed to subscribe to {NodeId}: {Error}", nodeId, subError);
    }
}

// Wire up value change events
client.NodeValueChanged += (sender, value) =>
{
    // Marshal to UI thread
    Application.Invoke(() => monitoredVariablesView.UpdateVariable(value));
};
```

---

## 7. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Atc.Console.Spectre 3.x has breaking API changes | Build failures in existing commands | Review changelog, test all commands after upgrade |
| Terminal.Gui v2.0.0 vs develop.5027 API differences | API mismatch with atc-dsc patterns | Use stable v2.0.0 APIs; check which atc-dsc patterns use develop-only features |
| ~~`OpcUaClient.Session` cast from `IOpcUaClient`~~ | ~~Tight coupling~~ | **Resolved**: `ISession? Session` added to `IOpcUaClient` interface |
| High-frequency OPC UA updates overwhelming UI | Terminal flicker, high CPU | Batch updates at 50ms intervals (opcilloscope pattern) |
| Thread safety: OPC UA callbacks on background threads | Race conditions in UI updates | Use `Application.Invoke()` / `UiThread.Run()` for all UI mutations |
| Terminal.Gui + Spectre.Console console conflicts | Rendering glitches | Terminal.Gui takes full control during interactive mode; Spectre output only before/after |
| Subscription loss on connection drop | Lost monitoring state | Implement subscription restoration (reattach or recreate pattern from opcilloscope) |

---

## 8. Open Questions & Decisions

1. **Atc.Console.Spectre 3.x compatibility**: Does `CreateWithRootCommand<T>` exist in 3.0.16, or do we need a newer version? The atc-dsc project uses it successfully.

2. ~~**Session access pattern**~~: **DECIDED** - Yes, add `ISession? Session { get; }` to `IOpcUaClient` interface. This avoids casting and makes Session access a first-class contract.

3. **Terminal.Gui version**: Should we use stable 2.0.0 (like opcilloscope) or 2.0.0-develop.5027 (like atc-dsc)? The develop branch has a different `Application.Create().Init()` API. Stable uses `Application.Init()`. Need to verify which API our target version supports.

4. ~~**Scope for v1**~~: **DECIDED** - All features included in v1: browse, monitor, details, write, log, scope/oscilloscope, trend plot, CSV recording, themes, config save/load.

5. **net9.0 vs net10.0**: The atc-dsc project targets net10.0 while atc-opc-ua targets net9.0. Should we upgrade the target framework as part of this work?

6. ~~**OPC Foundation SDK version**~~: **DECIDED** - Keep pinned at `[1.5.377.21]`. Do not upgrade.

---

## 9. Dependencies Between Tasks

```
Phase 0 (Package Upgrades)
   |
   v
Phase 1A/1B/1C (Core Library: Subscriptions & Browse)
   |
   v
Phase 1D (Subscription Sample App) ── validate core library works
   |
   v
Phase 2 (TUI Infrastructure) ----+
   |                              |
   v                              v
Phase 3 (Main Window & Layout) <--+
   |
   v
Phase 4 (Dialogs & Interactions)
   |
   v
Phase 5 (Integration & Polish)
   |
   v
Phase 6 (Advanced: Scope, Trends, CSV, Themes)
```

Phase 0 must complete first (package upgrades). Phases 1A-1C extend the core library. Phase 1D validates the core library features with a sample app before building the TUI. Phase 2 builds TUI scaffolding. Phase 3 depends on both 1 and 2. Phases 4-6 are sequential after Phase 3.
