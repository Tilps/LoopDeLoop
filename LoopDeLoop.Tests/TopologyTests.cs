using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LoopDeLoop;

namespace LoopDeLoop.Tests
{
    [TestClass]
    public class TopologyTests
    {
        private static readonly MeshType[] AllMeshTypes =
        {
            MeshType.Square,
            MeshType.SquareSymmetrical,
            MeshType.Triangle,
            MeshType.Hexagonal,
            MeshType.Hexagonal2,
            MeshType.Hexagonal3,
            MeshType.Octagon,
            MeshType.Square2,
            MeshType.Pentagon,
            MeshType.Kites,
            MeshType.AsymmetricPentagons,
            MeshType.Diamonds,
            MeshType.DiamondSquare,
            MeshType.PentagonHexagon,
            MeshType.FloretPentagons,
            MeshType.CairoPentagons,
            MeshType.Hexagonal4,
            MeshType.Square3,
            MeshType.PentagonHexagon2
        };

        [TestMethod]
        public void AllTopologies_HaveValidEdgeIntersectionPointers()
        {
            foreach (var type in AllMeshTypes)
            {
                PuzzleHelper.GetDefaultSize(type, out int w, out int h);
                var mesh = new Mesh(w, h, type);

                for (int eIdx = 0; eIdx < mesh.Edges.Count; eIdx++)
                {
                    var edge = mesh.Edges[eIdx];
                    Assert.AreEqual(2, edge.Intersections.Length,
                        $"{type}: Edge {eIdx} must have exactly 2 intersection endpoints");

                    int i1 = edge.Intersections[0];
                    int i2 = edge.Intersections[1];

                    Assert.IsTrue(i1 >= 0 && i1 < mesh.Intersections.Count,
                        $"{type}: Edge {eIdx} intersection 0 ({i1}) out of range [0, {mesh.Intersections.Count})");
                    Assert.IsTrue(i2 >= 0 && i2 < mesh.Intersections.Count,
                        $"{type}: Edge {eIdx} intersection 1 ({i2}) out of range [0, {mesh.Intersections.Count})");
                    Assert.AreNotEqual(i1, i2,
                        $"{type}: Edge {eIdx} has identical endpoints ({i1} == {i2})");

                    // Verify reciprocal adjacency: both intersections reference this edge
                    CollectionAssert.Contains(mesh.Intersections[i1].Edges, eIdx,
                        $"{type}: Intersection {i1} does not reference incident edge {eIdx}");
                    CollectionAssert.Contains(mesh.Intersections[i2].Edges, eIdx,
                        $"{type}: Intersection {i2} does not reference incident edge {eIdx}");
                }
            }
        }

        [TestMethod]
        public void AllTopologies_CellEdgeConsistencyAndManifoldProperty()
        {
            foreach (var type in AllMeshTypes)
            {
                PuzzleHelper.GetDefaultSize(type, out int w, out int h);
                var mesh = new Mesh(w, h, type);

                // Cell cycle sanity: each cell has at least 3 edges forming a polygon
                for (int cIdx = 0; cIdx < mesh.Cells.Count; cIdx++)
                {
                    var cell = mesh.Cells[cIdx];
                    Assert.IsTrue(cell.Edges.Count >= 3,
                        $"{type}: Cell {cIdx} has fewer than 3 edges ({cell.Edges.Count})");

                    foreach (int eIdx in cell.Edges)
                    {
                        Assert.IsTrue(eIdx >= 0 && eIdx < mesh.Edges.Count,
                            $"{type}: Cell {cIdx} references invalid edge index {eIdx}");
                        CollectionAssert.Contains(mesh.Edges[eIdx].Cells, cIdx,
                            $"{type}: Edge {eIdx} does not reciprocally reference cell {cIdx}");
                    }
                }

                // Manifold boundary property: an edge belongs to either 1 cell (boundary) or 2 cells (internal)
                for (int eIdx = 0; eIdx < mesh.Edges.Count; eIdx++)
                {
                    var edge = mesh.Edges[eIdx];
                    Assert.IsTrue(edge.Cells.Count == 1 || edge.Cells.Count == 2,
                        $"{type}: Edge {eIdx} belongs to {edge.Cells.Count} cells (expected 1 or 2)");
                }
            }
        }

        [TestMethod]
        public void AllTopologies_VertexCoordinatesAreFinite()
        {
            foreach (var type in AllMeshTypes)
            {
                PuzzleHelper.GetDefaultSize(type, out int w, out int h);
                var mesh = new Mesh(w, h, type);

                for (int i = 0; i < mesh.Intersections.Count; i++)
                {
                    var inter = mesh.Intersections[i];
                    Assert.IsFalse(double.IsNaN(inter.X) || double.IsInfinity(inter.X),
                        $"{type}: Intersection {i} has invalid X: {inter.X}");
                    Assert.IsFalse(double.IsNaN(inter.Y) || double.IsInfinity(inter.Y),
                        $"{type}: Intersection {i} has invalid Y: {inter.Y}");
                }
            }
        }

        [TestMethod]
        public void AllTopologies_CellPerimetersAreContinuousCycles()
        {
            foreach (var type in AllMeshTypes)
            {
                PuzzleHelper.GetDefaultSize(type, out int w, out int h);
                var mesh = new Mesh(w, h, type);

                for (int cIdx = 0; cIdx < mesh.Cells.Count; cIdx++)
                {
                    var cell = mesh.Cells[cIdx];
                    Assert.AreEqual(cell.Intersections.Count, cell.Edges.Count,
                        $"{type}: Cell {cIdx} vertex count ({cell.Intersections.Count}) != edge count ({cell.Edges.Count})");

                    int n = cell.Intersections.Count;
                    for (int j = 0; j < n; j++)
                    {
                        int v1 = cell.Intersections[j];
                        int v2 = cell.Intersections[(j + 1) % n];
                        int edge = mesh.GetEdgeJoining(v1, v2);
                        Assert.IsTrue(edge >= 0,
                            $"{type}: Cell {cIdx} consecutive vertices {v1} and {v2} are not joined by an edge");
                        CollectionAssert.Contains(cell.Edges, edge,
                            $"{type}: Cell {cIdx} does not contain edge {edge} connecting vertices {v1} and {v2}");
                    }
                }
            }
        }

