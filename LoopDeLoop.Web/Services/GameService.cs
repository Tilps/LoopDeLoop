using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LoopDeLoop;

namespace LoopDeLoop.Web.Services
{
    public class GameService : IDisposable
    {
        public Mesh? CurrentMesh { get; private set; }
        public UndoTree UndoTree { get; private set; } = new();
        public HashSet<int> MarkedEdges { get; private set; } = [];

        public MeshType CurrentType { get; set; } = MeshType.Square;
        public string SizeText { get; set; } = "10x10";
        public int Difficulty { get; set; } = 1; // Easy

        public bool DisallowFalseMove { get; set; } = false;
        public bool ShowCellColors { get; set; } = false;
        public bool ShowEdgeColors
        {
            get => field;
            set
            {
                field = value;
                if (!value) PendingGroupEdgeIndex = null;
            }
        } = false;

        // Phase 2 Settings (persisted via UserPreferences, hidden in Settings modal)
        public int AutoMoveLevel
        {
            get => field;
            set => field = Math.Clamp(value, 0, 4);
        } = 0; // 0 = Off, 1..3 = Depth 1..3, 4 = Full Cascade

        public bool AutoMoveConsiderMultipleLoops { get; set; } = true;
        public bool AutoMoveUseIC { get; set; } = false;
        public bool AutoStart { get; set; } = false;
        public MouseWheelMode WheelMode { get; set; } = MouseWheelMode.Zoom;
        public bool ShowClock { get; set; } = true;

        public double GenerateLengthFraction
        {
            get => field;
            set => field = double.IsNaN(value) ? 0.5 : Math.Clamp(value, 0.25, 0.75);
        } = 0.5;

        public double GenerateBoringFraction
        {
            get => field;
            set => field = double.IsNaN(value) ? 0.01 : Math.Clamp(value, 0.0, 0.20);
        } = 0.01;

        public GeneratorSolverMode SolverModeOverride { get; set; } = GeneratorSolverMode.Auto;

        public bool IsGenerating { get; private set; }
        public int PrunedProgress { get; private set; }
        public int TotalCells { get; private set; }

        public DateTime? StartTime { get; private set; }
        public TimeSpan ElapsedTime => StartTime.HasValue ? DateTime.UtcNow - StartTime.Value : TimeSpan.Zero;
        public bool IsSolved { get; private set; }
        public bool WasAutoSolved { get; private set; }

        public int? InvalidEdgeIndex { get; private set; }
        public int? PendingGroupEdgeIndex { get; private set; }
        public PuzzleAnalysisResult? CurrentAnalysis { get; private set; }
        public int? HintEdgeIndex { get; private set; }
        public EdgeState HintTargetState { get; private set; } = EdgeState.Filled;
        public bool HintIsApplied { get; private set; }
        public string? HintMessage { get; private set; }

        public event Action? OnStateChanged;

        public bool CanUndo => UndoTree.CanUndo;
        public bool CanRedo => UndoTree.CanRedo;

        public void NotifyChanged()
        {
            OnStateChanged?.Invoke();
        }

        private Mesh? generatingMesh;
        private CancellationTokenSource? generationCts;
        public DateTime? GenerationStartTime { get; private set; }

        public double GenerationElapsedSeconds => GenerationStartTime.HasValue
            ? (DateTime.UtcNow - GenerationStartTime.Value).TotalSeconds
            : 0;

        private static CancellationToken ResetTokenSource(ref CancellationTokenSource? cts)
        {
            var previous = cts;
            cts = new CancellationTokenSource();
            if (previous != null)
            {
                previous.Cancel();
                previous.Dispose();
            }
            return cts.Token;
        }

        private void ClearTransientBoardState()
        {
            preVictoryMarkedEdges = null;
            preVictoryUndoMark = null;
            preVictoryUndoTree = null;
            InvalidEdgeIndex = null;
            PendingGroupEdgeIndex = null;
            CurrentAnalysis = null;
            DismissVisualSolveInternal();
            ClearHint();
        }

        public void CancelGeneration()
        {
            if (generatingMesh != null)
            {
                generatingMesh.AbortPrune = true;
            }
            generationCts?.Cancel();
            IsGenerating = false;
            NotifyChanged();
        }

        public async Task GenerateNewPuzzleAsync()
        {
            if (IsGenerating) return;

            var token = ResetTokenSource(ref generationCts);

            DismissVisualSolveInternal();
            IsGenerating = true;
            GenerationStartTime = DateTime.UtcNow;
            IsSolved = false;
            PrunedProgress = 0;
            TotalCells = 0;
            NotifyChanged();

            // Allow UI to render the loading overlay before computation begins
            await Task.Delay(20);

            if (!PuzzleHelper.ParseSize(SizeText, CurrentType, out int width, out int height))
            {
                PuzzleHelper.GetDefaultSize(CurrentType, out width, out height);
                SizeText = $"{width}x{height}";
            }

            try
            {
                Mesh mesh = PuzzleHelper.MakeMesh(width, height, CurrentType, Difficulty);
                ApplyGeneratorOverrides(mesh);
                generatingMesh = mesh;
                TotalCells = mesh.Cells.Count;
                NotifyChanged();

                var progress = new Progress<int>(p =>
                {
                    PrunedProgress = p;
                    NotifyChanged();
                });

                await mesh.GenerateAsync(progress, token);

                if (!token.IsCancellationRequested && !mesh.AbortPrune)
                {
                    CurrentMesh = mesh;
                    MarkedEdges.Clear();
                    UndoTree = new UndoTree();
                    ApplyAutoStartIfEnabled(mesh);
                    StartTime = DateTime.UtcNow;
                    IsSolved = false;
                    WasAutoSolved = false;
                    ClearTransientBoardState();
                    CheckSolution();
                }
            }
            catch (OperationCanceledException)
            {
                // Gracefully cancelled by user
            }
            finally
            {
                generatingMesh = null;
                generationCts?.Dispose();
                generationCts = null;
                GenerationStartTime = null;
                IsGenerating = false;
                NotifyChanged();
            }
        }

