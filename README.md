# Loop-de-loop

Loop-de-loop is a puzzle game (Slitherlink / Loopy) on a grid where numbers in cells indicate the number of filled edges around that cell, and all filled edges must form a single, continuous, non-crossing loop.

## Architecture

The project is built on **.NET 10** and **C# 14**, organized into modular SDK-style projects:

- **`LoopDeLoop.Core`** (`net10.0`): The core puzzle generation, solver engine, and undo/redo tree supporting arbitrary planar grids (Square, Square Symmetrical, Triangle, Hexagon, Octagon, Square2, Pentagon).
- **`LoopDeLoop.Web`** (`net10.0` Blazor WebAssembly): Modern web frontend running directly in the browser via WebAssembly (replacing obsolete Silverlight and Bridge.NET implementations). Features interactive SVG rendering, keyboard shortcuts, checkpoints, timer, and difficulty settings.
- **`LoopDeLoop`** (`net10.0-windows` Windows Forms): Full-featured Windows desktop application with single-player and multiplayer modes.
- **`LoopDeLoop.Tests`** (`net10.0` MSTest): Comprehensive test suite covering grid topologies, generation, solving algorithms, and serialization.

## Building and Running

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)

### Build the Solution
```bash
dotnet build LoopDeLoop.sln
```

### Run Tests
```bash
dotnet test LoopDeLoop.sln
```

### Run the Blazor Web App
```bash
dotnet run --project LoopDeLoop.Web
```
Then navigate to `http://localhost:5000` (or the URL shown in the console) to play in the browser.

### Run the Windows Desktop App
```bash
dotnet run --project LoopDeLoop
```

## Game Controls (Web App)
- **Left-Click / Drag** edge: Cycle (Empty ➔ Filled ➔ Excluded ➔ Empty)
- **Right-Click** edge: Cycle (Empty ➔ Excluded ➔ Filled ➔ Empty)
- **Click cell**: Shade cell (Yellow / Gray) to track inside/outside regions
- **Fix** (`Ctrl+F`): Save a checkpoint (current moves turn green)
- **Revert** (`Ctrl+R`): Rollback to the last checkpoint
- **Undo / Redo** (`Ctrl+Z` / `Ctrl+Y`)
not just square grids.  It is probably not bug free, but it has been manually tested quite a lot.