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

        public event Action? OnStateChanged;

        public bool CanUndo => UndoTree.CanUndo;
        public bool CanRedo => UndoTree.CanRedo;

        public void NotifyChanged()
        {
            OnStateChanged?.Invoke();
        }

        public async Task GenerateNewPuzzleAsync()
        {
            if (IsGenerating) return;

            IsGenerating = true;
            IsSolved = false;
            PrunedProgress = 0;
            TotalCells = 0;
            MarkedEdges.Clear();
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
                TotalCells = mesh.Cells.Count;
                NotifyChanged();

                var progress = new Progress<int>(p =>
                {
                    PrunedProgress = p;
                    NotifyChanged();
                });

                await mesh.GenerateAsync(progress);
                CurrentMesh = mesh;

                UndoTree = new UndoTree();
                StartTime = DateTime.UtcNow;
                IsSolved = false;
            }
            finally
            {
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
                CheckSolution();
                NotifyChanged();
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
                CheckSolution();
                NotifyChanged();
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
                CheckSolution();
                NotifyChanged();
            }
        }

        public void Redo()
        {
            if (UndoTree.CanRedo)
            {
                UndoTree.Redo();
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

        public void CheckSolution()
        {
            if (CurrentMesh == null) return;
            bool currentlySolved = PuzzleHelper.CheckIsSolved(CurrentMesh);
            if (currentlySolved && !IsSolved)
            {
                IsSolved = true;
                Fix(); // Auto-fix solution upon victory
            }
            else if (!currentlySolved && IsSolved)
            {
                IsSolved = false;
            }
        }
    }
}