        private int GetEffectiveAutoMoveDepth(Mesh mesh)
        {
            return AutoMoveLevel switch
            {
                1 => 1,
                2 => 2,
                3 => 3,
                >= 4 => mesh.Edges.Count + 1,
                _ => 0
            };
        }

        public void ConfigureMeshForInteractiveMove(Mesh mesh)
        {
            mesh.ConsiderMultipleLoops = AutoMoveConsiderMultipleLoops;
            mesh.ConsiderIntersectCellInteractsAsSimple = AutoMoveUseIC;
            mesh.UseColoring = false;
            mesh.UseCellPairs = false;
            mesh.UseCellPairsTopLevel = false;
            mesh.UseEdgeRestricts = false;
            mesh.UseCellColoring = false;
            mesh.UseDerivedColoring = false;
            mesh.UseMerging = false;
            mesh.ColoringCheats = false;
        }

        public void ApplyGeneratorOverrides(Mesh mesh)
        {
            mesh.GenerateLengthFraction = GenerateLengthFraction;
            mesh.GenerateBoringFraction = GenerateBoringFraction;
            if (SolverModeOverride == GeneratorSolverMode.Iterative)
                mesh.SolverMethod = SolverMethod.Iterative;
            else if (SolverModeOverride == GeneratorSolverMode.Recursive)
                mesh.SolverMethod = SolverMethod.Recursive;
        }

        public void ApplyAutoStartIfEnabled(Mesh mesh)
        {
            if (!AutoStart) return;
            ConfigureMeshForInteractiveMove(mesh);
            mesh.PerformStart([]);
        }

        public UserPreferences ExportPreferences() => new()
        {
            DisallowFalseMove = DisallowFalseMove,
            ShowCellColors = ShowCellColors,
            ShowEdgeColors = ShowEdgeColors,
            AutoMoveLevel = AutoMoveLevel,
            AutoMoveConsiderMultipleLoops = AutoMoveConsiderMultipleLoops,
            AutoMoveUseIC = AutoMoveUseIC,
            AutoStart = AutoStart,
            WheelMode = WheelMode,
            ShowClock = ShowClock,
            GenerateLengthFraction = GenerateLengthFraction,
            GenerateBoringFraction = GenerateBoringFraction,
            SolverModeOverride = SolverModeOverride
        };

        public void ApplyPreferences(UserPreferences prefs)
        {
            DisallowFalseMove = prefs.DisallowFalseMove;
            ShowCellColors = prefs.ShowCellColors;
            ShowEdgeColors = prefs.ShowEdgeColors;
            AutoMoveLevel = prefs.AutoMoveLevel;
            AutoMoveConsiderMultipleLoops = prefs.AutoMoveConsiderMultipleLoops;
            AutoMoveUseIC = prefs.AutoMoveUseIC;
            AutoStart = prefs.AutoStart;
            WheelMode = Enum.IsDefined(prefs.WheelMode) ? prefs.WheelMode : MouseWheelMode.Zoom;
            ShowClock = prefs.ShowClock;
            GenerateLengthFraction = prefs.GenerateLengthFraction;
            GenerateBoringFraction = prefs.GenerateBoringFraction;
            SolverModeOverride = Enum.IsDefined(prefs.SolverModeOverride) ? prefs.SolverModeOverride : GeneratorSolverMode.Auto;
            NotifyChanged();
        }

        public void ResetPreferencesToDefaults()
        {
            ApplyPreferences(new UserPreferences());
        }

        public bool HandleEdgeGroupingClick(int edgeIndex, bool sameLoop)
        {
            if (CurrentMesh == null || IsGenerating || edgeIndex < 0 || edgeIndex >= CurrentMesh.Edges.Count)
                return false;

            bool dismissedVisual = DismissVisualSolveInternal();
            if (!ShowEdgeColors || MarkedEdges.Contains(edgeIndex))
            {
                if (dismissedVisual) NotifyChanged();
                return false;
            }

            if (PendingGroupEdgeIndex is not int firstEdge)
            {
                PendingGroupEdgeIndex = edgeIndex;
                NotifyChanged();
                return true;
            }

            PendingGroupEdgeIndex = null;
            if (firstEdge == edgeIndex)
            {
                NotifyChanged();
                return true;
            }

            var action = new ColorJoinAction(CurrentMesh, firstEdge, edgeIndex, sameLoop);
            if (UndoTree.Do(action))
            {
                InvalidEdgeIndex = null;
                NotifyChanged();
                return true;
            }

            FlashInvalidEdge(edgeIndex);
            NotifyChanged();
            return false;
        }

        public void ClearPendingEdgeGroup()
        {
            if (PendingGroupEdgeIndex.HasValue)
            {
                PendingGroupEdgeIndex = null;
                NotifyChanged();
            }
        }

        public bool CanExportSquareFormats => PuzzleFormatHelper.CanExportSquareFormats(CurrentMesh);

        public string ExportLoopText() => CurrentMesh != null ? PuzzleFormatHelper.ExportLoopText(CurrentMesh) : string.Empty;

        public string? ExportLoopyDescriptor() => CurrentMesh != null ? PuzzleFormatHelper.ExportLoopyDescriptor(CurrentMesh) : null;

        public string? ExportLoopySave() => CurrentMesh != null ? PuzzleFormatHelper.ExportLoopySave(CurrentMesh) : null;

