using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LoopDeLoop
{
    public enum EdgeState : byte
    {
        Empty,
        Filled,
        Excluded,
    }

    public enum TriState : byte
    {
        Unknown,
        Same,
        Opposite,
    }

    public enum EdgePairRestriction : byte
    {
        None,
        NotBoth,
        NotNeither,
    }

    public enum SolverMethod
    {
        Iterative,
        Recursive,
    }

    public enum SolveState
    {
        NoSolutions,
        MultipleSolutions,
        Solved,
    }

    public class Edge
    {
        public Edge()
        {
            Cells = new List<int>();
            Intersections = new int[2];
        }
        public Edge(Edge other)
        {
            State = other.State;
            Intersections = (int[])other.Intersections.Clone();
            Cells = new List<int>(other.Cells);
            Color = other.Color;
            EdgeSet = other.EdgeSet;
        }

        public List<int> Cells;
        public int[] Intersections;
        public EdgeState State;
        public int Color;
        public int EdgeSet;

        public Edge Clone()
        {
            return new Edge(this);
        }
    }

    public class Intersection
    {
        public Intersection()
        {
            Edges = new List<int>();
            Cells = new List<int>();
        }
        public Intersection(Intersection other)
        {
            FilledCount = other.FilledCount;
            ExcludedCount = other.ExcludedCount;
            Edges = new List<int>(other.Edges);
            Cells = new List<int>(other.Cells);
            X = other.X;
            Y = other.Y;
            EdgeSet = other.EdgeSet;
            // Can't clone edge set entries across, have to 'fixup' them later.
            //EdgeSetEntry = other.EdgeSetEntry;
        }
        public List<int> Edges;
        public List<int> Cells;
        public int FilledCount;
        public int ExcludedCount;
        public float X;
        public float Y;
        public int EdgeSet;

        public Intersection Clone()
        {
            return new Intersection(this);
        }
    }

    public class Cell
    {
        public Cell()
        {
            Edges = new List<int>();
            Intersections = new List<int>();
            TargetCount = -1;
        }
        public Cell(Cell other)
        {
            TargetCount = other.TargetCount;
            FilledCount = other.FilledCount;
            ExcludedCount = other.ExcludedCount;
            Edges = new List<int>(other.Edges);
            Intersections = new List<int>(other.Intersections);
            Color = other.Color;
        }
        public List<int> Edges;
        public List<int> Intersections;
        public int TargetCount;
        public int FilledCount;
        public int ExcludedCount;
        public int Color;

        public Cell Clone()
        {
            return new Cell(this);
        }
    }

    public enum MeshType
    {
        Square,
        Hexagonal,
        Triangle,
        Octagon,
        Hexagonal2,
        Square2,
        Pentagon,
        Hexagonal3,
        SquareSymmetrical,
        Kites,
        AsymmetricPentagons,
        Diamonds,
        DiamondSquare,
        PentagonHexagon,
        FloretPentagons,
        Hexagonal4,
        CairoPentagons,
        Square3,
        PentagonHexagon2,
        HexPentagons = FloretPentagons,
        SnubSquare = Square3,
        PrismaticPentagons = PentagonHexagon2,
    }

#region ApproxPointStorage class to help with constructing grids.
    /// <summary>
    /// Spatial grid bucketing for approximate point lookup.
    /// Recommended usage: choose epsilon to be roughly 1/10th (or smaller) of the smallest distance
    /// between any two distinct points in the mesh (typically distinct points are >100x epsilon apart).
    /// Cell size is set to 2 * epsilon. Under standard usage, each bucket contains at most 1 point.
    /// </summary>
    public class ApproxPointStorage
    {
        private readonly float eps;
        private readonly float cellSize;
        private readonly Dictionary<(long, long), List<(float X, float Y, int Index)>> buckets = new();

        public ApproxPointStorage(float eps)
        {
            this.eps = eps;
            this.cellSize = eps * 2f;
        }

        public int Add(float x, float y, int newIndex)
        {
            long bx = (long)MathF.Floor(x / cellSize);
            long by = (long)MathF.Floor(y / cellSize);

            for (long dx = -1; dx <= 1; dx++)
            {
                for (long dy = -1; dy <= 1; dy++)
                {
                    if (buckets.TryGetValue((bx + dx, by + dy), out var list))
                    {
                        foreach (var pt in list)
                        {
                            if (MathF.Abs(pt.X - x) <= eps && MathF.Abs(pt.Y - y) <= eps)
                                return pt.Index;
                        }
                    }
                }
            }

            if (!buckets.TryGetValue((bx, by), out var cellList))
            {
                cellList = new List<(float X, float Y, int Index)>();
                buckets[(bx, by)] = cellList;
            }
            System.Diagnostics.Debug.Assert(cellList.Count == 0, "Bucket already contains an entry; points may be unexpectedly close or epsilon is too large.");
            cellList.Add((x, y, newIndex));
            return newIndex;
        }
    }
#endregion

    public delegate void MeshChangeUpdateEventHandler(object sender, MeshChangeUpdateEventArgs args);

    public class MeshChangeUpdateEventArgs
    {
        public MeshChangeUpdateEventArgs(Mesh current, List<IAction>? justDone, bool success)
        {
            this.CurrentMesh = current;
            this.JustDone = justDone;
            this.SuccessfulAttempt = success;
        }
        public MeshChangeUpdateEventArgs(Mesh current, List<IAction>? justDone, bool success, bool starting)
        {
            this.CurrentMesh = current;
            this.JustDone = justDone;
            this.SuccessfulAttempt = success;
            this.Starting = starting;
        }

        public bool SuccessfulAttempt;

        public List<IAction>? JustDone;

        public Mesh CurrentMesh;

        public bool Starting;
    }

    public partial class Mesh
    {
        public Mesh(Mesh other)
        {
            edges = new List<Edge>();
            foreach (Edge edge in other.edges)
                edges.Add(edge.Clone());
            intersections = new List<Intersection>();
            foreach (Intersection inters in other.intersections)
                intersections.Add(inters.Clone());
            cells = new List<Cell>();
            foreach (Cell cell in other.cells)
                cells.Add(cell.Clone());
            SolverMethod = other.SolverMethod;
            IterativeSolverDepth = other.iterativeSolverDepth;
            IterativeRecMaxDepth = other.iterativeRecMaxDepth;
            meshType = other.meshType;
            considerMultipleLoops = other.considerMultipleLoops;
            considerIntersectCellInteractsAsSimple = other.considerIntersectCellInteractsAsSimple;
            useIntersectCellInteractsInSolver = other.useIntersectCellInteractsInSolver;
            numberOfNumbers = other.numberOfNumbers;
            this.coloringCheats = other.coloringCheats;
            this.colorSets = new List<List<int>>();
            foreach (List<int> otherColorSet in other.colorSets)
                colorSets.Add(new List<int>(otherColorSet));
            this.cellColorSets = new List<List<int>>();
            foreach (List<int> otherCellColorSet in other.cellColorSets)
                cellColorSets.Add(new List<int>(otherCellColorSet));
            this.contaminateFullSolver = other.contaminateFullSolver;
            this.edgeSets = new List<List<int>>();
            foreach (List<int> otherEdgeSet in other.edgeSets)
                edgeSets.Add(new List<int>(otherEdgeSet));
            this.GenerateLengthFraction = other.GenerateLengthFraction;
            this.GenerateBoringFraction = other.GenerateBoringFraction;
            this.satisifiedCount = other.satisifiedCount;
            this.satisifiedIntersCount = other.satisifiedIntersCount;
            this.useColoring = other.useColoring;
            this.useCellPairs = other.useCellPairs;
            this.useCellPairsTopLevel = other.useCellPairsTopLevel;
            this.useEdgeRestricts = other.useEdgeRestricts;
            this.useMerging = other.useMerging;
            this.useCellColoring = other.useCellColoring;
            this.useCellColoringTrials = other.useCellColoringTrials;
            this.useDerivedColoring = other.useDerivedColoring;
            this.superSlowMo = other.superSlowMo;
            if (other.edgePairRestrictions != null)
                this.edgePairRestrictions = (EdgePairRestriction[,])other.edgePairRestrictions.Clone();
            else
                this.edgePairRestrictions = new EdgePairRestriction[edges.Count, edges.Count];
        }

#region Mesh Construction using known prototype.
        public Mesh(int width, int height, MeshType type)
        {
            meshType = type;
            edges = new List<Edge>();
            intersections = new List<Intersection>();
            cells = new List<Cell>();
            ConstructGrid(width, height, type);
            FullClear();
        }
#endregion

        public event MeshChangeUpdateEventHandler? MeshChangeUpdate;

        public MeshType MeshType
        {
            get
            {
                return meshType;
            }
        }
        private MeshType meshType;

        public List<Edge> Edges
        {
            get
            {
                return edges;
            }
        }
        List<Edge> edges;

        public List<Intersection> Intersections
        {
            get
            {
                return intersections;
            }
        }
        List<Intersection> intersections;

        public List<Cell> Cells
        {
            get
            {
                return cells;
            }
        }
        List<Cell> cells;

        private int[,] edgeDistances
        {
            get
            {
                if (edgeDistancesCache == null)
                {
                    edgeDistancesCache = new int[edges.Count, edges.Count];
                    for (int i = 0; i < edges.Count; i++)
                    {
                        for (int j = 0; j < edges.Count; j++)
                        {
                            if (i != j)
                                edgeDistancesCache[i, j] = int.MaxValue;
                        }
                    }
                    bool progress = true;
                    while (progress)
                    {
                        progress = false;
                        for (int next = 0; next < edges.Count; next++)
                        {
                            Edge e = edges[next];
                            List<int> oneAway = new List<int>();
                            for (var index = 0; index < e.Intersections.Length; index++)
                            {
                                int inters = e.Intersections[index];
                                Intersection i = intersections[inters];
                                for (var index1 = 0; index1 < i.Edges.Count; index1++)
                                {
                                    int other = i.Edges[index1];
                                    if (other != next)
                                        oneAway.Add(other);
                                }
                            }
                            for (int i = 0; i < edges.Count; i++)
                            {
                                int toMe = edgeDistancesCache[next, i];
                                if (toMe != int.MaxValue)
                                {
                                    for (var index = 0; index < oneAway.Count; index++)
                                    {
                                        int other = oneAway[index];
                                        if (edgeDistancesCache[other, i] > toMe + 1)
                                        {
                                            edgeDistancesCache[other, i] = edgeDistancesCache[i, other] = toMe + 1;
                                            progress = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return edgeDistancesCache;
            }
        }
        int[,]? edgeDistancesCache;

        public void GetEdgeExtent(Edge e, out float startX, out float startY, out float endX, out float endY)
        {
            Intersection start = intersections[e.Intersections[0]];
            Intersection end = intersections[e.Intersections[1]];
            startX = start.X;
            startY = start.Y;
            endX = end.X;
            endY = end.Y;
        }

        public SolverMethod SolverMethod
        {
            get
            {
                return solverMethod;
            }
            set
            {
                solverMethod = value;
            }
        }
        private SolverMethod solverMethod;

        public int IterativeSolverDepth
        {
            get
            {
                return iterativeSolverDepth;
            }
            set
            {
                iterativeSolverDepth = value;
            }
        }
        private int iterativeSolverDepth = 1;

        public int IterativeRecMaxDepth
        {
            get
            {
                return iterativeRecMaxDepth;
            }
            set
            {
                iterativeRecMaxDepth = value;
            }
        }
        private int iterativeRecMaxDepth = 2;
        private int iterativeRecDepth = 1;

        public bool ConsiderIntersectCellInteractsAsSimple
        {
            get
            {
                return considerIntersectCellInteractsAsSimple;
            }
            set
            {
                considerIntersectCellInteractsAsSimple = value;
            }
        }
        private bool considerIntersectCellInteractsAsSimple;

        public bool UseIntersectCellInteractsInSolver
        {
            get
            {
                return useIntersectCellInteractsInSolver;
            }
            set
            {
                useIntersectCellInteractsInSolver = value;
            }
        }
        private bool useIntersectCellInteractsInSolver;

        public bool UseMerging
        {
            get
            {
                return useMerging;
            }
            set
            {
                useMerging = value;
            }
        }
        private bool useMerging;

        public bool ConsiderMultipleLoops
        {
            get
            {
                return considerMultipleLoops;
            }
            set
            {
                considerMultipleLoops = value;
            }
        }
        private bool considerMultipleLoops = false;

        public Mesh? FinalSolution
        {
            get
            {
                return finalSolution;
            }
        }
        Mesh? finalSolution;

        public int[]? FinalDepthPatern
        {
            get
            {
                return finalDepthPatern;
            }
        }
        int[]? finalDepthPatern;

        public Mesh SolutionFound
        {
            get
            {
                return solutionsFound[0];
            }
        }
        List<Mesh> solutionsFound = new List<Mesh>();

        public int[] DepthPatern
        {
            get
            {
                return solutionDepthPatern[0];
            }
        }
        List<int[]> solutionDepthPatern = new List<int[]>();
        List<int> curDepthPatern = new List<int>();

        public bool TryGetEdgeJoining(int inters1, int inters2, out int edgeIndex)
        {
            if (inters1 < 0 || inters1 >= intersections.Count || inters2 < 0 || inters2 >= intersections.Count)
            {
                edgeIndex = -1;
                return false;
            }
            Intersection rInters1 = intersections[inters1];
            Intersection rInters2 = intersections[inters2];
            for (int i = 0; i < rInters1.Edges.Count; i++)
            {
                int candidate = rInters1.Edges[i];
                for (int j = 0; j < rInters2.Edges.Count; j++)
                {
                    if (candidate == rInters2.Edges[j])
                    {
                        edgeIndex = candidate;
                        return true;
                    }
                }
            }
            edgeIndex = -1;
            return false;
        }

        public int GetEdgeJoining(int inters1, int inters2)
        {
            if (TryGetEdgeJoining(inters1, inters2, out int edgeIndex))
                return edgeIndex;
            throw new ArgumentException("Specified intersections aren't joined.");
        }


        public bool JoinColor(int a, int b, bool invert, List<int[]> changes, ref bool wasteOftime)
        {
            wasteOftime = false;
            Edge e = edges[a];
            Edge other = edges[b];
            if (e.Color == 0 && other.Color == 0)
            {
                colorSets.Add(new List<int>());
                changes.Add(new int[] { colorSets.Count });
                e.Color = colorSets.Count;
                colorSets[e.Color - 1].Add(a);
                changes.Add(new int[] { a, 0, e.Color });
                other.Color = invert ? -e.Color : e.Color;
                colorSets[e.Color - 1].Add(b);
                changes.Add(new int[] { b, 0, other.Color });
            }
            else if (e.Color == 0)
            {
                e.Color = invert ? -other.Color : other.Color;
                colorSets[Math.Abs(e.Color) - 1].Add(a);
                changes.Add(new int[] { a, 0, e.Color });
            }
            else if (other.Color == 0)
            {
                other.Color = invert ? -e.Color : e.Color;
                colorSets[Math.Abs(other.Color) - 1].Add(b);
                changes.Add(new int[] { b, 0, other.Color });
            }
            else
            {
                if (invert && e.Color == other.Color)
                    return false;
                if (!invert && e.Color == -other.Color)
                    return false;
                if (e.Color == other.Color)
                {
                    wasteOftime = true;
                    return true;
                }
                if (e.Color == -other.Color)
                {
                    wasteOftime = true;
                    return true;
                }
                int before = Math.Abs(e.Color) > Math.Abs(other.Color) ? e.Color : other.Color;
                int after = Math.Abs(e.Color) > Math.Abs(other.Color) ? other.Color : e.Color;
                if (invert)
                    after = -after;
                List<int> colorSetBefore = colorSets[Math.Abs(before) - 1];
                List<int> colorSetAfter = colorSets[Math.Abs(after) - 1];
                for (int i = colorSetBefore.Count - 1; i >= 0; i--)
                {
                    int edge = colorSetBefore[i];
                    Edge curEdge = edges[edge];
                    if (curEdge.Color == before)
                    {
                        curEdge.Color = after;
                        colorSetAfter.Add(edge);
                        changes.Add(new int[] { edge, before, after });
                    }
                    else
                    {
                        curEdge.Color = -after;
                        colorSetAfter.Add(edge);
                        changes.Add(new int[] { edge, -before, -after });
                    }
                }
                colorSets[Math.Abs(before) - 1].Clear();
            }
            return true;
        }


        public bool JoinCellColor(int a, int b, bool invert, List<int[]> changes, ref bool wasteOftime)
        {
            //Debug.WriteLine(string.Format("CellJoin {0} to {1} invert={2}", a, b, invert), "Actions");
            wasteOftime = false;
            Cell c = cells[a];
            if (b == -1)
            {
                if (cellColorSets.Count == 0)
                {
                    cellColorSets.Add(new List<int>());
                    changes.Add(new int[] { cellColorSets.Count });
                }
                if (!invert && c.Color == 1 || invert && c.Color == -1)
                {
                    wasteOftime = true;
                    return true;
                }
                if (Math.Abs(c.Color) == 1)
                {
                    UnjoinCellColor(changes);
                    return false;
                }
                if (c.Color == 0)
                {
                    c.Color = invert ? -1 : 1;
                    cellColorSets[0].Add(a);
                    changes.Add(new int[] { a, 0, c.Color });
                }
                else
                {
                    int before = c.Color;
                    int after = 1;
                    if (invert)
                        after = -after;
                    List<int> colorSetBefore = cellColorSets[Math.Abs(before) - 1];
                    List<int> colorSetAfter = cellColorSets[0];
                    for (int i = colorSetBefore.Count - 1; i >= 0; i--)
                    {
                        int cell = colorSetBefore[i];
                        Cell curCell = cells[cell];
                        if (curCell.Color == before)
                        {
                            curCell.Color = after;
                            colorSetAfter.Add(cell);
                            changes.Add(new int[] { cell, before, after });
                        }
                        else
                        {
                            curCell.Color = -after;
                            colorSetAfter.Add(cell);
                            changes.Add(new int[] { cell, -before, -after });
                        }
                    }
                    cellColorSets[Math.Abs(before) - 1].Clear();
                }
                return true;
            }
            Cell other = cells[b];
            if (c.Color == 0 && other.Color == 0)
            {
                cellColorSets.Add(new List<int>());
                // First color set is reserved for the non-existant cell.
                changes.Add(new int[] { cellColorSets.Count });
                if (cellColorSets.Count == 1)
                {
                    cellColorSets.Add(new List<int>());
                    changes.Add(new int[] { cellColorSets.Count });
                }
                c.Color = cellColorSets.Count;
                cellColorSets[c.Color - 1].Add(a);
                changes.Add(new int[] { a, 0, c.Color });
                other.Color = invert ? -c.Color : c.Color;
                cellColorSets[c.Color - 1].Add(b);
                changes.Add(new int[] { b, 0, other.Color });
            }
            else if (c.Color == 0)
            {
                c.Color = invert ? -other.Color : other.Color;
                cellColorSets[Math.Abs(c.Color) - 1].Add(a);
                changes.Add(new int[] { a, 0, c.Color });
            }
            else if (other.Color == 0)
            {
                other.Color = invert ? -c.Color : c.Color;
                cellColorSets[Math.Abs(other.Color) - 1].Add(b);
                changes.Add(new int[] { b, 0, other.Color });
            }
            else
            {
                if (invert && c.Color == other.Color)
                    return false;
                if (!invert && c.Color == -other.Color)
                    return false;
                if (c.Color == other.Color)
                {
                    wasteOftime = true;
                    return true;
                }
                if (c.Color == -other.Color)
                {
                    wasteOftime = true;
                    return true;
                }
                int before = Math.Abs(c.Color) > Math.Abs(other.Color) ? c.Color : other.Color;
                int after = Math.Abs(c.Color) > Math.Abs(other.Color) ? other.Color : c.Color;
                if (invert)
                    after = -after;
                List<int> colorSetBefore = cellColorSets[Math.Abs(before) - 1];
                List<int> colorSetAfter = cellColorSets[Math.Abs(after) - 1];
                for (int i = colorSetBefore.Count - 1; i >= 0; i--)
                {
                    int cell = colorSetBefore[i];
                    Cell curCell = cells[cell];
                    if (curCell.Color == before)
                    {
                        curCell.Color = after;
                        colorSetAfter.Add(cell);
                        changes.Add(new int[] { cell, before, after });
                    }
                    else
                    {
                        curCell.Color = -after;
                        colorSetAfter.Add(cell);
                        changes.Add(new int[] { cell, -before, -after });
                    }
                }
                cellColorSets[Math.Abs(before) - 1].Clear();
            }
            return true;
        }

        internal bool PerformSetZero(int edgeIndex, EdgeState state, List<int[]> edgeSetChanges)
        {
            Edge edge = edges[edgeIndex];
            bool failed = RawEdgeSet(state, edge);
            if (state == EdgeState.Filled)
            {
                int edgeSet1 = GetEdgeSet(edge.Intersections[0], edgeIndex);
                int edgeSet2 = GetEdgeSet(edge.Intersections[1], edgeIndex);
                if (edgeSet1 == 0 && edgeSet2 == 0)
                {
                    edgeSets.Add(new List<int>());
                    edge.EdgeSet = edgeSets.Count;
                    edgeSets[edge.EdgeSet - 1].Add(edgeIndex);
                    edgeSetChanges.Add(new int[] { edgeIndex, edge.EdgeSet, 0 });
                }
                else if (edgeSet1 == 0 || edgeSet2 == 0)
                {
                    // one of them is positive
                    int edgeSet = Math.Max(edgeSet1, edgeSet2);
                    edgeSets[edgeSet - 1].Add(edgeIndex);
                    edge.EdgeSet = edgeSet;
                    edgeSetChanges.Add(new int[] { edgeIndex, edge.EdgeSet, 0 });
                }
                else if (edgeSet1 != edgeSet2)
                {
                    // Merge
                    int toKeep = Math.Min(edgeSet1, edgeSet2);
                    int toGo = Math.Max(edgeSet1, edgeSet2);
                    edgeSets[toKeep - 1].Add(edgeIndex);
                    edge.EdgeSet = toKeep;
                    edgeSetChanges.Add(new int[] { edgeIndex, edge.EdgeSet, 0 });
                    for (int i = edgeSets[toGo - 1].Count - 1; i >= 0; i--)
                    {
                        int otherEdge = edgeSets[toGo - 1][i];
                        edgeSets[toKeep - 1].Add(otherEdge);
                        Edge otherE = edges[otherEdge];
                        otherE.EdgeSet = toKeep;
                        edgeSetChanges.Add(new int[] { otherEdge, otherE.EdgeSet, toGo });
                    }
                    edgeSets[toGo - 1].Clear();
                }
                else
                {
                    int edgeSet = edgeSet1;
                    edgeSets[edgeSet - 1].Add(edgeIndex);
                    edge.EdgeSet = edgeSet;
                    edgeSetChanges.Add(new int[] { edgeIndex, edge.EdgeSet, 0 });
                    if (!failed && considerMultipleLoops)
                    {
                        // We're joining ... this might be bad.

                        // rule 1, can't close the loop if there are any numbers not satisifed yet.
                        if (satisifiedCount != numberOfNumbers)
                            return false;
                        // rule 2, can't close the loop if there are any intersections with odd number of filleds touching.
                        if (satisifiedIntersCount != intersections.Count)
                            return false;
                        // Rule 3, can't close the loop if there is more then one (non-empty) edge set.
                        int nonEmpty = 0;
                        for (var index = 0; index < edgeSets.Count; index++)
                        {
                            List<int> otherEdgeSet = edgeSets[index];
                            if (otherEdgeSet.Count > 0)
                                nonEmpty++;
                        }
                        if (nonEmpty != 1)
                            return false;
                        GenerateCheck();
                    }

                }
            }
            // up until here has to be atomic, all of it runs, or none of it does.  This is to ensure consistent state for rollback, later.
            if (failed)
                return false;
            return true;
        }

        private void GenerateCheck()
        {
            if (pruning && !earlyFail && finalSolution != null)
            {
                // We've found a solution, we can check that against the known solution to see if we've removed enough cells to force multiple solutions to exist.
                for (int i = 0; i < edges.Count; i++)
                {
                    if (edges[i].State == EdgeState.Filled)
                    {
                        if (finalSolution.edges[i].State != EdgeState.Filled)
                        {
                            // missmatch - early out somehow...
                            earlyFail = true;
                            break;
                        }
                    }
                }
            }
        }
        private bool earlyFail = false;

        private bool RawEdgeSet(EdgeState state, Edge edge)
        {
            if (state == EdgeState.Empty)
                throw new ArgumentException("Cannot set edge to Empty via RawEdgeSet.", nameof(state));
            if (edge.State != EdgeState.Empty)
                throw new InvalidOperationException("Edge already set.");
            edge.State = state;
            bool failed = false;
            for (var index = 0; index < edge.Cells.Count; index++)
            {
                int cellIndex = edge.Cells[index];
                Cell cell = cells[cellIndex];
                if (state == EdgeState.Filled)
                {
                    cell.FilledCount++;
                    if (cell.TargetCount >= 0)
                    {
                        if (cell.FilledCount > cell.TargetCount)
                            failed = true;
                        else if (cell.FilledCount == cell.TargetCount)
                            satisifiedCount++;
                    }
                }
                else
                {
                    cell.ExcludedCount++;
                    if (cell.TargetCount >= 0)
                    {
                        if (cell.Edges.Count - cell.ExcludedCount < cell.TargetCount)
                            failed = true;
                    }
                }
            }
            for (var index = 0; index < edge.Intersections.Length; index++)
            {
                int intersIndex = edge.Intersections[index];
                Intersection inters = intersections[intersIndex];
                if (state == EdgeState.Filled)
                {
                    inters.FilledCount++;
                    if (inters.FilledCount > 2)
                        failed = true;
                    if (inters.FilledCount > 0 && inters.Edges.Count - inters.ExcludedCount < 2)
                        failed = true;
                    if (inters.FilledCount == 2)
                        satisifiedIntersCount++;
                    else if (inters.FilledCount < 4)
                        satisifiedIntersCount--;
                }
                else
                {
                    inters.ExcludedCount++;
                    if (inters.FilledCount > 0 && inters.Edges.Count - inters.ExcludedCount < 2)
                        failed = true;
                }
            }
            return failed;
        }

        private int GetEdgeSet(int intersection, int edgeToIgnore)
        {
            Intersection inter = intersections[intersection];
            for (var index = 0; index < inter.Edges.Count; index++)
            {
                int edgeIndex = inter.Edges[index];
                if (edgeIndex == edgeToIgnore)
                    continue;
                Edge e = edges[edgeIndex];
                if (e.State == EdgeState.Filled)
                {
                    return e.EdgeSet;
                }
            }
            return 0;
        }

        internal void UnperformSetZero(int edgeIndex, EdgeState state, List<int[]> edgeSetChanges)
        {
            Edge edge = edges[edgeIndex];
            RawEdgeUnset(state, edge);
            for (int i = edgeSetChanges.Count - 1; i >= 0; i--)
            {
                int[] edgeSetChange = edgeSetChanges[i];
                int otherEdgeIndex = edgeSetChange[0];
                int newEdgeSet = edgeSetChange[1];
                int oldEdgeSet = edgeSetChange[2];

                List<int> edgeSet = edgeSets[newEdgeSet - 1];
                int edgeCheck = edgeSet[edgeSet.Count - 1];
                if (edgeCheck != otherEdgeIndex)
                    throw new InvalidOperationException("Attempting to undo out of order.");
                edgeSet.RemoveAt(edgeSet.Count - 1);
                if (oldEdgeSet != 0)
                {
                    edgeSet = edgeSets[oldEdgeSet - 1];
                    edgeSet.Add(otherEdgeIndex);
                }
                else
                {
                    // If it was a new edge, it may have been a new edge set, which needs to be removed.
                    if (newEdgeSet == edgeSets.Count && edgeSets[newEdgeSet - 1].Count == 0)
                        edgeSets.RemoveAt(newEdgeSet - 1);
                }
                Edge otherE = edges[otherEdgeIndex];
                otherE.EdgeSet = oldEdgeSet;
            }
        }

        private void RawEdgeUnset(EdgeState state, Edge edge)
        {
            if (state == EdgeState.Empty)
                return;
            edge.State = EdgeState.Empty;
            for (var index = 0; index < edge.Cells.Count; index++)
            {
                int cellIndex = edge.Cells[index];
                Cell cell = cells[cellIndex];
                if (state == EdgeState.Filled)
                {
                    if (cell.TargetCount > 0)
                        if (cell.FilledCount == cell.TargetCount)
                            satisifiedCount--;
                    cell.FilledCount--;
                }
                else
                    cell.ExcludedCount--;
            }
            for (var index = 0; index < edge.Intersections.Length; index++)
            {
                int intersIndex = edge.Intersections[index];
                Intersection inters = intersections[intersIndex];
                if (state == EdgeState.Filled)
                {
                    inters.FilledCount--;
                    if (inters.FilledCount == 2 || inters.FilledCount == 0)
                        satisifiedIntersCount++;
                    else if (inters.FilledCount == 1)
                        satisifiedIntersCount--;
                }
                else
                    inters.ExcludedCount--;
            }
        }

        internal bool PerformUnsetZero(int edgeIndex, out EdgeState oldState, List<int[]> edgeSetChanges)
        {
            Edge edge = edges[edgeIndex];
            oldState = edge.State;
            if (oldState == EdgeState.Empty)
                return false;
            RawEdgeUnset(oldState, edge);
            if (edge.EdgeSet != 0)
            {
                int oldEdgeSet = edge.EdgeSet;
                int index = edgeSets[edge.EdgeSet - 1].IndexOf(edgeIndex);
                edgeSetChanges.Add(new int[] { edgeIndex, 0, edge.EdgeSet, index });
                edgeSets[edge.EdgeSet - 1].RemoveAt(index);
                edge.EdgeSet = 0;
                int target = edge.Intersections[1];
                List<int> foundEdges = new List<int>();
                HashSet<int> seenEdges = new HashSet<int>();
                Stack<int> todoEdges = new Stack<int>();
                Stack<int> todoPrevInters = new Stack<int>();
                todoEdges.Push(edgeIndex);
                todoPrevInters.Push(target);
                bool foundTarget = false;
                HashSet<int> foundInters = new HashSet<int>();
                while (todoEdges.Count > 0)
                {
                    int curEdge = todoEdges.Pop();
                    int lastInters = todoPrevInters.Pop();
                    int curInters = GetOtherInters(curEdge, lastInters);
                    if (curInters == target)
                    {
                        foundTarget = true;
                        break;
                    }
                    if (foundInters.Add(curInters))
                    {
                        Intersection inters = intersections[curInters];
                        for (var i = 0; i < inters.Edges.Count; i++)
                        {
                            int nextEdge = inters.Edges[i];
                            if (nextEdge != curEdge)
                            {
                                Edge e = edges[nextEdge];
                                if (e.State == EdgeState.Filled && seenEdges.Add(nextEdge))
                                {
                                    foundEdges.Add(nextEdge);
                                    todoEdges.Push(nextEdge);
                                    todoPrevInters.Push(curInters);
                                }
                            }
                        }
                    }
                }
                if (!foundTarget && foundEdges.Count > 0 && foundEdges.Count < edgeSets[oldEdgeSet - 1].Count)
                {
                    edgeSets.Add(new List<int>());
                    int newEdgeSet = edgeSets.Count;
                    for (var i = 0; i < foundEdges.Count; i++)
                    {
                        int otherEdgeIndex = foundEdges[i];
                        int otherIndex = edgeSets[oldEdgeSet - 1].IndexOf(otherEdgeIndex);
                        if (otherIndex < 0)
                            continue;
                        edgeSetChanges.Add(new int[] { otherEdgeIndex, newEdgeSet, oldEdgeSet, otherIndex });
                        edgeSets[oldEdgeSet - 1].RemoveAt(otherIndex);
                        Edge e = edges[otherEdgeIndex];
                        e.EdgeSet = newEdgeSet;
                        edgeSets[newEdgeSet - 1].Add(otherEdgeIndex);
                    }
                }
            }
            return true;
        }

        internal void UnperformUnsetZero(int edgeIndex, EdgeState state, List<int[]> edgeSetChanges)
        {
            Edge edge = edges[edgeIndex];
            RawEdgeSet(state, edge);
            for (int i = edgeSetChanges.Count - 1; i >= 0; i--)
            {
                int[] edgeSetChange = edgeSetChanges[i];
                int otherEdgeIndex = edgeSetChange[0];
                int newEdgeSet = edgeSetChange[1];
                int oldEdgeSet = edgeSetChange[2];
                if (newEdgeSet != 0)
                {

                    List<int> edgeSet = edgeSets[newEdgeSet - 1];
                    int edgeCheck = edgeSet[edgeSet.Count - 1];
                    if (edgeCheck != otherEdgeIndex)
                        throw new InvalidOperationException("Attempting to undo out of order.");
                    edgeSet.RemoveAt(edgeSet.Count - 1);
                    if (oldEdgeSet != 0)
                    {
                        edgeSet = edgeSets[oldEdgeSet - 1];
                        edgeSet.Insert(edgeSetChange[3], otherEdgeIndex);
                    }
                    // If it was a new edge, it may have been a new edge set, which needs to be removed.
                    if (newEdgeSet == edgeSets.Count && edgeSets[newEdgeSet - 1].Count == 0)
                        edgeSets.RemoveAt(newEdgeSet - 1);
                }
                else
                {
                    List<int> edgeSet = edgeSets[oldEdgeSet - 1];
                    edgeSet.Insert(edgeSetChange[3], edgeIndex);
                }
                Edge otherE = edges[otherEdgeIndex];
                otherE.EdgeSet = oldEdgeSet;
            }

        }

        internal void UnjoinColor(List<int[]> colorSetChanges)
        {
            for (int i = colorSetChanges.Count - 1; i >= 0; i--)
            {
                if (colorSetChanges[i].Length == 1)
                {
                    if (colorSetChanges[i][0] == colorSets.Count && colorSets[colorSets.Count - 1].Count == 0)
                        colorSets.RemoveAt(colorSets.Count - 1);
                    else
                        throw new InvalidOperationException("Color changes being performed out of order causing color leakage.");
                }
                else
                {
                    int edge = colorSetChanges[i][0];
                    int before = colorSetChanges[i][1];
                    int after = colorSetChanges[i][2];
                    Edge e = edges[edge];
                    if (e.Color != after)
                        throw new InvalidOperationException("Colors being undone out of order.");
                    e.Color = before;
                    List<int> currentSet = colorSets[Math.Abs(after) - 1];
                    if (currentSet[currentSet.Count - 1] != edge)
                        throw new InvalidOperationException("Colors being undone out of order.");
                    currentSet.RemoveAt(currentSet.Count - 1);
                    if (before != 0)
                    {
                        List<int> revertToSet = colorSets[Math.Abs(before) - 1];
                        revertToSet.Add(edge);
                    }
                }
            }
        }

        internal void UnjoinCellColor(List<int[]> colorSetChanges)
        {
            for (int i = colorSetChanges.Count - 1; i >= 0; i--)
            {
                if (colorSetChanges[i].Length == 1)
                {
                    if (colorSetChanges[i][0] == cellColorSets.Count && cellColorSets[cellColorSets.Count - 1].Count == 0)
                        cellColorSets.RemoveAt(cellColorSets.Count - 1);
                    else
                        throw new InvalidOperationException("Color changes being performed out of order causing color leakage.");
                }
                else
                {
                    int cell = colorSetChanges[i][0];
                    int before = colorSetChanges[i][1];
                    int after = colorSetChanges[i][2];
                    Cell c = cells[cell];
                    if (c.Color != after)
                        throw new InvalidOperationException("Colors being undone out of order.");
                    c.Color = before;
                    List<int> currentSet = cellColorSets[Math.Abs(after) - 1];
                    if (currentSet[currentSet.Count - 1] != cell)
                        throw new InvalidOperationException("Colors being undone out of order.");
                    currentSet.RemoveAt(currentSet.Count - 1);
                    if (before != 0)
                    {
                        List<int> revertToSet = cellColorSets[Math.Abs(before) - 1];
                        revertToSet.Add(cell);
                    }
                }
            }
        }

        internal void GetSomething(List<IAction> changes)
        {
            IterativeTrySolveWithoutRollback(changes);
        }

        internal bool ClearCellColor(int cell1, out int index)
        {
            index = -1;
            Cell c = cells[cell1];
            if (c.Color == 0)
                return false;
            int colorSet = Math.Abs(c.Color) - 1;
            List<int> set = cellColorSets[colorSet];
            for (int i = 0; i < set.Count; i++)
            {
                if (set[i] == cell1)
                {
                    c.Color = 0;
                    index = i;
                    set.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        internal void ResetCellColor(int cell1, int oldColor, int oldSetPos)
        {
            Cell c = cells[cell1];
            if (c.Color != 0)
                throw new InvalidOperationException("Unperforming actions out of order.");
            c.Color = oldColor;
            int colorSet = Math.Abs(c.Color) - 1;
            List<int> set = cellColorSets[colorSet];
            set.Insert(oldSetPos, cell1);
        }

        internal bool RestrictEdges(int edge1, int edge2, EdgePairRestriction state, ref bool wasteOfTime)
        {
            wasteOfTime = false;
            if (edgePairRestrictions[edge1, edge2] == state)
            {
                wasteOfTime = true;
                return true;
            }
            if (edgePairRestrictions[edge1, edge2] != EdgePairRestriction.None)
            {
                // Not matching is not a contradiction, technically we could 'upconvert' to a color join, but that is way too complicated for me right now.
                // TODO: upconvert to color join. WasteOfTime would then only possibly be false.
                wasteOfTime = true;
                return true;
            }
            edgePairRestrictions[edge1, edge2] = state;
            edgePairRestrictions[edge2, edge1] = state;
            return true;
        }

        internal void UnRestrictEdges(int edge1, int edge2)
        {
            Debug.Assert(this.edgePairRestrictions[edge1, edge2] != EdgePairRestriction.None);
            edgePairRestrictions[edge1, edge2] = EdgePairRestriction.None;
            edgePairRestrictions[edge2, edge1] = EdgePairRestriction.None;
        }
    }

    public class SetAction : IAction
    {

        public SetAction(Mesh mesh, int edge, EdgeState newState)
        {
            this.mesh = mesh;
            this.edge = edge;
            this.newState = newState;
        }

        private Mesh mesh;

        public int EdgeIndex
        {
            get
            {
                return edge;
            }
        }
        private int edge;

        public EdgeState EdgeState
        {
            get
            {
                return newState;
            }
        }
        private EdgeState newState;

        public List<int> GetAffectedEdges()
        {
            List<int> res = new List<int>();
            if (edgeSetChanges != null)
            {
                for (var index = 0; index < edgeSetChanges.Count; index++)
                {
                    int[] change = edgeSetChanges[index];
                    // Ignore changes to the number of sets.
                    if (change.Length > 1 && change[0] != edge)
                        res.Add(change[0]);
                }
            }
            res.Add(edge);
            return res;
        }
        private List<int[]>? edgeSetChanges;

        public bool Successful
        {
            get
            {
                return successful;
            }
        }
        private bool successful;

#region IAction Members

        public string Name
        {
            get { return "Make Edge " + edge.ToString() + " " + newState.ToString(); }
        }

        public bool Perform()
        {
            successful = true;
            edgeSetChanges = new List<int[]>();
            if (!mesh.PerformSetZero(edge, newState, edgeSetChanges))
            {
                successful = false;
            }
            return true;
        }

        public void Unperform()
        {
            if (edgeSetChanges != null)
                mesh.UnperformSetZero(edge, newState, edgeSetChanges);
        }

#endregion

#region IEquatable<IAction> Members

        public bool Equals(IAction? other)
        {
            SetAction? otherAction = other as SetAction;
            if (otherAction == null)
                return false;
            else
                return otherAction.newState == this.newState && otherAction.mesh == this.mesh && otherAction.edge == this.edge;
        }

#endregion

        public override int GetHashCode()
        {
            return HashCode.Combine(mesh.GetHashCode(), edge, (int)newState);
        }

        public override bool Equals(object? obj)
        {
            if (obj is IAction action)
                return Equals(action);
            return false;
        }
    }

    public class UnsetAction : IAction
    {

        public UnsetAction(Mesh mesh, int edge)
        {
            this.mesh = mesh;
            this.edge = edge;
        }

        private Mesh mesh;

        public int EdgeIndex
        {
            get
            {
                return edge;
            }
        }
        private int edge;
        private EdgeState oldState;
        private List<int[]>? edgeSetChanges;

        public bool Successful
        {
            get
            {
                return successful;
            }
        }
        private bool successful;

#region IAction Members

        public string Name
        {
            get { return "Clear Edge " + edge.ToString(); }
        }

        public bool Perform()
        {
            successful = true;
            edgeSetChanges = new List<int[]>();
            if (!mesh.PerformUnsetZero(edge, out oldState, edgeSetChanges))
            {
                successful = false;
                return false;
            }
            return true;
        }

        public void Unperform()
        {
            if (successful && edgeSetChanges != null)
                mesh.UnperformUnsetZero(edge, oldState, edgeSetChanges);
        }

#endregion

#region IEquatable<IAction> Members

        public bool Equals(IAction? other)
        {
            UnsetAction? otherAction = other as UnsetAction;
            if (otherAction == null)
                return false;
            else
                return otherAction.mesh == this.mesh && otherAction.edge == this.edge;
        }

#endregion

        public override int GetHashCode()
        {
            return HashCode.Combine(mesh.GetHashCode(), edge);
        }

        public override bool Equals(object? obj)
        {
            if (obj is IAction action)
                return Equals(action);
            return false;
        }
    }

    public class ColorJoinAction : IAction
    {

        public ColorJoinAction(Mesh mesh, int edge1, int edge2, bool same)
        {
            this.mesh = mesh;
            this.edge1 = edge1;
            this.edge2 = edge2;
            this.same = same;
        }

        private Mesh mesh;
        public int Edge1
        {
            get
            {
                return edge1;
            }
        }
        private int edge1;
        public int Edge2
        {
            get
            {
                return edge2;
            }
        }
        private int edge2;
        public bool Same
        {
            get
            {
                return same;
            }
        }
        private bool same;

        public List<int> GetAffectedEdges()
        {
            List<int> res = new List<int>();
            if (colorSetChanges != null)
            {
                for (var index = 0; index < colorSetChanges.Count; index++)
                {
                    int[] change = colorSetChanges[index];
                    // Ignore changes to the number of sets.
                    if (change.Length > 1)
                        res.Add(change[0]);
                }
            }
            return res;
        }

        private List<int[]>? colorSetChanges;

        public bool Successful
        {
            get
            {
                return successful;
            }
        }
        private bool successful;

        public bool WasteOfTime
        {
            get
            {
                return wasteOfTime;
            }
        }
        private bool wasteOfTime;

#region IAction Members

        public string Name
        {
            get { return "Color Edges " + edge1.ToString() + " and " + edge2.ToString() + (same ? " the same" : " opposite"); }
        }

        public bool Perform()
        {
            colorSetChanges = new List<int[]>();
            if (!mesh.JoinColor(edge1, edge2, !same, colorSetChanges, ref wasteOfTime))
            {
                successful = false;
                return false;
            }
            successful = true;
            return true;
        }

        public void Unperform()
        {
            if (colorSetChanges != null)
                mesh.UnjoinColor(colorSetChanges);
        }

#endregion

#region IEquatable<IAction> Members

        public bool Equals(IAction? other)
        {
            ColorJoinAction? otherAction = other as ColorJoinAction;
            if (otherAction == null)
                return false;
            else
                return otherAction.mesh == this.mesh && otherAction.edge1 == this.edge1 && otherAction.edge2 == this.edge2 && otherAction.same == this.same;
        }

#endregion

        public override int GetHashCode()
        {
            return HashCode.Combine(mesh.GetHashCode(), edge1, edge2, same ? 1 : 0);
        }

        public override bool Equals(object? obj)
        {
            if (obj is IAction action)
                return Equals(action);
            return false;
        }
    }

    public class CellColorJoinAction : IAction
    {

        public CellColorJoinAction(Mesh mesh, int cell1, int cell2, bool same)
        {
            this.mesh = mesh;
            this.cell1 = cell1;
            this.cell2 = cell2;
            this.same = same;
        }
        private Mesh mesh;
        public int Cell1
        {
            get
            {
                return cell1;
            }
        }
        private int cell1;
        public int Cell2
        {
            get
            {
                return cell2;
            }
        }
        private int cell2;
        public bool Same
        {
            get
            {
                return same;
            }
        }
        private bool same;

        public List<int> GetAffectedCells()
        {
            List<int> res = new List<int>();
            if (colorSetChanges != null)
            {
                for (var index = 0; index < colorSetChanges.Count; index++)
                {
                    int[] change = colorSetChanges[index];
                    // Ignore changes to the number of sets.
                    if (change.Length > 1)
                        res.Add(change[0]);
                }
            }
            return res;
        }

        private List<int[]>? colorSetChanges;

        public bool Successful
        {
            get
            {
                return successful;
            }
        }
        private bool successful;

        public bool WasteOfTime
        {
            get
            {
                return wasteOfTime;
            }
        }
        private bool wasteOfTime;

#region IAction Members

        public string Name
        {
            get { return "Color Cells " + cell1.ToString() + " and " + cell2.ToString() + (same ? " the same" : " opposite"); }
        }

        public bool Perform()
        {
            colorSetChanges = new List<int[]>();
            if (!mesh.JoinCellColor(cell1, cell2, !same, colorSetChanges, ref wasteOfTime))
            {
                successful = false;
                return false;
            }
            successful = true;
            return true;
        }

        public void Unperform()
        {
            if (colorSetChanges != null)
                mesh.UnjoinCellColor(colorSetChanges);
        }

#endregion

#region IEquatable<IAction> Members

        public bool Equals(IAction? other)
        {
            CellColorJoinAction? otherAction = other as CellColorJoinAction;
            if (otherAction == null)
                return false;
            else
                return otherAction.mesh == this.mesh && otherAction.cell1 == this.cell1 && otherAction.cell2 == this.cell2 && otherAction.same == this.same;
        }

#endregion

        public override int GetHashCode()
        {
            return HashCode.Combine(mesh.GetHashCode(), cell1, cell2, same ? 1 : 0);
        }

        public override bool Equals(object? obj)
        {
            if (obj is IAction action)
                return Equals(action);
            return false;
        }
    }

    public class CellColorClearAction : IAction
    {

        public CellColorClearAction(Mesh mesh, int cell1)
        {
            this.mesh = mesh;
            this.cell1 = cell1;
        }

        private Mesh mesh;
        public int Cell1
        {
            get
            {
                return cell1;
            }
        }
        private int cell1;

        private int oldColor;
        private int oldSetPos;


        public bool Successful
        {
            get
            {
                return successful;
            }
        }
        private bool successful;

#region IAction Members

        public string Name
        {
            get { return "Clear Color of Cell " + cell1.ToString(); }
        }

        public bool Perform()
        {
            oldColor = mesh.Cells[cell1].Color;
            if (!mesh.ClearCellColor(cell1, out oldSetPos))
            {
                successful = false;
                return false;
            }
            successful = true;
            return true;
        }

        public void Unperform()
        {
            mesh.ResetCellColor(cell1, oldColor, oldSetPos);
        }

#endregion

#region IEquatable<IAction> Members

        public bool Equals(IAction? other)
        {
            CellColorClearAction? otherAction = other as CellColorClearAction;
            if (otherAction == null)
                return false;
            else
                return otherAction.mesh == this.mesh && otherAction.cell1 == this.cell1;
        }

#endregion

        public override int GetHashCode()
        {
            return HashCode.Combine(mesh.GetHashCode(), cell1);
        }

        public override bool Equals(object? obj)
        {
            if (obj is IAction action)
                return Equals(action);
            return false;
        }
    }

    public class EdgeRestrictionAction : IAction
    {

        public EdgeRestrictionAction(Mesh mesh, int edge1, int edge2, EdgePairRestriction state)
        {
            this.mesh = mesh;
            this.edge1 = edge1;
            this.edge2 = edge2;
            this.state = state;
        }
        private Mesh mesh;
        public int Edge1
        {
            get
            {
                return edge1;
            }
        }
        private int edge1;
        public int Edge2
        {
            get
            {
                return edge2;
            }
        }
        private int edge2;
        public EdgePairRestriction State
        {
            get
            {
                return state;
            }
        }
        private EdgePairRestriction state;

        public List<int> GetAffectedEdges()
        {
            List<int> res = new List<int>();
            if (!wasteOfTime)
            {
                res.Add(edge1);
                res.Add(edge2);
            }
            return res;
        }

        public bool Successful
        {
            get
            {
                return successful;
            }
        }
        private bool successful;

        public bool WasteOfTime
        {
            get
            {
                return wasteOfTime;
            }
        }
        private bool wasteOfTime;

#region IAction Members

        public string Name
        {
            get { return "Restrict Edges " + edge1.ToString() + " and " + edge2.ToString() + " to be " + state.ToString(); }
        }

        public bool Perform()
        {
            if (!mesh.RestrictEdges(edge1, edge2, state, ref wasteOfTime))
            {
                successful = false;
                return false;
            }
            successful = true;
            return true;
        }

        public void Unperform()
        {
            if (!wasteOfTime)
                mesh.UnRestrictEdges(edge1, edge2);
        }

#endregion

#region IEquatable<IAction> Members

        public bool Equals(IAction? other)
        {
            EdgeRestrictionAction? otherAction = other as EdgeRestrictionAction;
            if (otherAction == null)
                return false;
            else
                return otherAction.mesh == this.mesh && otherAction.edge1 == this.edge1 && otherAction.edge2 == this.edge2 && otherAction.state == this.state;
        }

#endregion

        public override int GetHashCode()
        {
            return HashCode.Combine(mesh.GetHashCode(), edge1, edge2, (int)state);
        }

        public override bool Equals(object? obj)
        {
            if (obj is IAction action)
                return Equals(action);
            return false;
        }
    }

    public class ActionSorter : IComparer<IAction>
    {
#region IComparer<IAction> Members

        public int Compare(IAction? x, IAction? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            if (x is SetAction)
            {
                if (y is SetAction)
                {
                    return ((SetAction)x).EdgeIndex.CompareTo(((SetAction)y).EdgeIndex);
                }
                else
                {
                    return 1;
                }
            }
            else if (x is ColorJoinAction)
            {
                ColorJoinAction xc = (ColorJoinAction)x;
                if (y is SetAction)
                {
                    return -1;
                }
                else if (y is ColorJoinAction)
                {
                    ColorJoinAction yc = (ColorJoinAction)y;
                    int res = xc.Edge1.CompareTo(yc.Edge1);
                    if (res == 0)
                        res = xc.Edge2.CompareTo(yc.Edge2);
                    return res;
                }
                else
                {
                    return 1;
                }
            }
            else if (x is CellColorJoinAction)
            {
                CellColorJoinAction xc = (CellColorJoinAction)x;
                if (y is CellColorJoinAction)
                {
                    CellColorJoinAction yc = (CellColorJoinAction)y;
                    int res = xc.Cell1.CompareTo(yc.Cell1);
                    if (res == 0)
                        res = xc.Cell2.CompareTo(yc.Cell2);
                    return res;

                }
                else
                {
                    return -1;
                }
            }
            throw new NotSupportedException("ActionSorter doesn't support the provided action type.");
        }

#endregion
    }

    /// <summary>
    /// Tracks which set each item belongs to after a set of union operations to merge disjoint sets.
    /// This is very efficient, it can be consider that each operation takes close to O(1) time on average.
    /// </summary>
    public class DisjointTracker
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        public DisjointTracker(int size,bool prepop)
        {
            this.tracker = new int[size];
            this.ranker = new int[size];
            if (prepop)
            {
                for (int i = 0; i < size; i++)
                {
                    tracker[i] = i;
                    ranker[i] = 0;
                }
            }
        }

        private int[] tracker;
        private int[] ranker;

        public int Size => tracker != null ? tracker.Length : 0;


        /// <summary>
        /// Adds a new item to be tracked.  Initially it is in a set of its own.
        /// </summary>
        /// <param name="value">
        /// Value to be added to the tracking.
        /// </param>
        public void Add(int value)
        {
            tracker[value] = value;
            ranker[value] = 0;
        }

        /// <summary>
        /// Unions the two sets which contain the specified items.  Does nothing if the items are already in the same set.
        /// </summary>
        /// <param name="first">
        /// First item to find set to union.
        /// </param>
        /// <param name="second">
        /// Second item to find set to union.
        /// </param>
        public void Union(int first, int second)
        {
            Link(GetRepresentative(first), GetRepresentative(second));
        }

        /// <summary>
        /// Gets the primary member of the set which a given value belongs to.
        /// </summary>
        /// <param name="value">
        /// Value to lookup.
        /// </param>
        /// <returns>
        /// The primary member of the set that the value is currently a member of.
        /// </returns>
        public int GetRepresentative(int value)
        {
            int parent = tracker[value];
            if (parent == value || parent==tracker[parent])
                return parent;
            int realParent = GetRepresentative(parent);
            tracker[value] = realParent;
            return realParent;
        }

        private void Link(int first, int second)
        {
            if (first == second)
                return;
            int firstRank = ranker[first];
            int secondRank = ranker[second];
            if (firstRank > secondRank)
            {
                tracker[second] = first;
            }
            else
            {
                if (firstRank == secondRank)
                    ranker[second] = secondRank + 1;
                tracker[first] = second;
            }
        }

        internal void Reset()
        {
            int size = tracker.Length;
            for (int i = 0; i < size; i++)
            {
                tracker[i] = i;
                ranker[i] = 0;
            }
        }
    }
}