        [TestMethod]
        public void NewTopologies_CodecRoundTrip()
        {
            MeshType[] newTypes =
            {
                MeshType.Kites,
                MeshType.AsymmetricPentagons,
                MeshType.Diamonds,
                MeshType.DiamondSquare,
                MeshType.PentagonHexagon,
                MeshType.FloretPentagons,
                MeshType.CairoPentagons,
                MeshType.Hexagonal4,
                MeshType.Square3,
                MeshType.PentagonHexagon2
            };

            foreach (var type in newTypes)
            {
                PuzzleHelper.GetDefaultSize(type, out int w, out int h);
                var mesh = new Mesh(w, h, type);

                // Set sample clues
                for (int i = 0; i < mesh.Cells.Count; i += 3)
                {
                    mesh.SetClue(i, i % 3);
                }

                string encoded = PuzzleCodec.Encode(mesh, w, h);
                Assert.IsFalse(string.IsNullOrEmpty(encoded), $"{type}: Encoded string should not be empty");

                bool success = PuzzleCodec.TryDecode(encoded, out var decoded, out int decW, out int decH, out var decType);
                Assert.IsTrue(success, $"{type}: TryDecode should succeed");
                Assert.IsNotNull(decoded, $"{type}: Decoded mesh should not be null");
                Assert.AreEqual(w, decW, $"{type}: Width mismatch after round-trip");
                Assert.AreEqual(h, decH, $"{type}: Height mismatch after round-trip");
                Assert.AreEqual(mesh.MeshType, decoded!.MeshType, $"{type}: MeshType mismatch after round-trip");
                Assert.AreEqual(mesh.Cells.Count, decoded.Cells.Count, $"{type}: Cells count mismatch after round-trip");
                Assert.AreEqual(mesh.Edges.Count, decoded.Edges.Count, $"{type}: Edges count mismatch after round-trip");
                Assert.AreEqual(mesh.Intersections.Count, decoded.Intersections.Count, $"{type}: Intersections count mismatch after round-trip");

                for (int i = 0; i < mesh.Cells.Count; i++)
                {
                    Assert.AreEqual(mesh.Cells[i].TargetCount, decoded.Cells[i].TargetCount,
                        $"{type}: Cell {i} clue mismatch after round-trip");
                }
            }
        }

        [TestMethod]
        public void NewTopologies_CanSolveWithClues()
        {
            MeshType[] newTypes =
            [
                MeshType.Kites,
                MeshType.AsymmetricPentagons,
                MeshType.Diamonds,
                MeshType.DiamondSquare,
                MeshType.PentagonHexagon,
                MeshType.FloretPentagons,
                MeshType.CairoPentagons,
                MeshType.Hexagonal4,
                MeshType.Square3,
                MeshType.PentagonHexagon2
            ];

            foreach (var type in newTypes)
            {
                PuzzleHelper.GetDefaultSize(type, out int w, out int h);
                var mesh = new Mesh(w, h, type);

                // Setting a 0 clue forces all incident edges to be Excluded via PerformStart
                mesh.SetClue(0, 0);
                var changes = new List<IAction>();
                Assert.IsTrue(mesh.PerformStart(changes), $"{type}: PerformStart should succeed for single clue 0");

                foreach (int edgeIdx in mesh.Cells[0].Edges)
                {
                    Assert.AreEqual(EdgeState.Excluded, mesh.Edges[edgeIdx].State,
                        $"{type}: Edge {edgeIdx} of cell 0 should be marked Excluded by clue 0");
                }

                // Undo and verify TrySolve finds solutions cleanly and rolls back
                mesh.Unperform(changes);
                var state = mesh.TrySolve();
                Assert.AreNotEqual(SolveState.NoSolutions, state,
                    $"{type}: Solver should not conclude NoSolutions for empty grid with single clue 0");
            }
        }

        [TestMethod]
        public void DefaultSizes_MatchExpectedDefaults()
        {
            PuzzleHelper.GetDefaultSize(MeshType.Kites, out int kw, out int kh);
            Assert.AreEqual(5, kw);
            Assert.AreEqual(5, kh);

            PuzzleHelper.GetDefaultSize(MeshType.AsymmetricPentagons, out int apw, out int aph);
            Assert.AreEqual(6, apw);
            Assert.AreEqual(6, aph);

            PuzzleHelper.GetDefaultSize(MeshType.CairoPentagons, out int cw, out int ch);
            Assert.AreEqual(5, cw);
            Assert.AreEqual(5, ch);

            PuzzleHelper.GetDefaultSize(MeshType.FloretPentagons, out int fw, out int fh);
            Assert.AreEqual(5, fw);
            Assert.AreEqual(5, fh);
        }

        [TestMethod]
        public void CairoPentagons_HasExpectedCellCountAndManifoldEdges()
        {
            var mesh = new Mesh(3, 3, MeshType.CairoPentagons);
            // 3x3 units, 4 pentagons each = 36 cells
            Assert.AreEqual(36, mesh.Cells.Count);

            foreach (var edge in mesh.Edges)
            {
                Assert.IsTrue(edge.Cells.Count == 1 || edge.Cells.Count == 2,
                    $"CairoPentagons edge has invalid cell count: {edge.Cells.Count}");
            }
        }
    }
}

