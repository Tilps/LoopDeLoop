using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LoopDeLoop
{
    public static class PuzzleHelper
    {
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
            switch (type)
            {
                case MeshType.Octagon:
                case MeshType.Square2:
                case MeshType.Square3:
                case MeshType.FloretPentagons:
                case MeshType.CairoPentagons:
                case MeshType.Kites:
                    width = 5;
                    height = 5;
                    break;
                case MeshType.Hexagonal:
                    width = 5;
                    height = 10;
                    break;
                case MeshType.Hexagonal2:
                case MeshType.Triangle:
                case MeshType.Pentagon:
                case MeshType.Diamonds:
                case MeshType.AsymmetricPentagons:
                    width = 6;
                    height = 6;
                    break;
                case MeshType.Hexagonal3:
                case MeshType.Hexagonal4:
                case MeshType.PentagonHexagon:
                case MeshType.DiamondSquare:
                    width = 4;
                    height = 4;
                    break;
                case MeshType.PentagonHexagon2:
                    width = 8;
                    height = 4;
                    break;
                default:
                    width = 10;
                    height = 10;
                    break;
            }
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
                return width > 0 && height > 0;
            }
            else
            {
                if (!int.TryParse(span, out width))
                    return false;
                height = width;
                return width > 0;
            }
        }

        public static MeshType MeshTypeFromString(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return MeshType.Square;
            return typeName switch
            {
                "Square" => MeshType.Square,
                "Square Symmetrical" => MeshType.SquareSymmetrical,
                "Triangle" => MeshType.Triangle,
                "Hexagon" => MeshType.Hexagonal,
                "Hexagon2" => MeshType.Hexagonal2,
                "Hexagon3" => MeshType.Hexagonal3,
                "Octagon" => MeshType.Octagon,
                "Square2" => MeshType.Square2,
                "Square3" or "Square 3" or "Snub Square" or "SnubSquare" => MeshType.Square3,
                "Pentagon" => MeshType.Pentagon,
                "Kites" => MeshType.Kites,
                "Asymmetric Pentagons" or "AsymmetricPentagons" => MeshType.AsymmetricPentagons,
                "Diamonds" => MeshType.Diamonds,
                "Diamond-Square" or "DiamondSquare" => MeshType.DiamondSquare,
                "Pentagon-Hexagon" or "PentagonHexagon" => MeshType.PentagonHexagon,
                "Pentagon-Hexagon 2" or "PentagonHexagon2" or "Prismatic Pentagons" or "PrismaticPentagons" => MeshType.PentagonHexagon2,
                "Floret Pentagons" or "FloretPentagons" or "Hex-Pentagons" or "HexPentagons" => MeshType.FloretPentagons,
                "Cairo Pentagons" or "CairoPentagons" => MeshType.CairoPentagons,
                "Hexagon 4" or "Hexagon4" or "Hexagonal4" => MeshType.Hexagonal4,
                _ => Enum.TryParse<MeshType>(typeName, out var result) ? result : MeshType.Square
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

            var visitedEdges = new HashSet<int>();
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
                    if (nextEdge != -1) visitedEdges.Add(nextEdge);
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
    }
}