        public string? ExportSquareAscii() => CurrentMesh != null ? PuzzleFormatHelper.ExportSquareAscii(CurrentMesh) : null;

        public bool ImportPuzzle(string rawInput, out string detectedFormat)
        {
            detectedFormat = string.Empty;
            if (IsGenerating || !PuzzleFormatHelper.TryParsePuzzle(rawInput, out Mesh? mesh, out detectedFormat, out int w, out int h) || mesh == null)
                return false;

            CurrentMesh = mesh;
            CurrentType = mesh.MeshType;
            if (w > 0 && h > 0)
            {
                SizeText = $"{w}x{h}";
            }
            TotalCells = mesh.Cells.Count;
            PrunedProgress = 0;
            IsGenerating = false;
            UndoTree = new UndoTree();
            MarkedEdges.Clear();
            StartTime = DateTime.UtcNow;
            IsSolved = false;
            WasAutoSolved = false;
            ClearTransientBoardState();

            CheckSolution();
            NotifyChanged();
            return true;
        }

        public PuzzleAnalysisResult? AnalyzeCurrentPuzzle()
        {
            if (CurrentMesh == null || IsGenerating) return null;

            int clueCount = 0;
            foreach (var cell in CurrentMesh.Cells)
            {
                if (cell.TargetCount >= 0) clueCount++;
            }
            int totalCells = CurrentMesh.Cells.Count;
            int edgeCount = CurrentMesh.Edges.Count;

            Mesh solver = CreateFullSolverMesh(CurrentMesh);
            solver.Clear();

            SolveState fullResult = solver.TrySolve();
            if (fullResult == SolveState.MultipleSolutions)
            {
                CurrentAnalysis = new PuzzleAnalysisResult("Multiple Solutions", "Non-unique", 0, clueCount, totalCells, edgeCount);
                NotifyChanged();
                return CurrentAnalysis;
            }
            if (fullResult != SolveState.Solved)
            {
                CurrentAnalysis = new PuzzleAnalysisResult("No Solution", "Unsolvable", 0, clueCount, totalCells, edgeCount);
                NotifyChanged();
                return CurrentAnalysis;
            }

            // RecursiveTrySolve pushes a base frame before checking trials, so branch count is Length - 1.
            int branchDepth = Math.Max(0, (solver.DepthPatern?.Length ?? 0) - 1);
            string tier = $"Recursive Search (F, Depth {branchDepth})";
            int? lookaheadDepth = null;

            // Check lightest deductive tiers on demand, matching WinForms RatingThreadProc
            solver.SetRatingCodeOptions("I");
            if (solver.TrySolve() == SolveState.Solved)
            {
                int min = -1;
                int max = solver.Edges.Count;
                while (min < max - 1)
                {
                    int mid = (min + max) / 2;
                    solver.IterativeSolverDepth = mid;
                    if (solver.TrySolve() == SolveState.Solved)
                        max = mid;
                    else
                        min = mid;
                }
                lookaheadDepth = max;
                tier = max == 0 ? "Direct Deductions (I0)" : $"Iterative Lookahead (I{max})";
            }
            else
            {
                solver.SetRatingCodeOptions("IC");
                if (solver.TrySolve() == SolveState.Solved)
                {
                    tier = "Cell-Edge Interactions (IC)";
                }
                else
                {
                    solver.SetRatingCodeOptions("ID");
                    if (solver.TrySolve() == SolveState.Solved)
                    {
                        tier = "Edge Coloring (ID)";
                    }
                    else
                    {
                        solver.SetRatingCodeOptions("ICR");
                        if (solver.TrySolve() == SolveState.Solved)
                        {
                            tier = "Edge Restrictions (ICR)";
                        }
                    }
                }
            }

            CurrentAnalysis = new PuzzleAnalysisResult("Unique Solution", tier, branchDepth, clueCount, totalCells, edgeCount, lookaheadDepth);
            NotifyChanged();
            return CurrentAnalysis;
        }

        public void ToggleEdge(int edgeIndex, bool isAlternative)
        {
            if (CurrentMesh == null || edgeIndex < 0 || edgeIndex >= CurrentMesh.Edges.Count)
                return;

            PendingGroupEdgeIndex = null;
            bool dismissedVisual = DismissVisualSolveInternal();

            if (MarkedEdges.Contains(edgeIndex))
            {
                if (dismissedVisual) NotifyChanged();
                return;
            }

            var current = CurrentMesh.Edges[edgeIndex].State;
            var next = !isAlternative
                ? (current == EdgeState.Empty ? EdgeState.Filled : (current == EdgeState.Filled ? EdgeState.Excluded : EdgeState.Empty))
                : (current == EdgeState.Empty ? EdgeState.Excluded : (current == EdgeState.Excluded ? EdgeState.Filled : EdgeState.Empty));

            int autoMove = GetEffectiveAutoMoveDepth(CurrentMesh);
            if (autoMove > 0)
                ConfigureMeshForInteractiveMove(CurrentMesh);

            var action = new PuzzleSetEdgeStateAction(CurrentMesh, edgeIndex, next, DisallowFalseMove, autoMove);
            if (UndoTree.Do(action))
            {
                InvalidEdgeIndex = null;
                HintEdgeIndex = null;
                CheckSolution();
                NotifyChanged();
            }
            else
            {
                if (dismissedVisual) NotifyChanged();
                if (DisallowFalseMove)
                {
                    FlashInvalidEdge(edgeIndex);
                }
            }
        }

