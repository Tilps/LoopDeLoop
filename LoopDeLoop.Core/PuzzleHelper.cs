using System;
using System.Collections.Generic;
using System.Threading;
using LoopDeLoop.TrialOrdering;

namespace LoopDeLoop
{
    public sealed record PuzzleAnalysisResult(
        string SolveStatus,
        string TechniqueTier,
        int BranchDepth,
        int ClueCount,
        int TotalCells,
        int EdgeCount,
        int? LookaheadDepth = null);

    public static class PuzzleHelper
    {
        public const int MaxGridDimension = 50;

        /// <summary>
        /// When true, solver and difficulty 0-4 generator meshes attach the embedded pure-C#
        /// IncidenceGnnPolicy to prioritize high-progress trials first.
        /// </summary>
        public static bool UseNeuralTrialOrdering { get; set; } = true;

        public static Mesh CreateFullSolverMesh(Mesh source)
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
            if (UseNeuralTrialOrdering)
                solver.TrialOrderingPolicy = IncidenceGnnPolicy.CreateDefault();
            return solver;
        }

        public static PuzzleAnalysisResult AnalyzePuzzle(Mesh source, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);

            int clueCount = 0;
            foreach (var cell in source.Cells)
            {
                if (cell.TargetCount >= 0) clueCount++;
            }
            int totalCells = source.Cells.Count;
            int edgeCount = source.Edges.Count;

            Mesh solver = CreateFullSolverMesh(source);
            solver.Clear();

            SolveState fullResult = solver.TrySolve(false, cancellationToken);
            if (fullResult == SolveState.MultipleSolutions)
            {
                return new PuzzleAnalysisResult("Multiple Solutions", "Non-unique", 0, clueCount, totalCells, edgeCount);
            }
            if (fullResult != SolveState.Solved)
            {
                return new PuzzleAnalysisResult("No Solution", "Unsolvable", 0, clueCount, totalCells, edgeCount);
            }

            // RecursiveTrySolve pushes a base frame before checking trials, so branch count is Length - 1.
            int branchDepth = Math.Max(0, (solver.DepthPatern?.Length ?? 0) - 1);
            string tier = $"Recursive Search (F, Depth {branchDepth})";
            int? lookaheadDepth = null;

            // Evaluate deductive tiers in ascending order using validated SetRatingCodeOptions codes.
            solver.SetRatingCodeOptions("S");
            if (solver.TrySolve(false, cancellationToken) == SolveState.Solved)
            {
                int min = -1;
                int max = solver.Edges.Count;
                while (min < max - 1)
                {
                    int mid = (min + max) / 2;
                    solver.IterativeSolverDepth = mid;
                    if (solver.TrySolve(false, cancellationToken) == SolveState.Solved)
                        max = mid;
                    else
                        min = mid;
                }
                lookaheadDepth = max;
                tier = max == 0 ? "Direct Deductions (S0)" : $"Iterative Lookahead (S{max})";
            }
            else
            {
                (string Code, string Label)[] deductiveTiers =
                [
                    ("SI", "Cell-Edge Interactions (SI)"),
                    ("SIC", "Edge Coloring (SIC)"),
                    ("SICO", "Inside/Outside Cell Coloring (SICO)"),
                    ("SICOE", "Edge Pair Restrictions (SICOE)"),
                    ("SICOERC+MHP", "Full Iterative Reasoning (SICOERC+MHP)")
                ];

                foreach (var (code, label) in deductiveTiers)
                {
                    solver.SetRatingCodeOptions(code);
                    if (solver.TrySolve(false, cancellationToken) == SolveState.Solved)
                    {
                        tier = label;
                        break;
                    }
                }
            }

