using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LoopDeLoop;

namespace LoopDeLoop.Web.Services
{
    public class GameService
    {
        public Mesh? CurrentMesh { get; private set; }
        public UndoTree UndoTree { get; private set; } = new();
        public HashSet<int> MarkedEdges { get; private set; } = new();

        public MeshType CurrentType { get; set; } = MeshType.Square;
        public string SizeText { get; set; } = "10x10";
        public int Difficulty { get; set; } = 1; // Easy

        public bool DisallowFalseMove { get; set; } = false;
        public bool ShowCellColors { get; set; } = false;

        public bool IsGenerating { get; private set; }
        public int PrunedProgress { get; private set; }
        public int TotalCells { get; private set; }

        public DateTime? StartTime { get; private set; }
        public TimeSpan ElapsedTime => StartTime.HasValue ? DateTime.UtcNow - StartTime.Value : TimeSpan.Zero;
        public bool IsSolved { get; private set; }
        public bool WasAutoSolved { get; private set; }

        public int? InvalidEdgeIndex { get; private set; }
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

            generationCts?.Cancel();
            generationCts = new CancellationTokenSource();
            var token = generationCts.Token;

            IsGenerating = true;
            GenerationStartTime = DateTime.UtcNow;
            IsSolved = false;
            PrunedProgress = 0;
            TotalCells = 0;
            NotifyChanged();

            // Allow UI to render the loading overlay before computation begins
            await Task.Delay(20);

            int width, height;
            if (!PuzzleHelper.ParseSize(SizeText, CurrentType, out width, out height))
            {
                PuzzleHelper.GetDefaultSize(CurrentType, out width, out height);
                SizeText = $"{width}x{height}";
            }