        public void SetEdgeDirect(int edgeIndex, EdgeState targetState)
        {
            if (CurrentMesh == null || edgeIndex < 0 || edgeIndex >= CurrentMesh.Edges.Count)
                return;

            bool dismissedVisual = DismissVisualSolveInternal();

            if (MarkedEdges.Contains(edgeIndex) || CurrentMesh.Edges[edgeIndex].State == targetState)
            {
                if (dismissedVisual) NotifyChanged();
                return;
            }

            int autoMove = GetEffectiveAutoMoveDepth(CurrentMesh);
            if (autoMove > 0)
                ConfigureMeshForInteractiveMove(CurrentMesh);

            var action = new PuzzleSetEdgeStateAction(CurrentMesh, edgeIndex, targetState, DisallowFalseMove, autoMove);
            if (UndoTree.Do(action))
            {
                InvalidEdgeIndex = null;
                HintEdgeIndex = null;
                CheckSolution();
                NotifyChanged();
            }
            else
            {
                if (dismissedVisual) NotifyChanged();
                if (DisallowFalseMove)
                {
                    FlashInvalidEdge(edgeIndex);
                }
            }
        }

        public void ToggleCellColor(int cellIndex, bool isAlternative)
        {
            if (CurrentMesh == null || cellIndex < 0 || cellIndex >= CurrentMesh.Cells.Count)
                return;

            bool dismissedVisual = DismissVisualSolveInternal();
            var action = new PuzzleCellColorAction(CurrentMesh, cellIndex, isAlternative);
            if (UndoTree.Do(action))
            {
                NotifyChanged();
            }
            else if (dismissedVisual)
            {
                NotifyChanged();
            }
        }

        public void Undo()
        {
            bool dismissedVisual = DismissVisualSolveInternal();
            if (UndoTree.CanUndo)
            {
                UndoTree.Undo();
                WasAutoSolved = false;
                CheckSolution();
                NotifyChanged();
            }
            else if (dismissedVisual)
            {
                NotifyChanged();
            }
        }

        public bool UndoAndForget()
        {
            bool dismissedVisual = DismissVisualSolveInternal();
            if (UndoTree.CanUndo && UndoTree.UndoAndForget())
            {
                WasAutoSolved = false;
                CheckSolution();
                NotifyChanged();
                return true;
            }
            if (dismissedVisual) NotifyChanged();
            return false;
        }

        public void Redo()
        {
            bool dismissedVisual = DismissVisualSolveInternal();
            if (UndoTree.CanRedo)
            {
                UndoTree.Redo();
                WasAutoSolved = UndoTree.CurrentAction is PuzzleBatchSolveAction;
                CheckSolution();
                NotifyChanged();
            }
            else if (dismissedVisual)
            {
                NotifyChanged();
            }
        }

        public void Fix()
        {
            if (CurrentMesh == null) return;
            DismissVisualSolveInternal();
            MarkedEdges.Clear();
            for (int i = 0; i < CurrentMesh.Edges.Count; i++)
            {
                if (CurrentMesh.Edges[i].State != EdgeState.Empty)
                    MarkedEdges.Add(i);
            }
            UndoTree.Mark();
            NotifyChanged();
        }

        public void Unfix()
        {
            DismissVisualSolveInternal();
            MarkedEdges.Clear();
            UndoTree.ClearMark();
            CheckSolution();
            NotifyChanged();
        }

        public void RevertToFix()
        {
            DismissVisualSolveInternal();
            UndoTree.RevertToMark();
            CheckSolution();
            NotifyChanged();
        }

        // Checkpoint state captured just before the victory auto-Fix, so it can be
        // restored if the puzzle becomes unsolved again (e.g. via Undo).
        private int[]? preVictoryMarkedEdges;
        private object? preVictoryUndoMark;
        private UndoTree? preVictoryUndoTree;

        public void CheckSolution()
        {
            if (CurrentMesh == null) return;
            bool currentlySolved = PuzzleHelper.CheckIsSolved(CurrentMesh);
            if (currentlySolved && !IsSolved)
            {
                IsSolved = true;
                preVictoryMarkedEdges = MarkedEdges.ToArray();
                preVictoryUndoMark = UndoTree.ClearMark();
                UndoTree.SetMarkedDirect(preVictoryUndoMark);
                preVictoryUndoTree = UndoTree;
                Fix(); // Auto-fix solution upon victory
            }
            else if (!currentlySolved && IsSolved)
            {
                IsSolved = false;
                WasAutoSolved = false;
                if (preVictoryUndoTree != null && ReferenceEquals(preVictoryUndoTree, UndoTree) && preVictoryMarkedEdges != null)
                {
                    MarkedEdges.Clear();
                    foreach (int m in preVictoryMarkedEdges)
                        MarkedEdges.Add(m);
                    UndoTree.SetMarkedDirect(preVictoryUndoMark);
                }
                preVictoryMarkedEdges = null;
                preVictoryUndoMark = null;
                preVictoryUndoTree = null;
            }
        }

        public SavedGameState? ExportState()
        {
            if (CurrentMesh == null) return null;

            int[] edgeStates = new int[CurrentMesh.Edges.Count];
            for (int i = 0; i < CurrentMesh.Edges.Count; i++)
            {
                edgeStates[i] = (int)CurrentMesh.Edges[i].State;
            }

            int[] cellColors = new int[CurrentMesh.Cells.Count];
            for (int i = 0; i < CurrentMesh.Cells.Count; i++)
            {
                cellColors[i] = CurrentMesh.Cells[i].Color;
            }

            PuzzleHelper.ParseSize(SizeText, CurrentType, out int w, out int h);
            string puzzleEncoded = PuzzleCodec.Encode(CurrentMesh, w, h);

            return new SavedGameState
            {
                Puzzle = puzzleEncoded,
                EdgeStates = edgeStates,
                CellColors = cellColors,
                MarkedEdges = MarkedEdges.ToArray(),
                ElapsedSeconds = ElapsedTime.TotalSeconds,
                IsSolved = IsSolved,
                WasAutoSolved = WasAutoSolved,
                DisallowFalseMove = DisallowFalseMove,
                ShowCellColors = ShowCellColors,
                Type = CurrentType.ToString(),
                Size = SizeText,
                Difficulty = Difficulty
            };
        }

