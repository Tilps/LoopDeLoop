using System;
using System.Collections.Generic;

namespace LoopDeLoop
{
    internal static class EdgeActionHelper
    {
        public static bool PerformTransition(
            Mesh mesh,
            int edgeIndex,
            EdgeState targetState,
            int autoMove,
            bool disallowFalseMove,
            List<IAction> actionsPerformed,
            out bool allSuccessful)
        {
            allSuccessful = true;
            Edge edge = mesh.Edges[edgeIndex];

            if (edge.State != EdgeState.Empty)
            {
                IAction unsetAction = new UnsetAction(mesh, edgeIndex);
                bool res = unsetAction.Perform();
                if (!res || !unsetAction.Successful)
                {
                    allSuccessful = false;
                    if (res)
                        actionsPerformed.Add(unsetAction);
                    if (disallowFalseMove)
                        return false;
                }
                else
                {
                    actionsPerformed.Add(unsetAction);
                }
            }

            if (targetState != EdgeState.Empty)
            {
                bool res = mesh.Perform(edgeIndex, targetState, actionsPerformed, autoMove);
                if (!res)
                {
                    allSuccessful = false;
                    if (disallowFalseMove)
                        return false;
                }
            }

            return true;
        }
    }

    public class PuzzleEdgeAction : IAction
    {
        public PuzzleEdgeAction(Mesh mesh, int edgeIndex, bool isAlternativeCycle = false, int autoMove = 0, bool disallowFalseMove = false)
        {
            this.mesh = mesh;
            this.edgeIndex = edgeIndex;
            this.isAlternativeCycle = isAlternativeCycle;
            this.autoMove = autoMove;
            this.disallowFalseMove = disallowFalseMove;
        }

        private readonly Mesh mesh;
        private readonly int edgeIndex;
        private readonly bool isAlternativeCycle;
        private readonly int autoMove;
        private readonly bool disallowFalseMove;
        private List<IAction>? actionsPerformed;
        private bool successful;

        public bool Successful => successful;
        public string Name => $"Edge {edgeIndex} Toggle";

        private static EdgeState NextState(EdgeState current, bool reverse)
        {
            return (reverse, current) switch
            {
                (false, EdgeState.Empty) => EdgeState.Filled,
                (false, EdgeState.Filled) => EdgeState.Excluded,
                (false, EdgeState.Excluded) => EdgeState.Empty,
                (true, EdgeState.Empty) => EdgeState.Excluded,
                (true, EdgeState.Excluded) => EdgeState.Filled,
                (true, EdgeState.Filled) => EdgeState.Empty,
                _ => EdgeState.Empty
            };
        }

        public bool Perform()
        {
            Edge closest = mesh.Edges[edgeIndex];
            EdgeState toggled = NextState(closest.State, isAlternativeCycle);
            actionsPerformed = [];

            if (!EdgeActionHelper.PerformTransition(mesh, edgeIndex, toggled, autoMove, disallowFalseMove, actionsPerformed, out successful))
            {
                Unperform();
                return false;
            }

            return true;
        }

        public void Unperform()
        {
            if (actionsPerformed is { Count: > 0 })
            {
                mesh.Unperform(actionsPerformed);
            }
        }

        public bool Equals(IAction? other)
        {
            return other is PuzzleEdgeAction o
                && o.mesh == mesh
                && o.edgeIndex == edgeIndex
                && o.isAlternativeCycle == isAlternativeCycle
                && o.autoMove == autoMove
                && o.disallowFalseMove == disallowFalseMove;
        }

        public override bool Equals(object? obj) => obj is IAction action && Equals(action);

        public override int GetHashCode() => HashCode.Combine(mesh, edgeIndex, isAlternativeCycle, autoMove, disallowFalseMove);
    }

    public class PuzzleCellColorAction : IAction
    {
        public PuzzleCellColorAction(Mesh mesh, int cellIndex, bool reverse = false)
        {
            this.mesh = mesh;
            this.cellIndex = cellIndex;
            this.reverse = reverse;
        }

        private readonly Mesh mesh;
        private readonly int cellIndex;
        private readonly bool reverse;
        private List<IAction>? actionsPerformed;
        private bool successful;

        public bool Successful => successful;
        public string Name => $"Cell {cellIndex} Color";

        private static int Toggle(int color, bool reverse)
        {
            if (!reverse)
            {
                if (color == 1) return -1;
                if (color == -1) return 0;
                return 1;
            }
            else
            {
                if (color == 1) return 0;
                if (color == -1) return 1;
                return -1;
            }
        }