            try
            {
                Mesh mesh = PuzzleHelper.MakeMesh(width, height, CurrentType, Difficulty);
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
                    StartTime = DateTime.UtcNow;
                    IsSolved = false;
                    WasAutoSolved = false;
                    InvalidEdgeIndex = null;
                    ClearHint();
                }
            }
            catch (OperationCanceledException)
            {
                // Gracefully cancelled by user
            }
            finally
            {
                generatingMesh = null;
                generationCts = null;
                GenerationStartTime = null;
                IsGenerating = false;
                NotifyChanged();
            }
        }

        public void ToggleEdge(int edgeIndex, bool isAlternative)
        {
            if (CurrentMesh == null || edgeIndex < 0 || edgeIndex >= CurrentMesh.Edges.Count)
                return;

            if (MarkedEdges.Contains(edgeIndex))
                return;

            var current = CurrentMesh.Edges[edgeIndex].State;
            var next = !isAlternative
                ? (current == EdgeState.Empty ? EdgeState.Filled : (current == EdgeState.Filled ? EdgeState.Excluded : EdgeState.Empty))
                : (current == EdgeState.Empty ? EdgeState.Excluded : (current == EdgeState.Excluded ? EdgeState.Filled : EdgeState.Empty));

            var action = new PuzzleSetEdgeStateAction(CurrentMesh, edgeIndex, next, DisallowFalseMove);
            if (UndoTree.Do(action))
            {
                InvalidEdgeIndex = null;
                HintEdgeIndex = null;
                CheckSolution();
                NotifyChanged();
            }
            else if (DisallowFalseMove)
            {
                FlashInvalidEdge(edgeIndex);
            }
        }

        public void SetEdgeDirect(int edgeIndex, EdgeState targetState)
        {
            if (CurrentMesh == null || edgeIndex < 0 || edgeIndex >= CurrentMesh.Edges.Count)
                return;

            if (MarkedEdges.Contains(edgeIndex))
                return;

            if (CurrentMesh.Edges[edgeIndex].State == targetState)
                return;

            var action = new PuzzleSetEdgeStateAction(CurrentMesh, edgeIndex, targetState, DisallowFalseMove);
            if (UndoTree.Do(action))
            {
                InvalidEdgeIndex = null;
                HintEdgeIndex = null;
                CheckSolution();
                NotifyChanged();
            }
            else if (DisallowFalseMove)
            {
                FlashInvalidEdge(edgeIndex);
            }
        }

        public void ToggleCellColor(int cellIndex, bool isAlternative)
        {
            if (CurrentMesh == null || cellIndex < 0 || cellIndex >= CurrentMesh.Cells.Count)
                return;

            var action = new PuzzleCellColorAction(CurrentMesh, cellIndex, isAlternative);
            if (UndoTree.Do(action))
            {
                NotifyChanged();
            }
        }

        public void Undo()
        {
            if (UndoTree.CanUndo)
            {
                UndoTree.Undo();
                WasAutoSolved = false;
                CheckSolution();
                NotifyChanged();
            }
        }

        public bool UndoAndForget()
        {
            if (UndoTree.CanUndo && UndoTree.UndoAndForget())
            {
                WasAutoSolved = false;
                CheckSolution();
                NotifyChanged();
                return true;
            }
            return false;
        }

        public void Redo()
        {
            if (UndoTree.CanRedo)
            {
                UndoTree.Redo();
                WasAutoSolved = UndoTree.CurrentAction is PuzzleBatchSolveAction;
                CheckSolution();
                NotifyChanged();
            }
        }

        public void Fix()
        {
            if (CurrentMesh == null) return;
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
            MarkedEdges.Clear();
            UndoTree.ClearMark();
            CheckSolution();
            NotifyChanged();
        }

        public void RevertToFix()
        {
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

        public bool RestoreState(SavedGameState state)
        {
            if (string.IsNullOrEmpty(state.Puzzle)) return false;

            Mesh? mesh = null;
            if (PuzzleCodec.TryDecode(state.Puzzle, out var decodedMesh, out int w, out int h, out var decodedType))
            {
                mesh = decodedMesh;
            }
            else
            {
                var lines = state.Puzzle.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
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

            if (state.ElapsedSeconds > 0)
            {
                StartTime = DateTime.UtcNow.AddSeconds(-state.ElapsedSeconds);
            }
            else
            {
                StartTime = DateTime.UtcNow;
            }

            IsSolved = state.IsSolved;
            WasAutoSolved = state.IsSolved && state.WasAutoSolved;
            InvalidEdgeIndex = null;
            ClearHint();
            CheckSolution();
            NotifyChanged();
            return true;
        }

        public bool LoadPuzzleText(string puzzleText)
        {
            if (string.IsNullOrEmpty(puzzleText)) return false;

            Mesh? mesh = null;
            if (PuzzleCodec.TryDecode(puzzleText, out var decodedMesh, out int w, out int h, out var decodedType))
            {
                mesh = decodedMesh;
                SizeText = $"{w}x{h}";
            }
            else
            {
                var lines = puzzleText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
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
            InvalidEdgeIndex = null;
            ClearHint();

            CheckSolution();
            NotifyChanged();
            return true;
        }

        private CancellationTokenSource? invalidFlashCts;
        private CancellationTokenSource? hintFlashCts;
        private CancellationTokenSource? hintMessageCts;

        public void FlashInvalidEdge(int edgeIndex)
        {
            invalidFlashCts?.Cancel();
            invalidFlashCts = new CancellationTokenSource();
            var token = invalidFlashCts.Token;

            InvalidEdgeIndex = edgeIndex;
            NotifyChanged();

            _ = Task.Delay(600, token).ContinueWith(t =>
            {
                if (!token.IsCancellationRequested && InvalidEdgeIndex == edgeIndex)
                {
                    InvalidEdgeIndex = null;
                    NotifyChanged();
                }
            }, TaskScheduler.Default);
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
            StartTime = DateTime.UtcNow;
            IsSolved = false;
            WasAutoSolved = false;
            InvalidEdgeIndex = null;
            ClearHint();
            NotifyChanged();
        }

        public bool SolvePuzzle()
        {
            if (CurrentMesh == null || IsGenerating) return false;

            Mesh original = new Mesh(CurrentMesh);
            original.Clear();
            original.ConsiderMultipleLoops = true;
            original.UseIntersectCellInteractsInSolver = false;
            original.UseColoring = true;
            original.UseEdgeRestricts = true;
            original.UseCellColoring = true;
            original.SolverMethod = SolverMethod.Recursive;
            original.ContaminateFullSolver = true;
            original.ColoringCheats = true;
            original.UseDerivedColoring = true;
            original.UseMerging = true;
            original.UseCellPairsTopLevel = true;
            original.UseCellPairs = false;
            original.IterativeRecMaxDepth = 1;

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

                hintFlashCts?.Cancel();
                hintFlashCts = new CancellationTokenSource();
                var flashToken = hintFlashCts.Token;
                int delayMs = applyMove ? 1500 : 3500;
                _ = Task.Delay(delayMs, flashToken).ContinueWith(t =>
                {
                    if (!flashToken.IsCancellationRequested && HintEdgeIndex == edgeIdx.Value)
                    {
                        HintEdgeIndex = null;
                        NotifyChanged();
                    }
                }, TaskScheduler.Default);
            }

            hintMessageCts?.Cancel();
            hintMessageCts = new CancellationTokenSource();
            var msgToken = hintMessageCts.Token;
            _ = Task.Delay(5000, msgToken).ContinueWith(t =>
            {
                if (!msgToken.IsCancellationRequested)
                {
                    HintMessage = null;
                    NotifyChanged();
                }
            }, TaskScheduler.Default);

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
            Mesh fullSolverMesh = new Mesh(CurrentMesh);
            fullSolverMesh.ConsiderMultipleLoops = true;
            fullSolverMesh.UseIntersectCellInteractsInSolver = false;
            fullSolverMesh.UseColoring = true;
            fullSolverMesh.UseEdgeRestricts = true;
            fullSolverMesh.UseCellColoring = true;
            fullSolverMesh.SolverMethod = SolverMethod.Recursive;
            fullSolverMesh.ContaminateFullSolver = true;
            fullSolverMesh.ColoringCheats = true;
            fullSolverMesh.UseDerivedColoring = true;
            fullSolverMesh.UseMerging = true;
            fullSolverMesh.UseCellPairsTopLevel = true;
            fullSolverMesh.UseCellPairs = false;
            fullSolverMesh.IterativeRecMaxDepth = 1;

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