        private static readonly char[] LineSeparators = ['\r', '\n'];

        public bool RestoreState(SavedGameState state)
        {
            if (string.IsNullOrEmpty(state.Puzzle)) return false;

            Mesh? mesh;
            if (PuzzleCodec.TryDecode(state.Puzzle, out var decodedMesh, out int _, out int _, out var _))
            {
                mesh = decodedMesh;
            }
            else
            {
                var lines = state.Puzzle.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries);
                mesh = new Mesh(0, 0, MeshType.Square);
                if (!mesh.LoadFromText(lines)) return false;
            }

            if (mesh == null) return false;

            CurrentMesh = mesh;
            CurrentType = mesh.MeshType;
            if (!string.IsNullOrEmpty(state.Size)) SizeText = state.Size;
            Difficulty = state.Difficulty;
            DisallowFalseMove = state.DisallowFalseMove;
            ShowCellColors = state.ShowCellColors;
            TotalCells = mesh.Cells.Count;
            PrunedProgress = 0;
            IsGenerating = false;
            UndoTree = new UndoTree();
            MarkedEdges.Clear();

            if (state.EdgeStates != null && state.EdgeStates.Length == mesh.Edges.Count)
            {
                mesh.Clear();
                for (int i = 0; i < state.EdgeStates.Length; i++)
                {
                    var st = (EdgeState)state.EdgeStates[i];
                    if (st != EdgeState.Empty)
                        new SetAction(mesh, i, st).Perform();
                }
            }

            if (state.CellColors != null && state.CellColors.Length == mesh.Cells.Count)
            {
                for (int i = 0; i < state.CellColors.Length; i++)
                {
                    mesh.Cells[i].Color = state.CellColors[i];
                }
            }

            if (state.MarkedEdges != null)
            {
                foreach (int m in state.MarkedEdges)
                {
                    if (m >= 0 && m < mesh.Edges.Count)
                        MarkedEdges.Add(m);
                }
            }

            StartTime = state.ElapsedSeconds > 0
                ? DateTime.UtcNow.AddSeconds(-state.ElapsedSeconds)
                : DateTime.UtcNow;

            IsSolved = state.IsSolved;
            WasAutoSolved = state.IsSolved && state.WasAutoSolved;
            ClearTransientBoardState();
            CheckSolution();
            NotifyChanged();
            return true;
        }

        public bool LoadPuzzleText(string puzzleText)
        {
            if (string.IsNullOrEmpty(puzzleText)) return false;

            Mesh? mesh;
            if (PuzzleCodec.TryDecode(puzzleText, out var decodedMesh, out int w, out int h, out var _))
            {
                mesh = decodedMesh;
                SizeText = $"{w}x{h}";
            }
            else
            {
                var lines = puzzleText.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries);
                mesh = new Mesh(0, 0, MeshType.Square);
                if (!mesh.LoadFromText(lines)) return false;
            }

            if (mesh == null) return false;

            CurrentMesh = mesh;
            CurrentType = mesh.MeshType;
            TotalCells = mesh.Cells.Count;
            PrunedProgress = 0;
            IsGenerating = false;
            UndoTree = new UndoTree();
            MarkedEdges.Clear();
            StartTime = DateTime.UtcNow;
            IsSolved = false;
            WasAutoSolved = false;
            ClearTransientBoardState();