        public bool Perform()
        {
            successful = false;
            Cell closest = mesh.Cells[cellIndex];
            int newColor = Toggle(closest.Color, reverse);
            actionsPerformed = new List<IAction>();

            if (closest.Color != 0)
            {
                IAction unsetAction = new CellColorClearAction(mesh, cellIndex);
                bool res = unsetAction.Perform();
                if (!res || !unsetAction.Successful)
                {
                    if (res)
                        actionsPerformed.Add(unsetAction);
                    Unperform();
                    return false;
                }
                actionsPerformed.Add(unsetAction);
            }

            if (newColor != 0)
            {
                IAction setAction = new CellColorJoinAction(mesh, cellIndex, -1, newColor == 1);
                bool res = setAction.Perform();
                if (!res || !setAction.Successful)
                {
                    if (res)
                        actionsPerformed.Add(setAction);
                    Unperform();
                    return false;
                }
                actionsPerformed.Add(setAction);
            }

            successful = true;
            return true;
        }

        public void Unperform()
        {
            if (actionsPerformed != null && actionsPerformed.Count > 0)
            {
                mesh.Unperform(actionsPerformed);
            }
        }

        public bool Equals(IAction? other)
        {
            return other is PuzzleCellColorAction o && o.mesh == mesh && o.cellIndex == cellIndex && o.reverse == reverse;
        }

        public override bool Equals(object? obj) => obj is IAction action && Equals(action);

        public override int GetHashCode() => HashCode.Combine(mesh, cellIndex, reverse);
    }

    public class PuzzleSetEdgeStateAction : IAction
    {
        public PuzzleSetEdgeStateAction(Mesh mesh, int edgeIndex, EdgeState targetState, bool disallowFalseMove = false, int autoMove = 0)
        {
            this.mesh = mesh;
            this.edgeIndex = edgeIndex;
            this.targetState = targetState;
            this.disallowFalseMove = disallowFalseMove;
            this.autoMove = autoMove;
        }

        private readonly Mesh mesh;
        private readonly int edgeIndex;
        private readonly EdgeState targetState;
        private readonly bool disallowFalseMove;
        private readonly int autoMove;
        private List<IAction>? actionsPerformed;
        private bool successful;

        public bool Successful => successful;
        public string Name => $"Edge {edgeIndex} -> {targetState}";

        public bool Perform()
        {
            Edge closest = mesh.Edges[edgeIndex];
            if (closest.State == targetState)
            {
                successful = true;
                return false;
            }

            actionsPerformed = [];
            if (!EdgeActionHelper.PerformTransition(mesh, edgeIndex, targetState, autoMove, disallowFalseMove, actionsPerformed, out successful))
            {
                Unperform();
                return false;
            }

            return true;
        }

        public void Unperform()
        {
            if (actionsPerformed is { Count: > 0 })
            {
                mesh.Unperform(actionsPerformed);
            }
        }

        public bool Equals(IAction? other)
        {
            return other is PuzzleSetEdgeStateAction o
                && o.mesh == mesh
                && o.edgeIndex == edgeIndex
                && o.targetState == targetState
                && o.disallowFalseMove == disallowFalseMove
                && o.autoMove == autoMove;
        }

        public override bool Equals(object? obj) => obj is IAction action && Equals(action);

        public override int GetHashCode() => HashCode.Combine(mesh, edgeIndex, targetState, disallowFalseMove, autoMove);
    }

    public class PuzzleBatchSolveAction : IAction
    {
        private readonly Mesh mesh;
        private readonly EdgeState[] newStates;
        private readonly List<IAction> performed = new List<IAction>();

        public PuzzleBatchSolveAction(Mesh mesh, Mesh solvedMesh)
        {
            this.mesh = mesh;
            newStates = new EdgeState[mesh.Edges.Count];
            for (int i = 0; i < mesh.Edges.Count; i++)
            {
                newStates[i] = solvedMesh.Edges[i].State;
            }
        }

        public bool Successful => true;
        public string Name => "Solve Puzzle";

        public bool Perform()
        {
            performed.Clear();
            // Use the regular actions so all internal bookkeeping stays consistent.
            for (int i = 0; i < mesh.Edges.Count; i++)
            {
                if (mesh.Edges[i].State != EdgeState.Empty)
                {
                    var unset = new UnsetAction(mesh, i);
                    if (unset.Perform())
                        performed.Add(unset);
                }
            }
            for (int i = 0; i < mesh.Edges.Count; i++)
            {
                if (newStates[i] != EdgeState.Empty)
                {
                    var set = new SetAction(mesh, i, newStates[i]);
                    if (set.Perform())
                        performed.Add(set);
                }
            }
            return true;
        }

        public void Unperform()
        {
            for (int i = performed.Count - 1; i >= 0; i--)
            {
                performed[i].Unperform();
            }
            performed.Clear();
        }

        public bool Equals(IAction? other)
        {
            return other is PuzzleBatchSolveAction o
                && o.mesh == mesh
                && newStates.AsSpan().SequenceEqual(o.newStates);
        }

        public override bool Equals(object? obj) => obj is IAction action && Equals(action);

        public override int GetHashCode()
        {
            var hc = new HashCode();
            hc.Add(mesh);
            hc.Add(newStates.Length);
            for (int i = 0; i < Math.Min(newStates.Length, 16); i++)
                hc.Add(newStates[i]);
            return hc.ToHashCode();
        }
    }
}
