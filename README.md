# Loop-de-loop

Loop-de-loop is a puzzle game (Slitherlink / Loopy) played on planar grids where numbers in cells indicate how many edges of that cell belong to the loop. All filled edges must connect together into a single, continuous, non-intersecting closed loop.

The project features a solver and generator that work across arbitrary planar mesh topologies (not just square grids), shared across both a web client and a Windows desktop client.

## Grid Topologies Supported

- **Square** & **Square Symmetrical**
- **Triangle**
- **Hexagon** (standard, variation 2, variation 3)
- **Octagon** (Cairo / Octagon-Square tessellation)
- **Square 2**
- **Pentagon** (Cairo pentagonal tiling)

## Architecture

The project is built on **.NET 10** and **C# 14**, organized into modular SDK-style projects:

- **`LoopDeLoop.Core`** (`net10.0`): Core puzzle generation and solver engine supporting arbitrary planar graphs and depth-limited deductive inference, along with an undo/redo action tree.
- **`LoopDeLoop.Web`** (`net10.0` Blazor WebAssembly): Modern web frontend running client-side in the browser via WebAssembly (AOT-compiled in Release). Features SVG rendering, keyboard shortcuts, checkpoints, cell coloring, interactive edge dragging, and live generation progress.
- **`LoopDeLoop`** (`net10.0-windows` Windows Forms): Classic Windows desktop application featuring single-player puzzle solving and competitive multiplayer lobby/game modes.
- **`LoopDeLoop.Tests`** (`net10.0` MSTest): Automated test suite covering grid topology creation, loop generation, solver verification, and text serialization.

## Building and Running

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- Optional for Web AOT: `dotnet workload install wasm-tools`

### Build the Solution
```bash
dotnet build LoopDeLoop.sln
```

### Run Tests
```bash
dotnet test LoopDeLoop.sln
```

### Run the Web Application (Blazor)
```bash
dotnet run --project LoopDeLoop.Web
```
Then navigate to `http://localhost:5186` (or the URL displayed in the console) to play in the browser.

### Publish for Production (with WebAssembly AOT)
```bash
dotnet publish LoopDeLoop.Web/LoopDeLoop.Web.csproj -c Release -o publish
```
The output in `publish/wwwroot` can be hosted as static files on any web server (e.g., Apache, Nginx, or GitHub Pages).

### Run the Windows Desktop App
```bash
dotnet run --project LoopDeLoop
```

## Game Controls & Features (Web App)

- **Left-Click / Drag edge**: Cycle forward (Empty ➔ Line ➔ Cross ➔ Empty).
- **Right-Click edge**: Cycle reverse (Empty ➔ Cross ➔ Line ➔ Empty).
- **Shift + Click**: Reverses cycling order for left and right click.
- **Shade Cells Option**: When enabled, clicking a cell toggles background shading (Yellow / Gray) to help track interior vs. exterior regions. When disabled, clicks inside a cell automatically route to the nearest edge.
- **Disallow False Moves Option**: Restricts manual edge marks to moves that do not violate immediate local constraints (vertex degree > 2 or cell clue excess).
- **Checkpoints**:
  - **Fix** (`Ctrl+F`): Lock and color current edges green as a known-good checkpoint. Fixed edges cannot be accidentally modified.
  - **Unfix** (`Ctrl+U`): Unlock fixed edges so they can be changed again.
  - **Revert** (`Ctrl+R`): Discard subsequent moves and roll back to the saved checkpoint.
- **History**: **Undo** (`Ctrl+Z`) and **Redo** (`Ctrl+Y`).