            CheckSolution();
            NotifyChanged();
            return true;
        }

        private CancellationTokenSource? invalidFlashCts;
        private CancellationTokenSource? hintFlashCts;
        private CancellationTokenSource? hintMessageCts;

        private async Task ClearAfterDelayAsync(int delayMs, CancellationToken token, Action clearAction)
        {
            try
            {
                await Task.Delay(delayMs, token);
                if (!token.IsCancellationRequested)
                {
                    clearAction();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when superseded or cleared early
            }
        }

        public void FlashInvalidEdge(int edgeIndex)
        {
            var token = ResetTokenSource(ref invalidFlashCts);

            InvalidEdgeIndex = edgeIndex;
            NotifyChanged();

            _ = ClearAfterDelayAsync(600, token, () =>
            {
                if (InvalidEdgeIndex == edgeIndex)
                {
                    InvalidEdgeIndex = null;
                    NotifyChanged();
                }
            });
        }

        public void ClearHint()
        {
            hintFlashCts?.Cancel();
            hintMessageCts?.Cancel();
            HintEdgeIndex = null;
            HintMessage = null;
            NotifyChanged();
        }

        public void ResetCurrentPuzzle()
        {
            if (CurrentMesh == null || IsGenerating) return;

            CurrentMesh.Clear();
            MarkedEdges.Clear();
            UndoTree = new UndoTree();
            ApplyAutoStartIfEnabled(CurrentMesh);
            StartTime = DateTime.UtcNow;
            IsSolved = false;
            WasAutoSolved = false;
            ClearTransientBoardState();
            CheckSolution();
            NotifyChanged();
        }

        private static Mesh CreateFullSolverMesh(Mesh source)
        {
            Mesh solver = new Mesh(source);
            solver.ConsiderMultipleLoops = true;
            solver.UseIntersectCellInteractsInSolver = false;
            solver.UseColoring = true;
            solver.UseEdgeRestricts = true;
            solver.UseCellColoring = true;
            solver.SolverMethod = SolverMethod.Recursive;
            solver.ContaminateFullSolver = true;
            solver.ColoringCheats = true;
            solver.UseDerivedColoring = true;
            solver.UseMerging = true;
            solver.UseCellPairsTopLevel = true;
            solver.UseCellPairs = false;
            solver.IterativeRecMaxDepth = 1;
            return solver;
        }

        private const int MaxVisualSolveFrames = 1500;
        private readonly List<VisualSolveStep> visualSolveSteps = [];
        private Mesh? visualSolveSolutionMesh;

        public IReadOnlyList<VisualSolveStep> VisualSolveSteps => visualSolveSteps;
        public bool IsVisualSolving => visualSolveSteps.Count > 0;
        public int VisualSolveStepIndex { get; private set; }
        public int VisualSolveTotalSteps => visualSolveSteps.Count;
        public VisualSolveStep? CurrentVisualSolveStep => IsVisualSolving && VisualSolveStepIndex >= 0 && VisualSolveStepIndex < visualSolveSteps.Count
            ? visualSolveSteps[VisualSolveStepIndex]
            : null;

        public EdgeState GetVisualSolveEdgeState(int edgeIndex)
        {
            if (CurrentVisualSolveStep is { } step && edgeIndex >= 0 && edgeIndex < step.EdgeStates.Length)
                return step.EdgeStates[edgeIndex];
            if (CurrentMesh != null && edgeIndex >= 0 && edgeIndex < CurrentMesh.Edges.Count)
                return CurrentMesh.Edges[edgeIndex].State;
            return EdgeState.Empty;
        }

        private static EdgeState[] CaptureEdgeStates(Mesh mesh)
        {
            var states = new EdgeState[mesh.Edges.Count];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = mesh.Edges[i].State;
            }
            return states;
        }

        private bool DismissVisualSolveInternal()
        {
            if (visualSolveSteps.Count == 0 && visualSolveSolutionMesh == null)
                return false;

            visualSolveSteps.Clear();
            visualSolveSolutionMesh = null;
            VisualSolveStepIndex = 0;
            return true;
        }

        public void DismissVisualSolve()
        {
            if (DismissVisualSolveInternal())
                NotifyChanged();
        }

        public bool StartVisualSolve()
        {
            if (CurrentMesh == null || IsGenerating) return false;

            DismissVisualSolveInternal();
            ClearHint();

            Mesh solver = CreateFullSolverMesh(CurrentMesh);
            solver.Clear();
            solver.SuperSlowMo = true;

            visualSolveSteps.Add(new VisualSolveStep(CaptureEdgeStates(solver), "Starting solver..."));

            void OnUpdate(object sender, MeshChangeUpdateEventArgs args)
            {
                if (visualSolveSteps.Count >= MaxVisualSolveFrames - 1)
                    return;

                EdgeState[] snapshot = CaptureEdgeStates(args.CurrentMesh);
                if (snapshot.AsSpan().SequenceEqual(visualSolveSteps[^1].EdgeStates))
                    return;

                string status;
                bool isBacktrack = false;
                bool isBranching = false;

                if (args.Starting)
                {
                    status = "Testing branch...";
                    isBranching = true;
                }
                else if (!args.SuccessfulAttempt && args.JustDone != null)
                {
                    status = "Contradiction found — backtracking";
                    isBacktrack = true;
                }
                else if (args.JustDone != null)
                {
                    status = "Branch consistent — evaluating opposite";
                    isBranching = true;
                }
                else if (!args.SuccessfulAttempt)
                {
                    status = "Propagating deduction wave...";
                }
                else
                {
                    status = "Applying deduced moves";
                }

                visualSolveSteps.Add(new VisualSolveStep(snapshot, status, isBacktrack, isBranching));
            }

            solver.MeshChangeUpdate += OnUpdate;
            SolveState res;
            try
            {
                res = solver.TrySolve();
            }
            finally
            {
                solver.MeshChangeUpdate -= OnUpdate;
            }

            if ((res == SolveState.Solved || res == SolveState.MultipleSolutions) && solver.SolutionFound != null)
            {
                EdgeState[] finalStates = CaptureEdgeStates(solver.SolutionFound);
                visualSolveSolutionMesh = solver.SolutionFound;

                if (!finalStates.AsSpan().SequenceEqual(visualSolveSteps[^1].EdgeStates))
                {
                    visualSolveSteps.Add(new VisualSolveStep(finalStates, "Solution complete!"));
                }
                else
                {
                    visualSolveSteps[^1] = visualSolveSteps[^1] with { Status = "Solution complete!", IsBacktrack = false, IsBranching = false };
                }

                VisualSolveStepIndex = 0;
                NotifyChanged();
                return true;
            }

            DismissVisualSolveInternal();
            NotifyChanged();
            return false;
        }

        public void SetVisualSolveStep(int index)
        {
            if (!IsVisualSolving) return;
            int clamped = Math.Clamp(index, 0, visualSolveSteps.Count - 1);
            if (clamped == VisualSolveStepIndex) return;
            VisualSolveStepIndex = clamped;
            NotifyChanged();
        }

        public bool StepVisualSolveForward()
        {
            if (!IsVisualSolving || VisualSolveStepIndex >= visualSolveSteps.Count - 1)
                return false;
            VisualSolveStepIndex++;
            NotifyChanged();
            return true;
        }

        public bool StepVisualSolveBackward()
        {
            if (!IsVisualSolving || VisualSolveStepIndex <= 0)
                return false;
            VisualSolveStepIndex--;
            NotifyChanged();
            return true;
        }

        public bool ApplyVisualSolveSolution()
        {
            if (!IsVisualSolving || CurrentMesh == null || visualSolveSolutionMesh == null)
                return false;

            var action = new PuzzleBatchSolveAction(CurrentMesh, visualSolveSolutionMesh);
            if (UndoTree.Do(action))
            {
                WasAutoSolved = true;
                DismissVisualSolveInternal();
                CheckSolution();
                NotifyChanged();
                return true;
            }

            return false;
        }

        public bool SolvePuzzle()
        {
            if (CurrentMesh == null || IsGenerating) return false;

            DismissVisualSolveInternal();
            Mesh original = CreateFullSolverMesh(CurrentMesh);
            original.Clear();

            SolveState res = original.TrySolve();
            if ((res == SolveState.Solved || res == SolveState.MultipleSolutions) && original.SolutionFound != null)
            {
                var action = new PuzzleBatchSolveAction(CurrentMesh, original.SolutionFound);
                if (UndoTree.Do(action))
                {
                    WasAutoSolved = true;
                    CheckSolution();
                    NotifyChanged();
                    return true;
                }
            }
            return false;
        }

        public bool RequestHint(bool applyMove)
        {
            if (CurrentMesh == null || IsGenerating) return false;
            DismissVisualSolveInternal();

            var (edgeIdx, targetState, message) = ComputeHint();
            HintMessage = message;

            if (edgeIdx.HasValue)
            {
                HintEdgeIndex = edgeIdx.Value;
                HintTargetState = targetState;
                HintIsApplied = applyMove;
                if (applyMove)
                {
                    var action = new PuzzleSetEdgeStateAction(CurrentMesh, edgeIdx.Value, targetState, DisallowFalseMove);
                    UndoTree.Do(action);
                    CheckSolution();
                }
                else
                {
                    // Don't give away whether it's a line or a cross.
                    HintMessage = "The highlighted edge can be determined from what's around it.";
                }

                var flashToken = ResetTokenSource(ref hintFlashCts);
                int delayMs = applyMove ? 1500 : 3500;
                int capturedEdge = edgeIdx.Value;
                _ = ClearAfterDelayAsync(delayMs, flashToken, () =>
                {
                    if (HintEdgeIndex == capturedEdge)
                    {
                        HintEdgeIndex = null;
                        NotifyChanged();
                    }
                });
            }

            var msgToken = ResetTokenSource(ref hintMessageCts);
            _ = ClearAfterDelayAsync(5000, msgToken, () =>
            {
                HintMessage = null;
                NotifyChanged();
            });

            NotifyChanged();
            return edgeIdx.HasValue;
        }

        public (int? EdgeIndex, EdgeState TargetState, string Message) ComputeHint()
        {
            if (CurrentMesh == null)
                return (null, EdgeState.Empty, "No puzzle active.");

            if (IsSolved)
                return (null, EdgeState.Empty, "The puzzle is already solved!");

            // 1. Check for immediate user contradictions on the board
            for (int c = 0; c < CurrentMesh.Cells.Count; c++)
            {
                var cell = CurrentMesh.Cells[c];
                if (cell.TargetCount >= 0 && cell.FilledCount > cell.TargetCount)
                {
                    return (null, EdgeState.Empty, $"Contradiction: Cell clue {cell.TargetCount} has {cell.FilledCount} filled edges.");
                }
            }
            for (int v = 0; v < CurrentMesh.Intersections.Count; v++)
            {
                var inter = CurrentMesh.Intersections[v];
                if (inter.FilledCount > 2)
                {
                    return (null, EdgeState.Empty, "Contradiction: A vertex has more than 2 connected lines.");
                }
            }

            // 2. Clone mesh and run iterative deduction pass
            Mesh hintMesh = new Mesh(CurrentMesh);
            hintMesh.ConsiderIntersectCellInteractsAsSimple = true;
            hintMesh.ConsiderMultipleLoops = true;
            hintMesh.IterativeSolverDepth = 0;
            hintMesh.IterativeRecMaxDepth = 1;
            hintMesh.UseColoring = false;
            hintMesh.UseCellPairs = false;
            hintMesh.UseCellPairsTopLevel = false;
            hintMesh.UseEdgeRestricts = false;
            hintMesh.UseCellColoring = false;
            hintMesh.UseDerivedColoring = false;
            hintMesh.UseMerging = false;
            hintMesh.UseIntersectCellInteractsInSolver = true;

            List<IAction> changes = new List<IAction>();
            bool startOk = hintMesh.PerformStart(changes);
            if (!startOk)
            {
                return (null, EdgeState.Empty, "Contradiction detected in current board state.");
            }

            SetAction? action = changes.OfType<SetAction>().FirstOrDefault(a =>
                a.EdgeIndex >= 0 && a.EdgeIndex < CurrentMesh.Edges.Count &&
                CurrentMesh.Edges[a.EdgeIndex].State != a.EdgeState);

            while (action == null && hintMesh.IterativeSolverDepth <= 10)
            {
                changes.Clear();
                hintMesh.GetSomething(changes);
                action = changes.OfType<SetAction>().FirstOrDefault(a =>
                    a.EdgeIndex >= 0 && a.EdgeIndex < CurrentMesh.Edges.Count &&
                    CurrentMesh.Edges[a.EdgeIndex].State != a.EdgeState);
                if (action != null)
                    break;
                hintMesh.IterativeSolverDepth++;
            }

            if (action != null)
            {
                int edgeIdx = action.EdgeIndex;
                EdgeState state = action.EdgeState;
                string reason = ExplainDeduction(CurrentMesh, edgeIdx, state);
                return (edgeIdx, state, reason);
            }

            // 3. Fallback: full solver search on current state
            Mesh fullSolverMesh = CreateFullSolverMesh(CurrentMesh);

            SolveState solveRes = fullSolverMesh.TrySolve();
            if ((solveRes == SolveState.Solved || solveRes == SolveState.MultipleSolutions) && fullSolverMesh.SolutionFound != null)
            {
                var sol = fullSolverMesh.SolutionFound;
                for (int i = 0; i < CurrentMesh.Edges.Count; i++)
                {
                    if (CurrentMesh.Edges[i].State == EdgeState.Empty && sol.Edges[i].State != EdgeState.Empty)
                    {
                        string stateStr = sol.Edges[i].State == EdgeState.Filled ? "line" : "cross (✕)";
                        return (i, sol.Edges[i].State, $"Advanced deduction: This edge must be a {stateStr}.");
                    }
                }
            }
            else if (solveRes == SolveState.NoSolutions)
            {
                return (null, EdgeState.Empty, "Contradiction: No valid solution exists from this board state.");
            }

            return (null, EdgeState.Empty, "No logical deduction found at this step.");
        }

        private static string ExplainDeduction(Mesh mesh, int edgeIndex, EdgeState targetState)
        {
            Edge edge = mesh.Edges[edgeIndex];

            // Check adjacent cells
            foreach (int cIdx in edge.Cells)
            {
                if (cIdx < 0 || cIdx >= mesh.Cells.Count) continue;
                var cell = mesh.Cells[cIdx];
                if (cell.TargetCount == 0 && targetState == EdgeState.Excluded)
                {
                    return "Cell clue 0: all surrounding edges must be excluded.";
                }
                if (cell.TargetCount >= 0)
                {
                    if (cell.FilledCount == cell.TargetCount && targetState == EdgeState.Excluded)
                    {
                        return $"Clue {cell.TargetCount} already satisfied: remaining edges must be excluded.";
                    }
                    if (cell.Edges.Count - cell.ExcludedCount == cell.TargetCount && targetState == EdgeState.Filled)
                    {
                        return $"Clue {cell.TargetCount} needs all remaining edges: must be filled.";
                    }
                }
            }

            // Check adjacent intersections
            if (edge.Intersections != null && edge.Intersections.Length >= 2)
            {
                foreach (int vIdx in edge.Intersections)
                {
                    if (vIdx < 0 || vIdx >= mesh.Intersections.Count) continue;
                    var inter = mesh.Intersections[vIdx];
                    if (inter.FilledCount == 2 && targetState == EdgeState.Excluded)
                    {
                        return "Vertex already has 2 connections: remaining edges must be excluded.";
                    }
                    if (inter.FilledCount == 1 && inter.Edges.Count - inter.ExcludedCount == 2 && targetState == EdgeState.Filled)
                    {
                        return "Loop entering vertex has only one exit path: must continue here.";
                    }
                    if (inter.FilledCount == 0 && inter.Edges.Count - inter.ExcludedCount < 2 && targetState == EdgeState.Excluded)
                    {
                        return "Dead end: loop cannot enter vertex without an exit path.";
                    }
                }
            }

            return targetState == EdgeState.Filled
                ? "Logical deduction: This edge must be part of the loop."
                : "Logical deduction: This edge cannot be part of the loop.";
        }

        public void Dispose()
        {
            generationCts?.Cancel();
            generationCts?.Dispose();
            invalidFlashCts?.Cancel();
            invalidFlashCts?.Dispose();
            hintFlashCts?.Cancel();
            hintFlashCts?.Dispose();
            hintMessageCts?.Cancel();
            hintMessageCts?.Dispose();
        }
    }

    public enum MouseWheelMode
    {
        Zoom = 0,
        UndoRedo = 1
    }

    public enum GeneratorSolverMode
    {
        Auto = 0,
        Iterative = 1,
        Recursive = 2
    }

    public sealed record VisualSolveStep(
        EdgeState[] EdgeStates,
        string Status,
        bool IsBacktrack = false,
        bool IsBranching = false);

    public sealed record PuzzleAnalysisResult(
        string SolveStatus,
        string TechniqueTier,
        int BranchDepth,
        int ClueCount,
        int TotalCells,
        int EdgeCount,
        int? LookaheadDepth = null);

    public class UserPreferences
    {
        public bool DisallowFalseMove { get; set; } = false;
        public bool ShowCellColors { get; set; } = false;
        public bool ShowEdgeColors { get; set; } = false;
        public int AutoMoveLevel { get; set; } = 0;
        public bool AutoMoveConsiderMultipleLoops { get; set; } = true;
        public bool AutoMoveUseIC { get; set; } = false;
        public bool AutoStart { get; set; } = false;
        public MouseWheelMode WheelMode { get; set; } = MouseWheelMode.Zoom;
        public bool ShowClock { get; set; } = true;
        public double GenerateLengthFraction { get; set; } = 0.5;
        public double GenerateBoringFraction { get; set; } = 0.01;
        public GeneratorSolverMode SolverModeOverride { get; set; } = GeneratorSolverMode.Auto;
    }

    public class SavedGameState
    {
        public string Puzzle { get; set; } = string.Empty;
        public int[]? EdgeStates { get; set; }
        public int[]? CellColors { get; set; }
        public int[]? MarkedEdges { get; set; }
        public double ElapsedSeconds { get; set; }
        public bool IsSolved { get; set; }
        public bool WasAutoSolved { get; set; }
        public bool DisallowFalseMove { get; set; }
        public bool ShowCellColors { get; set; }
        public string Type { get; set; } = "Square";
        public string Size { get; set; } = "10x10";
        public int Difficulty { get; set; } = 1;
    }
}