            return new PuzzleAnalysisResult("Unique Solution", tier, branchDepth, clueCount, totalCells, edgeCount, lookaheadDepth);
        }

        public static Mesh MakeMesh(int width, int height, MeshType type, int difficulty)
        {
            Mesh mesh = new Mesh(width, height, type);
            mesh.ConsiderMultipleLoops = difficulty > 0;
            mesh.IterativeRecMaxDepth = 1;
            if (difficulty > 1)
            {
                mesh.UseCellColoring = true;
                mesh.UseCellColoringTrials = true;
                mesh.UseColoring = true;
                mesh.UseEdgeRestricts = true;
                mesh.UseDerivedColoring = true;
                mesh.UseMerging = true;
                mesh.UseCellPairsTopLevel = true;
            }
            if (difficulty < 5)
            {
                mesh.IterativeSolverDepth = Math.Max(0, difficulty - 2);
                if (UseNeuralTrialOrdering && difficulty >= 2)
                    mesh.TrialOrderingPolicy = IncidenceGnnPolicy.CreateDefault();
            }
            else
            {
                mesh.SolverMethod = SolverMethod.Recursive;
            }
            mesh.GenerateBoringFraction = 0.01;
            return mesh;
        }

        public static void GetDefaultSize(MeshType type, out int width, out int height)
        {
            (width, height) = type switch
            {
                MeshType.Octagon or MeshType.Square2 or MeshType.Square3 or MeshType.FloretPentagons or MeshType.CairoPentagons or MeshType.Kites => (5, 5),
                MeshType.Hexagonal => (5, 10),
                MeshType.Hexagonal2 or MeshType.Triangle or MeshType.Pentagon or MeshType.Diamonds or MeshType.AsymmetricPentagons => (6, 6),
                MeshType.Hexagonal3 or MeshType.Hexagonal4 or MeshType.PentagonHexagon or MeshType.DiamondSquare => (4, 4),
                MeshType.PentagonHexagon2 => (8, 4),
                _ => (10, 10)
            };
        }

        public static bool ParseSize(string val, MeshType type, out int width, out int height)
        {
            GetDefaultSize(type, out width, out height);
            if (string.IsNullOrWhiteSpace(val))
                return true;

            ReadOnlySpan<char> span = val.AsSpan().Trim();
            int sepIndex = span.IndexOfAny('x', 'X', '*');
            if (sepIndex >= 0)
            {
                ReadOnlySpan<char> wSpan = span[..sepIndex].Trim();
                ReadOnlySpan<char> hSpan = span[(sepIndex + 1)..].Trim();
                if (!int.TryParse(wSpan, out width) || !int.TryParse(hSpan, out height))
                    return false;
                return width > 0 && height > 0 && width <= MaxGridDimension && height <= MaxGridDimension;
            }
            else
            {
                if (!int.TryParse(span, out width))
                    return false;
                height = width;
                return width > 0 && width <= MaxGridDimension;
            }
        }

        public static string StringFromMeshType(MeshType type) => type switch
        {
            MeshType.Square => "Square",
            MeshType.SquareSymmetrical => "Square Symmetrical",
            MeshType.Triangle => "Triangle",
            MeshType.Hexagonal => "Hexagon",
            MeshType.Hexagonal2 => "Hexagon2",
            MeshType.Hexagonal3 => "Hexagon3",
            MeshType.Hexagonal4 => "Hexagon 4",
            MeshType.Octagon => "Octagon",
            MeshType.Square2 => "Square2",
            MeshType.Square3 => "Square3",
            MeshType.Pentagon => "Pentagon",
            MeshType.Kites => "Kites",
            MeshType.AsymmetricPentagons => "Asymmetric Pentagons",
            MeshType.Diamonds => "Diamonds",
            MeshType.DiamondSquare => "Diamond-Square",
            MeshType.PentagonHexagon => "Pentagon-Hexagon",
            MeshType.PentagonHexagon2 => "Pentagon-Hexagon 2",
            MeshType.FloretPentagons => "Floret Pentagons",
            MeshType.CairoPentagons => "Cairo Pentagons",
            _ => type.ToString()
        };

        public static MeshType MeshTypeFromString(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return MeshType.Square;
            string normalized = typeName.Trim();
            return normalized.ToLowerInvariant() switch
            {
                "square" => MeshType.Square,
                "square symmetrical" or "squaresymmetrical" => MeshType.SquareSymmetrical,
                "triangle" => MeshType.Triangle,
                "hexagon" or "hexagonal" => MeshType.Hexagonal,
                "hexagon2" or "hexagonal2" => MeshType.Hexagonal2,
                "hexagon3" or "hexagonal3" => MeshType.Hexagonal3,
                "octagon" => MeshType.Octagon,
                "square2" => MeshType.Square2,
                "square3" or "square 3" or "snub square" or "snubsquare" => MeshType.Square3,
                "pentagon" => MeshType.Pentagon,
                "kites" => MeshType.Kites,
                "asymmetric pentagons" or "asymmetricpentagons" => MeshType.AsymmetricPentagons,
                "diamonds" => MeshType.Diamonds,
                "diamond-square" or "diamondsquare" => MeshType.DiamondSquare,
                "pentagon-hexagon" or "pentagonhexagon" => MeshType.PentagonHexagon,
                "pentagon-hexagon 2" or "pentagonhexagon2" or "prismatic pentagons" or "prismaticpentagons" => MeshType.PentagonHexagon2,
                "floret pentagons" or "floretpentagons" or "hex-pentagons" or "hexpentagons" => MeshType.FloretPentagons,
                "cairo pentagons" or "cairopentagons" => MeshType.CairoPentagons,
                "hexagon 4" or "hexagon4" or "hexagonal4" => MeshType.Hexagonal4,
                _ => Enum.TryParse<MeshType>(normalized, ignoreCase: true, out var result) && Enum.IsDefined(result)
                    ? result
                    : MeshType.Square
            };
        }

        public static bool CheckIsSolved(Mesh mesh)
        {
            if (mesh == null || mesh.Cells.Count == 0 || mesh.Edges.Count == 0)
                return false;

            bool nonempty = false;
            for (int i = 0; i < mesh.Cells.Count; i++)
            {
                if (mesh.Cells[i].TargetCount >= 0)
                {
                    nonempty = true;
                    if (mesh.Cells[i].FilledCount != mesh.Cells[i].TargetCount)
                        return false;
                }
            }
            if (!nonempty)
                return false;

            int filledEdgeCount = 0;
            for (int i = 0; i < mesh.Edges.Count; i++)
            {
                if (mesh.Edges[i].State == EdgeState.Filled)
                    filledEdgeCount++;
            }
            if (filledEdgeCount < 3)
                return false;

            for (int i = 0; i < mesh.Intersections.Count; i++)
            {
                int deg = mesh.Intersections[i].FilledCount;
                if (deg != 0 && deg != 2)
                    return false;
            }

            // Verify that all filled edges form a single connected loop
            int startIntersection = -1;
            for (int i = 0; i < mesh.Intersections.Count; i++)
            {
                if (mesh.Intersections[i].FilledCount == 2)
                {
                    startIntersection = i;
                    break;
                }
            }
            if (startIntersection == -1)
                return false;

            HashSet<int> visitedEdges = [with(filledEdgeCount)];
            int currentIntersection = startIntersection;
            int prevEdge = -1;

            while (true)
            {
                int nextEdge = -1;
                foreach (int edgeIdx in mesh.Intersections[currentIntersection].Edges)
                {
                    if (edgeIdx != prevEdge && mesh.Edges[edgeIdx].State == EdgeState.Filled)
                    {
                        nextEdge = edgeIdx;
                        break;
                    }
                }
                if (nextEdge == -1 || visitedEdges.Contains(nextEdge))
                {
                    break;
                }
                visitedEdges.Add(nextEdge);
                Edge edge = mesh.Edges[nextEdge];
                currentIntersection = (edge.Intersections[0] == currentIntersection) ? edge.Intersections[1] : edge.Intersections[0];
                prevEdge = nextEdge;
                if (currentIntersection == startIntersection)
                    break;
            }

            return visitedEdges.Count == filledEdgeCount;
        }

        public static void RecalculateCounts(Mesh mesh)
        {
            for (int i = 0; i < mesh.Cells.Count; i++)
            {
                var cell = mesh.Cells[i];
                int filled = 0;
                int excluded = 0;
                for (int e = 0; e < cell.Edges.Count; e++)
                {
                    var st = mesh.Edges[cell.Edges[e]].State;
                    if (st == EdgeState.Filled) filled++;
                    else if (st == EdgeState.Excluded) excluded++;
                }
                cell.FilledCount = filled;
                cell.ExcludedCount = excluded;
            }

            for (int i = 0; i < mesh.Intersections.Count; i++)
            {
                var inter = mesh.Intersections[i];
                int filled = 0;
                int excluded = 0;
                for (int e = 0; e < inter.Edges.Count; e++)
                {
                    var st = mesh.Edges[inter.Edges[e]].State;
                    if (st == EdgeState.Filled) filled++;
                    else if (st == EdgeState.Excluded) excluded++;
                }
                inter.FilledCount = filled;
                inter.ExcludedCount = excluded;
            }
        }
    }
}

